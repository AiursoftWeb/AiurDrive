using System.Diagnostics.CodeAnalysis;
using Aiursoft.AiurDrive.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Sqlite;

[ExcludeFromCodeCoverage]

public class SqliteContext(DbContextOptions<SqliteContext> options) : TemplateDbContext(options)
{
    public override Task<bool> CanConnectAsync()
    {
        return Task.FromResult(true);
    }
}
