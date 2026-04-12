using Microsoft.EntityFrameworkCore;
using TrackUniversity.Models;

namespace TrackUniversity.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<BusReading> BusReadings => Set<BusReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BusReading>()
            .HasOne(r => r.Bus)
            .WithMany(b => b.Readings)
            .HasForeignKey(r => r.BusId);

        // Índice para consultas por bus y tiempo
        modelBuilder.Entity<BusReading>()
            .HasIndex(r => new { r.BusId, r.Timestamp });
    }
}
