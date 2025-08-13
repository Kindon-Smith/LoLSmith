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
        modelBuilder.Entity<Match>()
            .HasIndex(m => m.MatchId)
            .IsUnique();
        modelBuilder.Entity<Match>()
            .HasIndex(m => new
            {
                m.Participants, m.MatchId
            })
            .IsUnique();
    }
}