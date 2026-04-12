using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrackUniversity.Data;
using TrackUniversity.Models;

namespace TrackUniversity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OccupancyHistoryController : ControllerBase
{
    private readonly AppDbContext _db;

    public OccupancyHistoryController(AppDbContext db) => _db = db;

    /// <summary>Guarda registros de ocupación por ruta.</summary>
    [HttpPost]
    public async Task<IActionResult> SaveOccupancyHistory([FromBody] List<RouteOccupancyRecord> records)
    {
        if (records == null || records.Count == 0)
            return BadRequest(new { message = "No records provided" });

        foreach (var record in records)
        {
            var history = new RouteOccupancyHistory
            {
                RouteCode = record.RouteCode,
                RouteName = record.RouteName,
                OccupancyPercent = record.OccupancyPercent,
                ActiveBuses = record.ActiveBuses,
                TotalBuses = record.TotalBuses,
                Timestamp = DateTime.UtcNow,
            };

            _db.RouteOccupancyHistories.Add(history);
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Records saved", count = records.Count });
    }

    /// <summary>Obtiene el historial de ocupación de las últimas horas.</summary>
    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestOccupancyHistory(int hours = 1, int limit = 200)
    {
        var since = DateTime.UtcNow.AddHours(-hours);

        var history = await _db.RouteOccupancyHistories
            .Where(h => h.Timestamp >= since)
            .OrderBy(h => h.Timestamp)
            .Take(limit)
            .ToListAsync();

        return Ok(history);
    }

    /// <summary>Obtiene el historial de ocupación por ruta específica.</summary>
    [HttpGet("route/{routeCode}")]
    public async Task<IActionResult> GetOccupancyHistoryByRoute(string routeCode, int hours = 1)
    {
        var since = DateTime.UtcNow.AddHours(-hours);

        var history = await _db.RouteOccupancyHistories
            .Where(h => h.RouteCode == routeCode && h.Timestamp >= since)
            .OrderBy(h => h.Timestamp)
            .ToListAsync();

        return Ok(history);
    }
}

/// <summary>DTO para recibir datos de ocupación desde el frontend.</summary>
public class RouteOccupancyRecord
{
    public string RouteCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public double OccupancyPercent { get; set; }
    public int ActiveBuses { get; set; }
    public int TotalBuses { get; set; }
}
