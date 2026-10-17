using System.ComponentModel.DataAnnotations;

namespace JawsOfDeath.Backend.Models;

public class Player
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string Username { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ScoreEntry> ScoreEntries { get; set; } = new();
}