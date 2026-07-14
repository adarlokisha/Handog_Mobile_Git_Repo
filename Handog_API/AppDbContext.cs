using Microsoft.EntityFrameworkCore;
using Handog_API.Models;

namespace Handog_API
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Maps to your ACCOUNT table
        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>().ToTable("ACCOUNT");
            modelBuilder.Entity<Account>()
                .Property(a => a.AbsenceCount)
                .HasDefaultValue(0);
        }
    }
}
