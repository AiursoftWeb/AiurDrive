using Aiursoft.AiurDrive.Entities;
using Aiursoft.DbTools;
using Aiursoft.DbTools.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.AiurDrive.Sqlite;

public class SqliteSupportedDb(bool allowCache, bool splitQuery) : SupportedDatabaseType<AiurDriveDbContext>
{
    public override string DbType => "Sqlite";

    public override IServiceCollection RegisterFunction(IServiceCollection services, string connectionString)
    {
        return services.AddAiurSqliteWithCache<SqliteContext>(
            connectionString,
            splitQuery: splitQuery,
            allowCache: allowCache);
    }

    public override AiurDriveDbContext ContextResolver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<SqliteContext>();
    }
}