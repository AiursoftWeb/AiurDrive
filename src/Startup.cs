﻿using Aiursoft.AiurDrive.Data;
using Aiursoft.AiurDrive.Models;
using Aiursoft.AiurDrive.Services;
using Aiursoft.Canon;
using Aiursoft.CSTools.Services;
using Aiursoft.WebTools.Abstractions.Models;
using Microsoft.AspNetCore.Identity;
using Aiursoft.DbTools.Sqlite;
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
            
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddMemoryCache();
            services.AddAiurSqliteWithCache<AiurDriveDbContext>(connectionString);
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
