using JawsOfDeath.Backend.Data;
using JawsOfDeath.Backend.Dtos;
using JawsOfDeath.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JawsOfDeath.Backend.Controllers;

[ApiController]
[Route("api/players")]
public class PlayersController : ControllerBase
{
    private readonly GameDbContext _db;

    public PlayersController(GameDbContext db)
    {
        _db = db;
    }

    // POST /api/players  body: { "username": "Ashlu" }
    // Creates the player, or returns the existing one (case-insensitive).
    [HttpPost]
    public async Task<ActionResult<PlayerResponse>> CreateOrGetPlayer(CreatePlayerRequest request)
    {
        var username = (request.Username ?? string.Empty).Trim();

        if (username.Length == 0)
            return BadRequest("Username is required.");
        if (username.Length > 20)
            return BadRequest("Username must be 20 characters or fewer.");

        // The Username column ignores upper/lower case, so "ashlu" finds "Ashlu"
        var player = await _db.Players.FirstOrDefaultAsync(p => p.Username == username);

        if (player == null)
        {
            player = new Player { Username = username };
            _db.Players.Add(player);
            await _db.SaveChangesAsync();
        }

        return Ok(new PlayerResponse { Id = player.Id, Username = player.Username });
    }

    // GET /api/players/5/stats
    [HttpGet("{id}/stats")]
    public async Task<ActionResult<PlayerStatsResponse>> GetStats(int id)
    {
        var player = await _db.Players.FindAsync(id);
        if (player == null)
            return NotFound($"Player {id} not found.");

        var scores = await _db.ScoreEntries
            .Where(s => s.PlayerId == id)
            .ToListAsync();

        var response = new PlayerStatsResponse
        {
            Id = player.Id,
            Username = player.Username,
            GamesPlayed = scores.Count,
            TotalKills = scores.Sum(s => s.Kills),
            TotalDeaths = scores.Sum(s => s.Deaths),
            BestScore = scores.Count > 0 ? scores.Max(s => s.Score) : 0,
            FastestFirstOrb = scores.Count > 0 ? scores.Min(s => s.TimeToFirstOrb) : 0,
            FastestBossKill = scores.Where(s => s.TimeToBossKill != null).Min(s => s.TimeToBossKill),
            Wins = scores.Count(s => s.Won)
        };

        return Ok(response);
    }
}