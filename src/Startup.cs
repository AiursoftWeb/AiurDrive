﻿using AiurDrive.Data;
using AiurDrive.Models;
using Aiursoft.Archon.SDK.Services;
using Aiursoft.Identity;
using Aiursoft.SDK;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AiurDrive
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            AppsContainer.CurrentAppId = configuration["AiurDriveAppId"];
            AppsContainer.CurrentAppSecret = configuration["AiurDriveAppSecret"];
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextWithCache<AiurDriveDbContext>(Configuration.GetConnectionString("DatabaseConnection"));

            services.AddIdentity<AiurDriveUser, IdentityRole>()
                .AddEntityFrameworkStores<AiurDriveDbContext>()
                .AddDefaultTokenProviders();

            services.AddAiurMvc();

            services.AddAiursoftIdentity<AiurDriveUser>(
                archonEndpoint: Configuration.GetConnectionString("ArchonConnection"),
                observerEndpoint: Configuration.GetConnectionString("ObserverConnection"),
                probeEndpoint: Configuration.GetConnectionString("ProbeConnection"),
                gateEndpoint: Configuration.GetConnectionString("GatewayConnection"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseAiurUserHandler(env.IsDevelopment());
            app.UseAiursoftDefault();
        }
    }
}
