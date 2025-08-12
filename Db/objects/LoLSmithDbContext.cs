using Microsoft.EntityFrameworkCore;

namespace LoLSmith.Db;

public class LoLSmithDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public LoLSmithDbContext(DbContextOptions<LoLSmithDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Puuid)
            .IsUnique();
    }
}