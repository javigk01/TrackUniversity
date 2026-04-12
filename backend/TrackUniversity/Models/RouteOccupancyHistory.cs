namespace TrackUniversity.Models;

public class RouteOccupancyHistory
{
    public int Id { get; set; }

    /// <summary>Código de la ruta (FK conceptual, sin navegación)</summary>
    public string RouteCode { get; set; } = string.Empty;

    /// <summary>Nombre de la ruta</summary>
    public string RouteName { get; set; } = string.Empty;

    /// <summary>Porcentaje de ocupación en el momento del registro</summary>
    public double OccupancyPercent { get; set; }

    /// <summary>Número de buses activos en esa ruta</summary>
    public int ActiveBuses { get; set; }

    /// <summary>Total de buses en esa ruta</summary>
    public int TotalBuses { get; set; }

    /// <summary>Timestamp del registro</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
