using NinetyBackend.Infrastructure.Security;
using NinetyBackend.Modules.Stations.DTOs;
using NinetyBackend.Modules.Stations.Models;
using NinetyBackend.Modules.Stations.Repositories;
using NinetyBackend.Modules.Auth.Services;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace NinetyBackend.Modules.Stations.Services;

public class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IStationRepository _stationRepository;
    private readonly IAgentRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtService _jwtService;
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> BootstrapLocks = new();

    public AgentService(
        IAgentRepository agentRepository,
        IStationRepository stationRepository,
        IAgentRefreshTokenRepository refreshTokenRepository,
        IJwtService jwtService)
    {
        _agentRepository = agentRepository;
        _stationRepository = stationRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtService = jwtService;
    }

    public async Task<AgentRegistrationResponseDto> RegisterAsync(RegisterAgentDto dto)
    {
        var station = await _stationRepository.GetByIdAsync(dto.StationId);
        if (station == null)
            throw new InvalidOperationException("Gaming station not found.");

        if (string.IsNullOrWhiteSpace(dto.MachineName) || string.IsNullOrWhiteSpace(dto.Version)) throw new InvalidOperationException("Machine name and version are required.");
        var now = DateTime.UtcNow;
        var agent = await _agentRepository.GetByStationIdAsync(dto.StationId);
        if (agent is not null && agent.Status is AgentStatus.Pending or AgentStatus.Approved or AgentStatus.Online or AgentStatus.Offline)
            throw new InvalidOperationException("Station already has an active Agent. A supervisor must revoke it before reinstalling the Agent.");
        var bootstrap = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        if (agent == null)
        {
            agent = new Agent
            {
                Id = Guid.NewGuid(),
                StationId = dto.StationId,
                MachineName = dto.MachineName,
                Version = dto.Version,
                Status = AgentStatus.Pending, RegisteredAt = now, BootstrapSecretHash = SecurityUtils.HashToken(bootstrap)
            };
            await _agentRepository.AddAsync(agent);
        }
        else if (agent is not null)
        {
            agent = new Agent
            {
                Id = Guid.NewGuid(),
                StationId = dto.StationId,
                MachineName = dto.MachineName,
                Version = dto.Version,
                Status = AgentStatus.Pending, RegisteredAt = now, BootstrapSecretHash = SecurityUtils.HashToken(bootstrap)
            };
            await _agentRepository.AddAsync(agent);
        }

        station.AgentId = agent.Id;
        station.UpdatedAt = now;
        await _stationRepository.UpdateAsync(station);

        return new AgentRegistrationResponseDto(agent.Id, agent.StationId, agent.Status.ToString(), agent.RegisteredAt, bootstrap, agent.MachineName, agent.Version, agent.LastHeartbeatAt);
    }

    public async Task<IReadOnlyList<AgentResponseDto>> GetPendingAsync() => (await _agentRepository.GetPendingAsync()).Select(MapToResponseDto).ToList();
    public async Task<AgentResponseDto?> ApproveAsync(Guid id) { var a=await _agentRepository.GetByIdAsync(id); if(a is null || a.Status!=AgentStatus.Pending) return null; a.Status=AgentStatus.Approved; a.ApprovedAt=DateTime.UtcNow; await _agentRepository.UpdateAsync(a); return MapToResponseDto(a); }
    public async Task<AgentResponseDto?> RejectAsync(Guid id)
    {
        var a = await _agentRepository.GetByIdAsync(id);
        if (a is null || a.Status != AgentStatus.Pending) return null;
        a.Status = AgentStatus.Rejected;
        a.RejectedAt = DateTime.UtcNow;
        a.BootstrapSecretHash = null;
        await _agentRepository.UpdateAsync(a);
        await ClearStationAssignmentAsync(a);
        return MapToResponseDto(a);
    }
    public async Task<AgentResponseDto?> RevokeAsync(Guid id)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent is null) return null;
        agent.Status = AgentStatus.Revoked;
        agent.RevokedAt = DateTime.UtcNow;
        agent.BootstrapSecretHash = null;
        await _refreshTokenRepository.RevokeAllByAgentIdAsync(id);
        await _agentRepository.UpdateAsync(agent);
        await ClearStationAssignmentAsync(agent);
        return MapToResponseDto(agent);
    }

    private async Task ClearStationAssignmentAsync(Agent agent)
    {
        var station = await _stationRepository.GetByIdAsync(agent.StationId);
        if (station?.AgentId != agent.Id) return;
        station.AgentId = null;
        station.UpdatedAt = DateTime.UtcNow;
        await _stationRepository.UpdateAsync(station);
    }
    public async Task<AgentTokenResponseDto?> ExchangeBootstrapAsync(Guid id, string secret, string? ipAddress = null)
    {
        var gate = BootstrapLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
        if (string.IsNullOrWhiteSpace(secret))
            return null;

        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent is null || agent.Status != AgentStatus.Approved || string.IsNullOrWhiteSpace(agent.BootstrapSecretHash))
            return null;

        byte[] expected;
        byte[] supplied;
        try
        {
            expected = Convert.FromBase64String(agent.BootstrapSecretHash);
            supplied = Convert.FromBase64String(SecurityUtils.HashToken(secret));
        }
        catch (FormatException)
        {
            return null;
        }
        if (!CryptographicOperations.FixedTimeEquals(expected, supplied))
            return null;

        // Keep Approved until the authenticated SignalR connection is established.
        var tokens = await IssueTokensAsync(id, ipAddress);
        agent.BootstrapSecretHash = null;
        await _agentRepository.UpdateAsync(agent);
        return tokens;
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<AgentResponseDto?> HeartbeatAsync(Guid id)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent == null) return null;

        var now = DateTime.UtcNow;
        agent.LastHeartbeatAt = now;
        agent.Status = AgentStatus.Online;
        await _agentRepository.UpdateAsync(agent);

        var station = await _stationRepository.GetByIdAsync(agent.StationId);
        if (station != null)
        {
            station.LastSeenAt = now;
            // Only recover from Offline — never overwrite InUse/Reserved/Locked/Maintenance
            if (station.Status == StationStatus.Offline)
                station.Status = StationStatus.Available;
            station.UpdatedAt = now;
            await _stationRepository.UpdateAsync(station);
        }

        return MapToResponseDto(agent);
    }

    public async Task<AgentResponseDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        return agent is null ? null : MapToResponseDto(agent);
    }

    public async Task<AgentResponseDto?> ConnectAsync(Guid agentId, Guid stationId, CancellationToken ct = default)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId);
        return agent is null || agent.StationId != stationId
            ? null
            : await HeartbeatAsync(agentId);
    }

    public async Task<bool> DisconnectAsync(Guid id, CancellationToken ct = default)
    {
        var agent = await _agentRepository.GetByIdAsync(id);
        if (agent == null) return false;

        var now = DateTime.UtcNow;
        agent.Status = AgentStatus.Offline;
        agent.LastHeartbeatAt = now;
        await _agentRepository.UpdateAsync(agent);

        var station = await _stationRepository.GetByIdAsync(agent.StationId);
        if (station != null)
        {
            // Only set Offline if station was Available — don't interrupt InUse/Reserved/Locked/Maintenance
            if (station.Status == StationStatus.Available)
                station.Status = StationStatus.Offline;
            station.UpdatedAt = now;
            await _stationRepository.UpdateAsync(station);
        }

        return true;
    }

    public async Task<AgentTokenResponseDto> IssueTokensAsync(Guid agentId, string? ipAddress = null)
    {
        var agent = await _agentRepository.GetByIdAsync(agentId)
            ?? throw new KeyNotFoundException("Agent not found.");

        var station = await _stationRepository.GetByIdAsync(agent.StationId)
            ?? throw new KeyNotFoundException("Station not found.");

        if (!CanAuthenticate(agent.Status))
            throw new UnauthorizedAccessException("Agent is not approved or has been revoked.");

        var accessToken = _jwtService.GenerateAgentAccessToken(agentId);
        var rawRefreshToken = _jwtService.GenerateRefreshToken();
        var tokenHash = SecurityUtils.HashToken(rawRefreshToken);

        var refreshToken = new AgentRefreshToken
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedByIp = ipAddress
        };

        await _refreshTokenRepository.CreateAsync(refreshToken);

        return new AgentTokenResponseDto(
            agentId,
            station.Id,
            accessToken,
            rawRefreshToken,
            DateTime.UtcNow.AddMinutes(15)
        );
    }

    public async Task<AgentTokenResponseDto?> RefreshTokenAsync(string rawRefreshToken, string? ipAddress = null)
    {
        var tokenHash = SecurityUtils.HashToken(rawRefreshToken);
        var stored = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (stored == null)
            return null;

        // Reuse detection: token already revoked → revoke all agent tokens (replay attack)
        if (stored.RevokedAt != null)
        {
            await _refreshTokenRepository.RevokeAllByAgentIdAsync(stored.AgentId);
            return null;
        }

        if (stored.ExpiresAt <= DateTime.UtcNow)
            return null;

        var agent = await _agentRepository.GetByIdAsync(stored.AgentId);
        if (agent == null || !CanAuthenticate(agent.Status))
            return null;

        var station = await _stationRepository.GetByIdAsync(agent.StationId);
        if (station == null)
            return null;

        // Rotate: revoke old, issue new
        var newRaw = _jwtService.GenerateRefreshToken();
        var newHash = SecurityUtils.HashToken(newRaw);
        var newToken = new AgentRefreshToken
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            TokenHash = newHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedByIp = ipAddress
        };

        stored.RevokedAt = DateTime.UtcNow;
        stored.ReplacedByTokenId = newToken.Id;
        await _refreshTokenRepository.UpdateAsync(stored);
        await _refreshTokenRepository.CreateAsync(newToken);

        var accessToken = _jwtService.GenerateAgentAccessToken(agent.Id);

        return new AgentTokenResponseDto(
            agent.Id,
            station.Id,
            accessToken,
            newRaw,
            DateTime.UtcNow.AddMinutes(15)
        );
    }

    public async Task RevokeRefreshTokenAsync(string rawRefreshToken)
    {
        var tokenHash = SecurityUtils.HashToken(rawRefreshToken);
        var stored = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);
        if (stored != null && stored.RevokedAt == null)
        {
            stored.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(stored);
        }
    }

    private static AgentResponseDto MapToResponseDto(Agent agent)
    {
        return new AgentResponseDto(
            agent.Id,
            agent.StationId,
            agent.MachineName,
            agent.Version,
            agent.Status.ToString(),
            agent.LastHeartbeatAt
        );
    }

    private static bool CanAuthenticate(AgentStatus status) => status is
        AgentStatus.Approved or AgentStatus.Online or AgentStatus.Offline;
}
