using Microsoft.EntityFrameworkCore;

namespace AccountService.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<ProfileData> Profile { get; set; }

    }
}
