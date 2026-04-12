using Microsoft.EntityFrameworkCore;
using TrackUniversity.Data;
using TrackUniversity.Models;
using TrackUniversity.Services;

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

        db.Routes.AddRange(ruta1, ruta2);
        db.SaveChanges();

        db.Buses.AddRange(
            new Bus { Name = "Bus Azul Ruta 1",  Plate = "TUB-001", Capacity = 40, RouteId = ruta1.Id },
            new Bus { Name = "Bus Verde Ruta 2", Plate = "TUB-002", Capacity = 35, RouteId = ruta2.Id }
        );
        db.SaveChanges();

        app.Logger.LogInformation("Datos semilla insertados: 2 rutas, 9 paradas, 2 buses.");
    }
}

app.UseCors();
app.MapControllers();

app.Run();
