namespace TrackUniversity.Models;

public class BusReading
{
    public int Id { get; set; }
    public int BusId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>Velocidad en km/h en el momento de la lectura.</summary>
    public double Speed { get; set; }

    public DateTime Timestamp { get; set; }

    public Bus Bus { get; set; } = null!;
}
