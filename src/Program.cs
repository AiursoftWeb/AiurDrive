using Aiursoft.AiurDrive.Data;
using Aiursoft.DbTools;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.AiurDrive;

public abstract class Program
{
    public static async Task Main(string[] args)
    {
        var app = await AppAsync<Startup>(args);
        await app.UpdateDbAsync<AiurDriveDbContext>(UpdateMode.MigrateThenUse);
        await app.RunAsync();
    }
}
