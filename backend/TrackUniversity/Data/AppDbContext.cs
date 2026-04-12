using Microsoft.EntityFrameworkCore;
using Route = TrackUniversity.Models.Route;
using TrackUniversity.Models;

namespace TrackUniversity.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<BusReading> BusReadings => Set<BusReading>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<RouteStop> RouteStops => Set<RouteStop>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Bus → BusReading
        modelBuilder.Entity<BusReading>()
            .HasOne(r => r.Bus)
            .WithMany(b => b.Readings)
            .HasForeignKey(r => r.BusId);

        // Bus → Route (muchos buses pueden pertenecer a una ruta)
        modelBuilder.Entity<Bus>()
            .HasOne(b => b.Route)
            .WithMany(r => r.Buses)
            .HasForeignKey(b => b.RouteId);

        // RouteStop → Route
        modelBuilder.Entity<RouteStop>()
            .HasOne(s => s.Route)
            .WithMany(r => r.Stops)
            .HasForeignKey(s => s.RouteId);

        // La placa del bus debe ser única
        modelBuilder.Entity<Bus>()
            .HasIndex(b => b.Plate)
            .IsUnique();

        // El código de ruta debe ser único (se usa para identificar en MQTT)
        modelBuilder.Entity<Route>()
            .HasIndex(r => r.RouteCode)
            .IsUnique();

        // Índice para consultas de historial por bus y tiempo
        modelBuilder.Entity<BusReading>()
            .HasIndex(r => new { r.BusId, r.Timestamp });

        // Índice para ordenar paradas por ruta
        modelBuilder.Entity<RouteStop>()
            .HasIndex(s => new { s.RouteId, s.StopOrder });
    }
}
