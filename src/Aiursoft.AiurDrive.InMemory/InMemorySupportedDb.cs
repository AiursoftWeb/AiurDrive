using Aiursoft.AiurDrive.Entities;
using Aiursoft.DbTools;
using Aiursoft.DbTools.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Aiursoft.AiurDrive.InMemory;

public class InMemorySupportedDb : SupportedDatabaseType<AiurDriveDbContext>
{
    public override string DbType => "InMemory";

    public override IServiceCollection RegisterFunction(IServiceCollection services, string connectionString)
    {
        return services.AddAiurInMemoryDb<InMemoryContext>();
    }

    public override AiurDriveDbContext ContextResolver(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<InMemoryContext>();
    }
}