using System.Diagnostics.CodeAnalysis;
using Aiursoft.DbTools;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Entities;

[ExcludeFromCodeCoverage]

public abstract class AiurDriveDbContext(DbContextOptions options) : IdentityDbContext<User>(options), ICanMigrate
{
    public DbSet<GlobalSetting> GlobalSettings => Set<GlobalSetting>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<SiteShare> SiteShares => Set<SiteShare>();
    public virtual  Task MigrateAsync(CancellationToken cancellationToken) =>
        Database.MigrateAsync(cancellationToken);

    public virtual  Task<bool> CanConnectAsync() =>
        Database.CanConnectAsync();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Site>()
            .HasOne(m => m.AppUser)
            .WithMany(u => u.Sites)
            .HasForeignKey(m => m.AppUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
