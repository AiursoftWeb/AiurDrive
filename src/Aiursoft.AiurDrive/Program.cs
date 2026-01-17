using System.Diagnostics.CodeAnalysis;
using Aiursoft.DbTools;
using Aiursoft.AiurDrive.Entities;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.AiurDrive;

[ExcludeFromCodeCoverage]
public abstract class Program
{
    public static async Task Main(string[] args)
    {
        var app = await AppAsync<Startup>(args);
        await app.UpdateDbAsync<TemplateDbContext>();
        await app.SeedAsync();
        await app.CopyAvatarFileAsync();
        await app.RunAsync();
    }
}
