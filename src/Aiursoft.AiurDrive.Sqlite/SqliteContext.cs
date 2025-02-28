using Aiursoft.AiurDrive.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Sqlite;

public class SqliteContext(DbContextOptions<SqliteContext> options) : AiurDriveDbContext(options)
{
    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}