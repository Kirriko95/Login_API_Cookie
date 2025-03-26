using Microsoft.EntityFrameworkCore;
using Grupp3_Login_API.Models;

namespace Grupp3_Login_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Role> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, RoleName = "Admin" },
                new Role { Id = 2, RoleName = "Employee" },
                new Role { Id = 3, RoleName = "User" }
            );
        }
    }
}
