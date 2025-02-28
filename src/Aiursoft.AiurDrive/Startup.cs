using Aiursoft.AiurDrive.Entities;
using Aiursoft.AiurDrive.InMemory;
using Aiursoft.AiurDrive.Models;
using Aiursoft.AiurDrive.MySql;
using Aiursoft.AiurDrive.Services;
using Aiursoft.AiurDrive.Sqlite;
using Aiursoft.Canon;
using Aiursoft.CSTools.Services;
using Aiursoft.CSTools.Tools;
using Aiursoft.WebTools.Abstractions.Models;
using Microsoft.AspNetCore.Identity;
using Aiursoft.DbTools.Switchable;
using Aiursoft.WebTools.Services;
using Microsoft.AspNetCore.Http.Features;

namespace Aiursoft.AiurDrive
{
    public class Startup : IWebStartup
    {
        public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
        {
            // Configure big file upload.
            services.Configure<FormOptions>(options =>
            {
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartBodyLengthLimit = int.MaxValue;
                options.MultipartHeadersLengthLimit = int.MaxValue;
            });

            var (connectionString, dbType, allowCache) = configuration.GetDbSettings();
            services.AddSwitchableRelationalDatabase(
                dbType: EntryExtends.IsInUnitTests() ? "InMemory": dbType,
                connectionString: connectionString,
                supportedDbs:
                [
                    new MySqlSupportedDb(allowCache: allowCache, splitQuery: false),
                    new SqliteSupportedDb(allowCache: allowCache, splitQuery: true),
                    new InMemorySupportedDb()
                ]);


            services.AddMemoryCache();
            services.AddSingleton<IHostedService, TimedCleaner>();
            services.AddTransient<QRCodeService>();

            services.AddIdentity<AiurDriveUser, IdentityRole>(options => options.Password = new PasswordOptions
                {
                    RequireNonAlphanumeric = false,
                    RequireDigit = false,
                    RequiredLength = 6,
                    RequiredUniqueChars = 0,
                    RequireLowercase = false,
                    RequireUppercase = false
                })
                .AddEntityFrameworkStores<AiurDriveDbContext>()
                .AddDefaultTokenProviders();

            services.AddTaskCanon();
            services.AddScoped<UpScaleService>();
            services.AddScoped<CommandService>();

            services.AddTransient<StorageService>();
            services.AddControllersWithViews().AddApplicationPart(typeof(Startup).Assembly);
        }

        public void Configure(WebApplication app)
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapDefaultControllerRoute();
        }
    }
}
