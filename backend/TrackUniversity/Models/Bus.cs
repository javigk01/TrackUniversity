namespace TrackUniversity.Models;

public class Bus
{
    public int Id { get; set; }

    /// <summary>Nombre descriptivo del bus, ej: "Bus Ruta 1 - Azul"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Placa del vehículo, ej: "TUB-001". También se usa como identificador en el tópico MQTT: universidad/bus/{Plate}</summary>
    public string Plate { get; set; } = string.Empty;

    /// <summary>Capacidad máxima de pasajeros.</summary>
    public int Capacity { get; set; }

    /// <summary>FK hacia la ruta asignada.</summary>
    public int RouteId { get; set; }

    // ── Última telemetría recibida vía MQTT ──────────────────────────────────
    public double? LastLatitude { get; set; }
    public double? LastLongitude { get; set; }

    /// <summary>Velocidad en km/h reportada en la última lectura.</summary>
    public double? LastSpeed { get; set; }

    public DateTime? LastUpdated { get; set; }

    public Route Route { get; set; } = null!;
    public ICollection<BusReading> Readings { get; set; } = new List<BusReading>();
}
