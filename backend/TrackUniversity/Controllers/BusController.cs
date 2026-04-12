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

    /// <summary>Devuelve todos los buses con información completa y posición actual.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllBuses()
    {
        var buses = await _db.Buses
            .Include(b => b.Route)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Plate,
                b.Capacity,
                b.LastLatitude,
                b.LastLongitude,
                b.LastSpeed,
                b.CurrentPassengers,
                b.LastUpdated,
                Route = new
                {
                    b.Route.Id,
                    b.Route.Name,
                    b.Route.RouteCode,
                    b.Route.Origin,
                    b.Route.Destination,
                    b.Route.AverageTimeMinutes,
                },
            })
            .ToListAsync();

        return Ok(buses);
    }

    /// <summary>Devuelve la información completa de un bus por ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBus(int id)
    {
        var bus = await _db.Buses
            .Include(b => b.Route)
            .Where(b => b.Id == id)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Plate,
                b.Capacity,
                b.LastLatitude,
                b.LastLongitude,
                b.LastSpeed,
                b.CurrentPassengers,
                b.LastUpdated,
                Route = new
                {
                    b.Route.Id,
                    b.Route.Name,
                    b.Route.RouteCode,
                    b.Route.Origin,
                    b.Route.Destination,
                    b.Route.AverageTimeMinutes,
                },
            })
            .FirstOrDefaultAsync();

        if (bus is null) return NotFound(new { message = $"Bus {id} no encontrado." });

        return Ok(bus);
    }

    /// <summary>Devuelve solo la telemetría en tiempo real (lat, lng, velocidad) de un bus.</summary>
    [HttpGet("{id:int}/position")]
    public async Task<IActionResult> GetBusPosition(int id)
    {
        var position = await _db.Buses
            .Where(b => b.Id == id)
            .Select(b => new
            {
                b.Id,
                b.Plate,
                b.LastLatitude,
                b.LastLongitude,
                b.LastSpeed,
                b.LastUpdated,
            })
            .FirstOrDefaultAsync();

        if (position is null) return NotFound(new { message = $"Bus {id} no encontrado." });

        return Ok(position);
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
            .Select(r => new { r.Latitude, r.Longitude, r.Speed, r.Timestamp })
            .ToListAsync();

        return Ok(readings);
    }
}
