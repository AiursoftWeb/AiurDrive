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
            var app = App<Startup>(args);
            await app.UpdateDbAsync<AiurDriveDbContext>();
            await app.InitSiteAsync<DirectoryAppTokenService>(c => c["AiurDrivePublicSiteName"], a => a.GetAccessTokenAsync());
            await app.RunAsync();
        }
    }
}
