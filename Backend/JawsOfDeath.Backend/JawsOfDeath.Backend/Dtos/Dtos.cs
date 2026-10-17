using System.ComponentModel.DataAnnotations;

namespace JawsOfDeath.Backend.Dtos;

// ---------- Requests (what Unity sends) ----------

public class CreatePlayerRequest
{
    [Required]
    [MaxLength(20)]
    public string Username { get; set; } = string.Empty;
}

public class CreateScoreRequest
{
    public int PlayerId { get; set; }

    [Range(0, int.MaxValue)]
    public int Kills { get; set; }

    [Range(0, int.MaxValue)]
    public int Deaths { get; set; }

    [Range(0, double.MaxValue)]
    public double TimeToFirstOrb { get; set; }

    [Range(0, double.MaxValue)]
    public double? TimeToBossKill { get; set; }

    [Range(0, double.MaxValue)]
    public double SurvivalTime { get; set; }

    public bool Won { get; set; }
}

// ---------- Responses (what the API sends back) ----------

public class PlayerResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class ScoreResponse
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public double TimeToFirstOrb { get; set; }
    public double? TimeToBossKill { get; set; }
    public double SurvivalTime { get; set; }
    public int Score { get; set; }
    public bool Won { get; set; }
    public DateTime PlayedAt { get; set; }
}

public class LeaderboardEntryDto
{
    public string Username { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public double SurvivalTime { get; set; }
    public bool Won { get; set; }
}

// Unity cannot read a top-level array, so the list is wrapped in an object
public class LeaderboardResponse
{
    public List<LeaderboardEntryDto> Entries { get; set; } = new();
}

public class PlayerStatsResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public int GamesPlayed { get; set; }
    public int TotalKills { get; set; }
    public int TotalDeaths { get; set; }
    public int BestScore { get; set; }
    public double FastestFirstOrb { get; set; }
    public double? FastestBossKill { get; set; }
    public int Wins { get; set; }
}