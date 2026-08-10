using Microsoft.EntityFrameworkCore;
using JawsOfDeath.Backend.Models;

namespace JawsOfDeath.Backend.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

        public DbSet<Player> Players => Set<Player>();
        public DbSet<ScoreEntry> ScoreEntries => Set<ScoreEntry>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>()
                .HasMany(p => p.ScoreEntries)
                .WithOne(s => s.Player)
                .HasForeignKey(s => s.PlayerId);
        }
    }
}
