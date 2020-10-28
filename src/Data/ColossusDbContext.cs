using AiurDrive.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AiurDrive.Data
{
    public class ColossusDbContext : IdentityDbContext<ColossusUser>
    {
        public ColossusDbContext(DbContextOptions<ColossusDbContext> options)
            : base(options)
        {

        }
    }
}
