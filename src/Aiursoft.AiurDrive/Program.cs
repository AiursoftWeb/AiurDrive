using Aiursoft.AiurDrive.Entities;
using Aiursoft.DbTools;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.AiurDrive;

public abstract class Program
{
    public static async Task Main(string[] args)
    {
        var app = await AppAsync<Startup>(args);
        await app.UpdateDbAsync<AiurDriveDbContext>();
        await app.PullContainersAsync();
        await app.RunAsync();
    }
}
