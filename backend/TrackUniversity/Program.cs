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
// NOTA: En producción, restringe los orígenes en appsettings.json → Cors:AllowedOrigins
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

    if (!db.Buses.Any())
    {
        db.Buses.AddRange(
            new Bus { Name = "Bus Ruta 1", Route = "ruta1" },
            new Bus { Name = "Bus Ruta 2", Route = "ruta2" }
        );
        db.SaveChanges();
        app.Logger.LogInformation("Datos semilla insertados: 2 buses.");
    }
}

app.UseCors();
app.MapControllers();

app.Run();
