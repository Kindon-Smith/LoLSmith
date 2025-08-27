using Microsoft.EntityFrameworkCore;

namespace LoLSmith.Db;

public class LoLSmithDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<Match> Matches { get; set; }

    public DbSet<UserMatches> UserMatches { get; set; }

    // moved here: class-level DbSet for refresh tokens
    public DbSet<LoLSmith.Db.Entities.RefreshToken> RefreshTokens { get; set; }

    public LoLSmithDbContext(DbContextOptions<LoLSmithDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserMatches>()
            .HasKey(um => new { um.UserId, um.MatchId });

        modelBuilder.Entity<UserMatches>()
            .HasOne(um => um.User)
            .WithMany(u => u.UserMatches)
            .HasForeignKey(um => um.UserId);

        modelBuilder.Entity<UserMatches>()
            .HasOne(um => um.Match)
            .WithMany(m => m.UserMatches)
            .HasForeignKey(um => um.MatchId);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Puuid)
            .IsUnique();
        modelBuilder.Entity<Match>()
            .HasIndex(m => m.MatchId)
            .IsUnique();
    }
    
}