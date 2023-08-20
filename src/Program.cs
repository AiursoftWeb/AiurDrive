using Aiursoft.AiurDrive.Data;
using Aiursoft.DbTools;
using Aiursoft.Directory.SDK.Services;
using Aiursoft.Probe.SDK;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.AiurDrive
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var app = App<Startup>(args);
            await app.UpdateDbAsync<AiurDriveDbContext>(UpdateMode.MigrateThenUse);
            await app.InitSiteAsync<DirectoryAppTokenService>(c => c["AiurDrivePublicSiteName"], a => a.GetAccessTokenAsync());
            await app.RunAsync();
        }
    }
}
