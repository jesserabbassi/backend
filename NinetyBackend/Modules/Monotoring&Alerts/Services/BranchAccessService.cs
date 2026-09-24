using System.Security.Claims;
using NinetyGamingStationBackend.Application.Interfaces;

namespace NinetyGamingStationBackend.Application.Services;

public sealed class BranchAccessService : IBranchAccessService
{
    public bool CanAccess(ClaimsPrincipal user, Guid branchId)
    {
        var role = user.FindFirst(ClaimTypes.Role)?.Value ?? user.FindFirst("role")?.Value;
        if (string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
            return true;

        var claim = user.FindFirst("branch_id")?.Value;
        return Guid.TryParse(claim, out var userBranchId) && userBranchId == branchId;
    }
}
