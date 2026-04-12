namespace TrackUniversity.Models;

public class RouteStop
{
    public int Id { get; set; }
    public int RouteId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Posición en el recorrido (1 = primera parada).</summary>
    public int StopOrder { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public Route Route { get; set; } = null!;
}
