using AccountService.Models;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Data
{
    public class AccountRepository : DbContext
    {
        public AccountRepository(DbContextOptions<AccountRepository> options) : base(options) { }

        public DbSet<Profile> Profile { get; set; }

    }
}
