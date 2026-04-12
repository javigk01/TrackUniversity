namespace TrackUniversity.Models;

public class Bus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Nombre corto de la ruta, coincide con el segmento del tópico MQTT: universidad/bus/{Route}</summary>
    public string Route { get; set; } = string.Empty;

    public double? LastLatitude { get; set; }
    public double? LastLongitude { get; set; }
    public DateTime? LastUpdated { get; set; }

    public ICollection<BusReading> Readings { get; set; } = new List<BusReading>();
}
