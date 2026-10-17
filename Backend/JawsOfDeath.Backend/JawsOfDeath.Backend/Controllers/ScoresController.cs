using JawsOfDeath.Backend.Data;
using JawsOfDeath.Backend.Dtos;
using JawsOfDeath.Backend.Models;
using Microsoft.AspNetCore.Mvc;

namespace JawsOfDeath.Backend.Controllers;

[ApiController]
[Route("api/scores")]
public class ScoresController : ControllerBase
{
    private readonly GameDbContext _db;

    public ScoresController(GameDbContext db)
    {
        _db = db;
    }

    // The score formula lives ONLY here. Change this one method to change scoring.
    public static int CalculateScore(double survivalTime, int kills)
    {
        return (int)Math.Round(survivalTime) + kills;
    }

    // POST /api/scores
    [HttpPost]
    public async Task<ActionResult<ScoreResponse>> CreateScore(CreateScoreRequest request)
    {
        var player = await _db.Players.FindAsync(request.PlayerId);
        if (player == null)
            return NotFound($"Player {request.PlayerId} not found.");

        var entry = new ScoreEntry
        {
            PlayerId = request.PlayerId,
            Kills = request.Kills,
            Deaths = request.Deaths,
            TimeToFirstOrb = request.TimeToFirstOrb,
            TimeToBossKill = request.TimeToBossKill,
            SurvivalTime = request.SurvivalTime,
            Won = request.Won,
            Score = CalculateScore(request.SurvivalTime, request.Kills),
            PlayedAt = DateTime.UtcNow
        };

        _db.ScoreEntries.Add(entry);
        await _db.SaveChangesAsync();

        return Ok(new ScoreResponse
        {
            Id = entry.Id,
            PlayerId = entry.PlayerId,
            Kills = entry.Kills,
            Deaths = entry.Deaths,
            TimeToFirstOrb = entry.TimeToFirstOrb,
            TimeToBossKill = entry.TimeToBossKill,
            SurvivalTime = entry.SurvivalTime,
            Score = entry.Score,
            Won = entry.Won,
            PlayedAt = entry.PlayedAt
        });
    }
}