using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JawsOfDeath.Backend.Data;
using JawsOfDeath.Backend.Models;

namespace JawsOfDeath.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScoresController : ControllerBase
    {
        private readonly GameDbContext _context;

        public ScoresController(GameDbContext context)
        {
            _context = context;
        }

        // POST /api/scores
        [HttpPost]
        public async Task<ActionResult<ScoreEntry>> CreateScore([FromBody] ScoreEntry scoreEntry)
        {
            var playerExists = await _context.Players.AnyAsync(p => p.Id == scoreEntry.PlayerId);
            if (!playerExists)
            {
                return BadRequest("PlayerId does not exist.");
            }

            scoreEntry.DatePlayed = DateTime.UtcNow;
            _context.ScoreEntries.Add(scoreEntry);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateScore), new { id = scoreEntry.Id }, scoreEntry);
        }

        // GET /api/scores/leaderboard
        [HttpGet("leaderboard")]
        public async Task<ActionResult> GetLeaderboard()
        {
            var leaderboard = await _context.ScoreEntries
                .Include(s => s.Player)
                .OrderByDescending(s => s.ZombiesKilled)
                .ThenBy(s => s.TimeSurvived)
                .Take(20)
                .Select(s => new
                {
                    PlayerNickname = s.Player!.Nickname,
                    s.ZombiesKilled,
                    s.DeathCount,
                    s.TimeToFirstOrb,
                    s.TimeToKillBoss,
                    s.TimeSurvived,
                    s.DatePlayed
                })
                .ToListAsync();

            return Ok(leaderboard);
        }
    }
}