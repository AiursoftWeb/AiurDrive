using AiurDrive.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AiurDrive.Data
{
    public class AiurDriveDbContext : IdentityDbContext<AiurDriveUser>
    {
        public AiurDriveDbContext(DbContextOptions<AiurDriveDbContext> options)
            : base(options)
        {

        }
    }
}
