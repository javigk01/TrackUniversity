using Microsoft.EntityFrameworkCore;
using TrackUniversity.Data;
using TrackUniversity.Models;
using TrackUniversity.Services;
using Route = TrackUniversity.Models.Route;

var builder = WebApplication.CreateBuilder(args);

// ── Base de datos (MySQL via Pomelo EF Core) ────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'DefaultConnection' en appsettings.json");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ── Controladores HTTP ──────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Worker MQTT (Background Service) ───────────────────────────────────────
builder.Services.AddHostedService<MqttWorkerService>();

// ── CORS: permite llamadas desde el frontend Astro ──────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:4321"];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// ── Crear tablas y sembrar datos iniciales ──────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Routes.Any())
    {
        var ruta1 = new Route
        {
            Name               = "Ruta 1 - Campus Norte",
            RouteCode          = "ruta1",
            Origin             = "Entrada Principal",
            Destination        = "Terminal Norte",
            AverageTimeMinutes = 25,
            Stops = new List<RouteStop>
            {
                new() { Name = "Entrada Principal",  StopOrder = 1, Latitude = 4.6284, Longitude = -74.0641 },
                new() { Name = "Biblioteca Central", StopOrder = 2, Latitude = 4.6301, Longitude = -74.0660 },
                new() { Name = "Parada Norte",       StopOrder = 3, Latitude = 4.6325, Longitude = -74.0672 },
                new() { Name = "Cruce Intermedio",   StopOrder = 4, Latitude = 4.6348, Longitude = -74.0658 },
                new() { Name = "Terminal Norte",     StopOrder = 5, Latitude = 4.6361, Longitude = -74.0630 },
            },
        };

        var ruta2 = new Route
        {
            Name               = "Ruta 2 - Campus Sur",
            RouteCode          = "ruta2",
            Origin             = "Campus Sur",
            Destination        = "Bosa Terminal",
            AverageTimeMinutes = 35,
            Stops = new List<RouteStop>
            {
                new() { Name = "Campus Sur",     StopOrder = 1, Latitude = 4.6100, Longitude = -74.1100 },
                new() { Name = "Cafetería",      StopOrder = 2, Latitude = 4.6120, Longitude = -74.1080 },
                new() { Name = "Kennedy",        StopOrder = 3, Latitude = 4.6200, Longitude = -74.1050 },
                new() { Name = "Bosa Terminal",  StopOrder = 4, Latitude = 4.6250, Longitude = -74.1800 },
            },
        };

        var ruta3 = new Route
        {
            Name               = "Ruta 3 - Clínica",
            RouteCode          = "ruta3",
            Origin             = "Entrada Principal",
            Destination        = "Clínica Universitaria",
            AverageTimeMinutes = 20,
            Stops = new List<RouteStop>
            {
                new() { Name = "Entrada Principal", StopOrder = 1, Latitude = 4.6284, Longitude = -74.0641 },
                new() { Name = "Edificio A",        StopOrder = 2, Latitude = 4.6270, Longitude = -74.0700 },
                new() { Name = "Clínica Univ",      StopOrder = 3, Latitude = 4.6280, Longitude = -74.0750 },
            },
        };

        var ruta4 = new Route
        {
            Name               = "Ruta 4 - Deportes",
            RouteCode          = "ruta4",
            Origin             = "Entrada Principal",
            Destination        = "Centro Deportivo",
            AverageTimeMinutes = 30,
            Stops = new List<RouteStop>
            {
                new() { Name = "Entrada Principal",  StopOrder = 1, Latitude = 4.6284, Longitude = -74.0641 },
                new() { Name = "Cafetería",          StopOrder = 2, Latitude = 4.6300, Longitude = -74.0600 },
                new() { Name = "Centro Deportivo",   StopOrder = 3, Latitude = 4.6200, Longitude = -74.0550 },
            },
        };

        var ruta5 = new Route
        {
            Name               = "Ruta 5 - Laboratorios",
            RouteCode          = "ruta5",
            Origin             = "Entrada Sur",
            Destination        = "Bloque Laboratorios",
            AverageTimeMinutes = 15,
            Stops = new List<RouteStop>
            {
                new() { Name = "Entrada Sur",         StopOrder = 1, Latitude = 4.6150, Longitude = -74.0700 },
                new() { Name = "Bloque Laboratorios", StopOrder = 2, Latitude = 4.6180, Longitude = -74.0680 },
            },
        };

        var ruta6 = new Route
        {
            Name               = "Ruta 6 - Estacionamiento",
            RouteCode          = "ruta6",
            Origin             = "Estacionamiento A",
            Destination        = "Entrada Principal",
            AverageTimeMinutes = 10,
            Stops = new List<RouteStop>
            {
                new() { Name = "Estacionamiento A", StopOrder = 1, Latitude = 4.6350, Longitude = -74.0500 },
                new() { Name = "Entrada Principal", StopOrder = 2, Latitude = 4.6284, Longitude = -74.0641 },
            },
        };

        db.Routes.AddRange(ruta1, ruta2, ruta3, ruta4, ruta5, ruta6);
        db.SaveChanges();

        db.Buses.AddRange(
            // Ruta 1 - Campus Norte (4 buses)
            new Bus { Name = "Bus Azul Ruta 1 - 01",   Plate = "TUB-001", Capacity = 40, RouteId = ruta1.Id },
            new Bus { Name = "Bus Azul Ruta 1 - 02",   Plate = "TUB-003", Capacity = 40, RouteId = ruta1.Id },
            new Bus { Name = "Bus Azul Ruta 1 - 03",   Plate = "TUB-004", Capacity = 45, RouteId = ruta1.Id },
            new Bus { Name = "Bus Azul Ruta 1 - 04",   Plate = "TUB-005", Capacity = 40, RouteId = ruta1.Id },
            
            // Ruta 2 - Campus Sur (4 buses)
            new Bus { Name = "Bus Verde Ruta 2 - 01",  Plate = "TUB-002", Capacity = 35, RouteId = ruta2.Id },
            new Bus { Name = "Bus Verde Ruta 2 - 02",  Plate = "TUB-006", Capacity = 35, RouteId = ruta2.Id },
            new Bus { Name = "Bus Verde Ruta 2 - 03",  Plate = "TUB-007", Capacity = 40, RouteId = ruta2.Id },
            new Bus { Name = "Bus Verde Ruta 2 - 04",  Plate = "TUB-008", Capacity = 35, RouteId = ruta2.Id },
            
            // Ruta 3 - Clínica (3 buses)
            new Bus { Name = "Bus Rojo Ruta 3 - 01",   Plate = "TUB-009", Capacity = 30, RouteId = ruta3.Id },
            new Bus { Name = "Bus Rojo Ruta 3 - 02",   Plate = "TUB-010", Capacity = 30, RouteId = ruta3.Id },
            new Bus { Name = "Bus Rojo Ruta 3 - 03",   Plate = "TUB-011", Capacity = 30, RouteId = ruta3.Id },
            
            // Ruta 4 - Deportes (3 buses)
            new Bus { Name = "Bus Amarillo Ruta 4 - 01", Plate = "TUB-012", Capacity = 35, RouteId = ruta4.Id },
            new Bus { Name = "Bus Amarillo Ruta 4 - 02", Plate = "TUB-013", Capacity = 35, RouteId = ruta4.Id },
            new Bus { Name = "Bus Amarillo Ruta 4 - 03", Plate = "TUB-014", Capacity = 40, RouteId = ruta4.Id },
            
            // Ruta 5 - Laboratorios (2 buses)
            new Bus { Name = "Bus Naranja Ruta 5 - 01", Plate = "TUB-015", Capacity = 25, RouteId = ruta5.Id },
            new Bus { Name = "Bus Naranja Ruta 5 - 02", Plate = "TUB-016", Capacity = 25, RouteId = ruta5.Id },
            
            // Ruta 6 - Estacionamiento (2 buses)
            new Bus { Name = "Bus Morado Ruta 6 - 01", Plate = "TUB-017", Capacity = 30, RouteId = ruta6.Id },
            new Bus { Name = "Bus Morado Ruta 6 - 02", Plate = "TUB-018", Capacity = 30, RouteId = ruta6.Id }
        );
        db.SaveChanges();

        app.Logger.LogInformation("Datos semilla insertados: 6 rutas, 18 buses.");
    }
}

app.UseCors();
app.MapControllers();

app.Run();
