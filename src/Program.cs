using AiurDrive.Data;
using Aiursoft.Gateway.SDK.Services;
using Aiursoft.Probe.SDK;
using Aiursoft.SDK;
using Microsoft.Extensions.Hosting;
using static Aiursoft.WebTools.Extends;

namespace AiurDrive
{
    public class Program
    {
        public static void Main(string[] args)
        {
            App<Startup>(args)
                .Update<AiurDriveDbContext>()
                .InitSite<AppsContainer>(c => c["AiurDrivePublicSiteName"], a => a.AccessTokenAsync())
                .Run();
        }

        // For EF
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return BareApp<Startup>(args);
        }
    }
}
