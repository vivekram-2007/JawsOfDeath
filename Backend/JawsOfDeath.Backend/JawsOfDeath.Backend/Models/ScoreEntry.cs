using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JawsOfDeath.Backend.Models
{
    public class ScoreEntry
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Player))]
        public int PlayerId { get; set; }
        public Player? Player { get; set; }

        // Stat 1: how many zombies were killed this run
        public int ZombiesKilled { get; set; }

        // Stat 2: how many times the player died this run
        // (usually 1 for a single "run ends on death" flow, but kept as
        // an int in case you ever support respawns/multiple lives)
        public int DeathCount { get; set; }

        // Stat 3: seconds from game start to picking up Orb 1
        public double TimeToFirstOrb { get; set; }

        // Stat 4: seconds from game start to killing the boss
        public double TimeToKillBoss { get; set; }

        // Total time survived this run, in seconds
        public double TimeSurvived { get; set; }

        public DateTime DatePlayed { get; set; } = DateTime.UtcNow;
    }
}
