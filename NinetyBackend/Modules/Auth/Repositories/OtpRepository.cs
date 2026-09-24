using Microsoft.EntityFrameworkCore;
using NinetyBackend.Infrastructure.Database;
using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Modules.Auth.Repositories;

public class OtpRepository : IOtpRepository
{
    private readonly ApplicationDbContext _db;

    public OtpRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<OtpVerification?> GetLatestByUserIdAndPurposeAsync(Guid userId, OtpPurpose purpose)
    {
        return _db.OtpVerifications
            .Where(x => x.UserId == userId && x.Purpose == purpose && !x.Verified)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task InvalidateActiveOtpsAsync(Guid userId, OtpPurpose purpose)
    {
        var activeOtps = await _db.OtpVerifications
            .Where(x => x.UserId == userId && x.Purpose == purpose && !x.Verified && x.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var otp in activeOtps)
        {
            otp.Verified = true; // mark as used so they can't be replayed
        }

        if (activeOtps.Count > 0)
        {
            await _db.SaveChangesAsync();
        }
    }

    public async Task CreateAsync(OtpVerification otp)
    {
        _db.OtpVerifications.Add(otp);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(OtpVerification otp)
    {
        _db.OtpVerifications.Update(otp);
        await _db.SaveChangesAsync();
    }
}
