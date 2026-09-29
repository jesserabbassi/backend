using Microsoft.EntityFrameworkCore;
using NinetyBackend.Modules.Auth.Models;
using NinetyBackend.Modules.MonitoringAlerts.Models;
using NinetyBackend.Modules.Stations.Models;
using NinetyBackend.Modules.Wallets.Models;

namespace NinetyBackend.Infrastructure.Database;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<GamingStation> GamingStations => Set<GamingStation>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Telemetry> Telemetries => Set<Telemetry>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired();
        });

        modelBuilder.Entity<OtpVerification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                .WithMany(u => u.ExternalLogins)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<GamingStation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Code).IsRequired();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MachineName).IsRequired();
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<Telemetry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StationId, e.Timestamp });
            entity.Property(e => e.NetworkStatus).HasMaxLength(128);
            entity.Property(e => e.Timestamp).IsRequired();
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.StationId, e.Type, e.ResolvedAt });
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.Type).HasConversion<string>();
            entity.Property(e => e.Severity).HasConversion<string>();
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CustomerId).IsUnique();
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3).IsRequired();
            entity.Property(e => e.Version).IsConcurrencyToken();
            entity.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.WalletId, e.IdempotencyKey }).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.PreviousBalance).HasPrecision(18, 2);
            entity.Property(e => e.NewBalance).HasPrecision(18, 2);
            entity.Property(e => e.Type).HasConversion<string>();
            entity.HasOne(e => e.Wallet).WithMany(w => w.Transactions).HasForeignKey(e => e.WalletId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

