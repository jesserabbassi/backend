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

    public Task<OtpVerification?> GetLatestByUserIdAsync(Guid userId)
    {
        return _db.OtpVerifications
            .Where(x => x.UserId == userId && !x.Verified)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
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
