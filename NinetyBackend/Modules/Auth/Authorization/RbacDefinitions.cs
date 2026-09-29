namespace NinetyBackend.Modules.Auth.Authorization;

public static class RbacDefinitions
{
    public const string CustomerRole = "Customer";
    public const string AdminRole = "Admin";
    public const string SupervisorRole = "Supervisor";

    public static readonly string[] Permissions =
    [
        "users.read", "users.create", "users.update", "users.delete",
        "roles.manage", "stations.read", "stations.manage",
        "wallets.read", "wallets.credit", "wallets.debit", "wallets.refund",
        "monitoring.read"
    ];

    public static readonly IReadOnlyDictionary<string, string[]> RolePermissions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [CustomerRole] = ["wallets.read", "wallets.credit", "wallets.debit"],
            [SupervisorRole] = ["stations.read", "stations.manage", "monitoring.read", "wallets.read"],
            [AdminRole] = Permissions
        };
}
