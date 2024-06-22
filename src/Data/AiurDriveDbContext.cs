using Aiursoft.AiurDrive.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Data
{
    public class AiurDriveDbContext(DbContextOptions<AiurDriveDbContext> options)
        : IdentityDbContext<AiurDriveUser>(options);
}
