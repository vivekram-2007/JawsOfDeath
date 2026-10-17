namespace JawsOfDeath.Backend.Models;

public class ScoreEntry
{
    public int Id { get; set; }

    public int PlayerId { get; set; }
    public Player? Player { get; set; }

    public int Kills { get; set; }
    public int Deaths { get; set; }
    public double TimeToFirstOrb { get; set; }
    public double? TimeToBossKill { get; set; }
    public double SurvivalTime { get; set; }

    public int Score { get; set; }
    public bool Won { get; set; }
    public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
}