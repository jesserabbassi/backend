using Microsoft.EntityFrameworkCore;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.MonitoringAlerts.Models;

namespace NinetyBackend.Modules.MonitoringAlerts.Repositories;

public class TelemetryRepository(ApplicationDbContext db) : ITelemetryRepository
{
    public async Task AddAsync(Telemetry telemetry, CancellationToken ct = default)
    {
        db.Telemetries.Add(telemetry);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<Telemetry>> GetHistoryAsync(Guid stationId, int take, CancellationToken ct = default)
    {
        return await db.Telemetries
            .Where(t => t.StationId == stationId)
            .OrderByDescending(t => t.Timestamp)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(ct);
    }

    public async Task<Telemetry?> GetLatestAsync(Guid stationId, CancellationToken ct = default)
    {
        return await db.Telemetries
            .Where(t => t.StationId == stationId)
            .OrderByDescending(t => t.Timestamp)
            .FirstOrDefaultAsync(ct);
    }
}

public class AlertRepository(ApplicationDbContext db) : IAlertRepository
{
    public async Task<Alert> AddAsync(Alert alert, CancellationToken ct = default)
    {
        db.Alerts.Add(alert);
        await db.SaveChangesAsync(ct);
        return alert;
    }

    public async Task<List<Alert>> GetActiveAsync(Guid? stationId, CancellationToken ct = default)
    {
        var query = db.Alerts.AsQueryable();
        if (stationId.HasValue)
        {
            query = query.Where(a => a.StationId == stationId.Value && a.ResolvedAt == null);
        }
        else
        {
            query = query.Where(a => a.ResolvedAt == null);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Alert?> GetByIdAsync(Guid alertId, CancellationToken ct = default)
    {
        return await db.Alerts.FirstOrDefaultAsync(a => a.Id == alertId, ct);
    }

    public async Task UpdateAsync(Alert alert, CancellationToken ct = default)
    {
        db.Alerts.Update(alert);
        await db.SaveChangesAsync(ct);
    }
}
