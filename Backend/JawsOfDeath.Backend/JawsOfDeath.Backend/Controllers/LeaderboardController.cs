using JawsOfDeath.Backend.Data;
using JawsOfDeath.Backend.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JawsOfDeath.Backend.Controllers;

[ApiController]
[Route("api/leaderboard")]
public class LeaderboardController : ControllerBase
{
    private readonly GameDbContext _db;

    public LeaderboardController(GameDbContext db)
    {
        _db = db;
    }

    // GET /api/leaderboard  ->  { "entries": [ ... ] }  (top 10 by score)
    [HttpGet]
    public async Task<ActionResult<LeaderboardResponse>> GetLeaderboard()
    {
        var entries = await _db.ScoreEntries
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.PlayedAt)
            .Take(10)
            .Select(s => new LeaderboardEntryDto
            {
                Username = s.Player!.Username,
                Score = s.Score,
                Kills = s.Kills,
                Deaths = s.Deaths,
                SurvivalTime = s.SurvivalTime,
                Won = s.Won
            })
            .ToListAsync();

        return Ok(new LeaderboardResponse { Entries = entries });
    }
}