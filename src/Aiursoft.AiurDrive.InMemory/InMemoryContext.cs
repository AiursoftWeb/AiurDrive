using Aiursoft.AiurDrive.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.InMemory;

public class InMemoryContext(DbContextOptions<InMemoryContext> options) : AiurDriveDbContext(options)
{
    public override Task MigrateAsync(CancellationToken cancellationToken)
    {
        return Database.EnsureCreatedAsync(cancellationToken);
    }

    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
