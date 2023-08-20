using Aiursoft.AiurDrive.Data;
using Aiursoft.AiurDrive.Models;
using Aiursoft.Identity;
using Aiursoft.SDK;
using Microsoft.AspNetCore.Identity;

namespace Aiursoft.AiurDrive
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextForInfraApps<AiurDriveDbContext>(Configuration.GetConnectionString("DatabaseConnection"));

            services.AddIdentity<AiurDriveUser, IdentityRole>()
                .AddEntityFrameworkStores<AiurDriveDbContext>()
                .AddDefaultTokenProviders();

            services.AddAiurMvc();

            services.AddAiursoftIdentity<AiurDriveUser>(
                probeConfig: Configuration.GetSection("AiursoftProbe"),
                authenticationConfig: Configuration.GetSection("AiursoftAuthentication"),
                observerConfig: Configuration.GetSection("AiursoftObserver"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseAiursoftHandler(env.IsDevelopment());
            app.UseAiursoftAppRouters();
        }
    }
}
