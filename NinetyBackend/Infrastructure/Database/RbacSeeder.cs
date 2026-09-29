using Microsoft.EntityFrameworkCore;
using NinetyBackend.Modules.Auth.Authorization;
using NinetyBackend.Modules.Auth.Models;

namespace NinetyBackend.Infrastructure.Database;

public static class RbacSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db)
    {
        await db.Database.MigrateAsync();

        var permissions = await db.Permissions.ToListAsync();
        foreach (var name in RbacDefinitions.Permissions)
        {
            if (permissions.All(p => !p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                var permission = new Permission { Name = name };
                db.Permissions.Add(permission);
                permissions.Add(permission);
            }
        }

        var roles = await db.Roles.Include(r => r.Permissions).ToListAsync();
        foreach (var (roleName, permissionNames) in RbacDefinitions.RolePermissions)
        {
            var role = roles.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            if (role == null)
            {
                role = new Role { Name = roleName };
                db.Roles.Add(role);
                roles.Add(role);
            }

            role.Permissions = permissions
                .Where(p => permissionNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        await db.SaveChangesAsync();
    }
}
