using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JawsOfDeath.Backend.Data;
using JawsOfDeath.Backend.Models;

namespace JawsOfDeath.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlayersController : ControllerBase
    {
        private readonly GameDbContext _context;

        public PlayersController(GameDbContext context)
        {
            _context = context;
        }

        // POST /api/players
        [HttpPost]
        public async Task<ActionResult<Player>> CreatePlayer([FromBody] CreatePlayerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Nickname))
            {
                return BadRequest("Nickname is required.");
            }

            var player = new Player { Nickname = request.Nickname };
            _context.Players.Add(player);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPlayerStats), new { id = player.Id }, player);
        }

        // GET /api/players/{id}/stats
        [HttpGet("{id}/stats")]
        public async Task<ActionResult> GetPlayerStats(int id)
        {
            var player = await _context.Players
                .Include(p => p.ScoreEntries)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (player == null)
            {
                return NotFound();
            }

            return Ok(player);
        }
    }

    public class CreatePlayerRequest
    {
        public string Nickname { get; set; } = string.Empty;
    }
}