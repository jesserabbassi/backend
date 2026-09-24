using System.Security.Cryptography;
using NinetyGamingStationBackend.Application.DTOs.Stations;
using NinetyGamingStationBackend.Application.Interfaces;
using NinetyGamingStationBackend.Domain.Entities;
using NinetyGamingStationBackend.Domain.Enums;
using NinetyGamingStationBackend.Realtime;

namespace NinetyGamingStationBackend.Application.Services;

public sealed class StationService(IStationRepository repository, AgentConnectionManager connections) : IStationService
{
    public async Task<List<StationDto>> GetAllAsync(Guid? branchId, CancellationToken ct)
        => (await repository.GetAllAsync(branchId, ct))
            .Select(ToDto).ToList();

    public async Task<StationDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var s = await repository.GetByIdAsync(id, ct);
        return s is null ? null : ToDto(s);
    }

    public async Task<RegisterStationResultDto> RegisterAsync(RegisterStationDto dto, CancellationToken ct)
    {
        var existing = await repository.GetByMachineIdAsync(dto.MachineId.Trim(), ct);
        if (existing is not null)
            throw new InvalidOperationException("A station with this machine_id already exists.");

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var station = new GamingStation
        {
            BranchId = dto.BranchId,
            Name = dto.Name.Trim(),
            MachineId = dto.MachineId.Trim(),
            IpAddress = dto.IpAddress.Trim(),
            MacAddress = dto.MacAddress.Trim().ToUpperInvariant(),
            Status = StationStatus.Offline,
            AgentTokenHash = AgentTokenHasher.Hash(rawToken),
            LastHeartbeat = DateTime.UtcNow
        };
        await repository.AddAsync(station, ct);
        return new RegisterStationResultDto(ToDto(station), rawToken);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateStationDto dto, CancellationToken ct)
    {
        var s = await repository.GetByIdAsync(id, ct);
        if (s is null) return false;
        s.Name = dto.Name.Trim();
        s.BranchId = dto.BranchId;
        s.IpAddress = dto.IpAddress.Trim();
        s.MacAddress = dto.MacAddress.Trim().ToUpperInvariant();
        if (s.Status != StationStatus.Offline || dto.Status == StationStatus.Offline)
            s.Status = dto.Status;
        s.UpdatedAt = DateTime.UtcNow;
        await repository.UpdateAsync(s, ct);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct)
    {
        var s = await repository.GetByIdAsync(id, ct);
        if (s is null) return false;
        await repository.DeleteAsync(s, ct);
        connections.Disconnect(id.ToString());
        return true;
    }

    public async Task UpdateStatusAsync(Guid id, StationStatus status, DateTime heartbeatAt, CancellationToken ct)
    {
        var s = await repository.GetByIdAsync(id, ct);
        if (s is null) return;
        s.Status = status;
        s.LastHeartbeat = heartbeatAt;
        s.UpdatedAt = DateTime.UtcNow;
        await repository.UpdateAsync(s, ct);
    }

    private StationDto ToDto(GamingStation s) =>
        new(s.Id, s.BranchId, s.Name, s.MachineId, s.IpAddress, s.MacAddress,
            s.Status, s.LastHeartbeat, s.AgentVersion, connections.IsConnected(s.Id.ToString()));
}

internal static class AgentTokenHasher
{
    public static string Hash(string token)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(token, salt, 120_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string token, string stored)
    {
        try
        {
            var parts = stored.Split('.', 2);
            if (parts.Length != 2) return false;
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(token, salt, 120_000, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch { return false; }
    }
}
