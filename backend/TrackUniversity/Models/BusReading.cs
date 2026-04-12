namespace TrackUniversity.Models;

public class BusReading
{
    public int Id { get; set; }
    public int BusId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }

    public Bus Bus { get; set; } = null!;
}
