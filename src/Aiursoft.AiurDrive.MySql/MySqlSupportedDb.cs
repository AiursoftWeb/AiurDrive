using Aiursoft.AiurDrive.Entities;
using Aiursoft.DbTools;
using Aiursoft.DbTools.MySql;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.AiurDrive.MySql;

public class MySqlSupportedDb(bool allowCache, bool splitQuery) : SupportedDatabaseType<AiurDriveDbContext>
{
    public override string DbType => "MySql";

    public override IServiceCollection RegisterFunction(IServiceCollection services, string connectionString)
    {
        return services.AddAiurMySqlWithCache<MySqlContext>(
            connectionString,
            splitQuery: splitQuery,
            allowCache: allowCache);
    }

    public override AiurDriveDbContext ContextResolver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<MySqlContext>();
    }
}