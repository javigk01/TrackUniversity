using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackUniversity.Data;

namespace TrackUniversity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusController : ControllerBase
{
    private readonly AppDbContext _db;

    public BusController(AppDbContext db) => _db = db;

    /// <summary>Devuelve todos los buses con su posición más reciente.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllBuses()
    {
        var buses = await _db.Buses
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Route,
                b.LastLatitude,
                b.LastLongitude,
                b.LastUpdated,
            })
            .ToListAsync();

        return Ok(buses);
    }

    /// <summary>Devuelve la posición actual de un bus por ID.</summary>
    [HttpGet("{id:int}/position")]
    public async Task<IActionResult> GetBusPosition(int id)
    {
        var bus = await _db.Buses
            .Where(b => b.Id == id)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Route,
                b.LastLatitude,
                b.LastLongitude,
                b.LastUpdated,
            })
            .FirstOrDefaultAsync();

        if (bus is null) return NotFound(new { message = $"Bus {id} no encontrado." });

        return Ok(bus);
    }

    /// <summary>Devuelve las últimas N lecturas históricas de un bus (default 50).</summary>
    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetBusHistory(int id, [FromQuery] int limit = 50)
    {
        if (limit is < 1 or > 500)
            return BadRequest(new { message = "El parámetro 'limit' debe estar entre 1 y 500." });

        var readings = await _db.BusReadings
            .Where(r => r.BusId == id)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .Select(r => new { r.Latitude, r.Longitude, r.Timestamp })
            .ToListAsync();

        return Ok(readings);
    }
}
