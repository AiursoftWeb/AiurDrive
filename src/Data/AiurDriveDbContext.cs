using Aiursoft.AiurDrive.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.AiurDrive.Data
{
    public class AiurDriveDbContext : IdentityDbContext<AiurDriveUser>
    {
        public AiurDriveDbContext(DbContextOptions<AiurDriveDbContext> options)
            : base(options)
        {

        }
    }
}
