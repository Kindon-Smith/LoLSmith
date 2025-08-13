using Microsoft.EntityFrameworkCore;

namespace LoLSmith.Db;

public class LoLSmithDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<Match> Matches { get; set; }

    public DbSet<UserMatches> UserMatches { get; set; }

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
                m.Participants,
                m.MatchId
            })
            .IsUnique();
        modelBuilder.Entity<UserMatches>()
            .HasOne(um => um.User)
            .WithMany(u => u.UserMatches)
            .HasForeignKey(um => um.UserId);
        modelBuilder.Entity<UserMatches>()
            .HasOne(um => um.Match)
            .WithMany(m => m.UserMatches)
            .HasForeignKey(um => um.MatchId);
    }
}