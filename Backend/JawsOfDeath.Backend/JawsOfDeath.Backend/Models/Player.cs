using System.ComponentModel.DataAnnotations;

namespace JawsOfDeath.Backend.Models
{
    public class Player
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nickname { get; set; } = string.Empty;

        // One player can have many recorded runs
        public List<ScoreEntry> ScoreEntries { get; set; } = new();
    }
}
