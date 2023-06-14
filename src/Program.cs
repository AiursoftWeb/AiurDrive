using System.Threading.Tasks;
using AiurDrive.Data;
using Aiursoft.Directory.SDK.Services;
using Aiursoft.Probe.SDK;
using Aiursoft.SDK;
using Microsoft.Extensions.Hosting;
using static Aiursoft.WebTools.Extends;

namespace AiurDrive
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await (await App<Startup>(args)
                .Update<AiurDriveDbContext>()
                .InitSite<AppsContainer>(c => c["AiurDrivePublicSiteName"], a => a.GetAccessTokenAsync()))
                .RunAsync();
        }

        // For EF
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return BareApp<Startup>(args);
        }
    }
}
