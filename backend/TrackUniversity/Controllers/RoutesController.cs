using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackUniversity.Data;

namespace TrackUniversity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoutesController : ControllerBase
{
    private readonly AppDbContext _db;

    public RoutesController(AppDbContext db) => _db = db;

    /// <summary>Devuelve todas las rutas con sus paradas y buses asignados.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllRoutes()
    {
        var routes = await _db.Routes
            .Include(r => r.Stops.OrderBy(s => s.StopOrder))
            .Include(r => r.Buses)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.RouteCode,
                r.Origin,
                r.Destination,
                r.AverageTimeMinutes,
                Stops = r.Stops.OrderBy(s => s.StopOrder).Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.StopOrder,
                    s.Latitude,
                    s.Longitude,
                }),
                Buses = r.Buses.Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Plate,
                    b.Capacity,
                    b.LastLatitude,
                    b.LastLongitude,
                    b.LastSpeed,
                    b.LastUpdated,
                }),
            })
            .ToListAsync();

        return Ok(routes);
    }

    /// <summary>Devuelve el detalle de una ruta con sus paradas y buses.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRoute(int id)
    {
        var route = await _db.Routes
            .Where(r => r.Id == id)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.RouteCode,
                r.Origin,
                r.Destination,
                r.AverageTimeMinutes,
                Stops = r.Stops.OrderBy(s => s.StopOrder).Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.StopOrder,
                    s.Latitude,
                    s.Longitude,
                }),
                Buses = r.Buses.Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Plate,
                    b.Capacity,
                    b.LastLatitude,
                    b.LastLongitude,
                    b.LastSpeed,
                    b.LastUpdated,
                }),
            })
            .FirstOrDefaultAsync();

        if (route is null) return NotFound(new { message = $"Ruta {id} no encontrada." });

        return Ok(route);
    }

    /// <summary>Devuelve solo los buses activos (con posición) de una ruta.</summary>
    [HttpGet("{id:int}/buses")]
    public async Task<IActionResult> GetBusesByRoute(int id)
    {
        var exists = await _db.Routes.AnyAsync(r => r.Id == id);
        if (!exists) return NotFound(new { message = $"Ruta {id} no encontrada." });

        var buses = await _db.Buses
            .Where(b => b.RouteId == id && b.LastLatitude != null)
            .Select(b => new
            {
                b.Id,
                b.Name,
                b.Plate,
                b.Capacity,
                b.LastLatitude,
                b.LastLongitude,
                b.LastSpeed,
                b.LastUpdated,
            })
            .ToListAsync();

        return Ok(buses);
    }

}
