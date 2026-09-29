using NinetyBackend.Modules.Stations.DTOs;
using NinetyBackend.Modules.Stations.Models;
using NinetyBackend.Modules.Stations.Repositories;

namespace NinetyBackend.Modules.Stations.Services;

public class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IStationRepository _stationRepository;

    public AgentService(IAgentRepository agentRepository, IStationRepository stationRepository)
    {
        _agentRepository = agentRepository;
        _stationRepository = stationRepository;
    }

    public async Task<AgentResponseDto> RegisterAsync(RegisterAgentDto dto)
    {
        var station = await _stationRepository.GetByIdAsync(dto.StationId);
        if (station == null)
        {
            throw new InvalidOperationException("Gaming station not found.");
        }

        var now = DateTime.UtcNow;
        var agent = await _agentRepository.GetByStationIdAsync(dto.StationId);

        if (agent == null)
        {
            agent = new Agent
            {
                Id = Guid.NewGuid(),
                StationId = dto.StationId,
                MachineName = dto.MachineName,
                Version = dto.Version,
                Status = AgentStatus.Online,
                RegisteredAt = now,
                LastHeartbeatAt = now
            };
            await _agentRepository.AddAsync(agent);
        }
        else
        {
            agent.MachineName = dto.MachineName;
            agent.Version = dto.Version;
            agent.Status = AgentStatus.Online;
            agent.LastHeartbeatAt = now;
            await _agentRepository.UpdateAsync(agent);
        }

        station.AgentId = agent.Id;
        station.Status = StationStatus.Available;
        station.LastSeenAt = now;
        station.UpdatedAt = now;
        await _stationRepository.UpdateAsync(station);

        return MapToResponseDto(agent);
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
            if (station.Status == StationStatus.Offline)
            {
                station.Status = StationStatus.Available;
            }
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
        await _agentRepository.UpdateAsync(agent);

        var station = await _stationRepository.GetByIdAsync(agent.StationId);
        if (station != null)
        {
            station.Status = StationStatus.Offline;
            station.UpdatedAt = now;
            await _stationRepository.UpdateAsync(station);
        }

        return true;
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
}
