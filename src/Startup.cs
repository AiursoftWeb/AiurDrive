using Aiursoft.AiurDrive.Data;
using Aiursoft.AiurDrive.Models;
using Aiursoft.Identity;
using Aiursoft.SDK;
using Aiursoft.WebTools.Models;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.AiurDrive
{
    public class Startup : IWebStartup
    {
        public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
        {
            services.AddDbContextForInfraApps<AiurDriveDbContext>(configuration.GetConnectionString("DatabaseConnection"));

            services.AddIdentity<AiurDriveUser, IdentityRole>()
                .AddEntityFrameworkStores<AiurDriveDbContext>()
                .AddDefaultTokenProviders();

            services.AddAiurosftWebFeatures();

            services.AddAiursoftIdentity<AiurDriveUser>(
                probeConfig: configuration.GetSection("AiursoftProbe"),
                authenticationConfig: configuration.GetSection("AiursoftAuthentication"),
                observerConfig: configuration.GetSection("AiursoftObserver"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseAiursoftHandler(env.IsDevelopment());
            app.UseAiursoftAppRouters();
        }
    }
}
