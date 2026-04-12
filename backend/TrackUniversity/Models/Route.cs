namespace TrackUniversity.Models;

public class Route
{
    public int Id { get; set; }

    /// <summary>Nombre descriptivo: "Ruta 1 - Campus Norte"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Código corto que coincide con el segmento del tópico MQTT: universidad/bus/{RouteCode}</summary>
    public string RouteCode { get; set; } = string.Empty;

    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;

    /// <summary>Tiempo promedio del recorrido completo en minutos.</summary>
    public int AverageTimeMinutes { get; set; }

    public ICollection<RouteStop> Stops { get; set; } = new List<RouteStop>();
    public ICollection<Bus> Buses { get; set; } = new List<Bus>();
}
