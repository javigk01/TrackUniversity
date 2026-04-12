using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TrackUniversity.Data;
using TrackUniversity.Models;

namespace TrackUniversity.Services;

/// <summary>
/// Background Service que se suscribe al broker MQTT y persiste
/// cada coordenada recibida en MySQL mediante Entity Framework.
/// </summary>
public class MqttWorkerService : BackgroundService
{
    private readonly ILogger<MqttWorkerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public MqttWorkerService(
        ILogger<MqttWorkerService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger        = logger;
        _scopeFactory  = scopeFactory;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var section  = _configuration.GetSection("Mqtt");
        var host     = section["Host"] ?? "localhost";
        var port     = section.GetValue<int>("Port", 1883);
        var useTls   = section.GetValue<bool>("UseTls", false);
        var username = section["Username"] ?? string.Empty;
        var password = section["Password"] ?? string.Empty;

        // ── Construir opciones del cliente MQTT ──────────────────────────────
        var clientOptsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId($"trackuniversity-backend-{Guid.NewGuid():N}")
            .WithCleanSession();

        if (useTls)
        {
            clientOptsBuilder = clientOptsBuilder
                .WithCredentials(username, password)
                .WithTlsOptions(o => o.UseTls());
        }

        // ManagedMqttClient: reconexión automática sin código extra
        var managedOptions = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(clientOptsBuilder.Build())
            .Build();

        var factory = new MqttFactory();
        using var client = factory.CreateManagedMqttClient();

        client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        client.ConnectedAsync    += e => { _logger.LogInformation("[MQTT] Conectado a {Host}:{Port}", host, port); return Task.CompletedTask; };
        client.DisconnectedAsync += e => { _logger.LogWarning("[MQTT] Desconectado. Reconectando..."); return Task.CompletedTask; };

        await client.StartAsync(managedOptions);

        // Tópico usa wildcard "#" para escuchar todas las rutas: universidad/bus/ruta1, ruta2, ...
        await client.SubscribeAsync("universidad/bus/#");
        _logger.LogInformation("[MQTT] Suscrito a universidad/bus/#");

        // Mantener el servicio vivo hasta que se solicite cancelación
        await Task.Delay(Timeout.Infinite, stoppingToken);

        await client.StopAsync();
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        var topic   = e.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

        _logger.LogInformation("[MQTT] {Topic} → {Payload}", topic, payload);

        try
        {
            var data = JsonSerializer.Deserialize<BusLocationMessage>(payload, _jsonOptions);
            if (data is null) return;

            // Extraer ruta del tópico: universidad/bus/ruta1 → "ruta1"
            var parts = topic.Split('/');
            var route = parts.Length >= 3 ? parts[2] : string.Empty;
            if (string.IsNullOrEmpty(route)) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var bus = await db.Buses.FirstOrDefaultAsync(b => b.Route == route);
            if (bus is null)
            {
                _logger.LogWarning("[MQTT] No existe un bus con ruta '{Route}' en la BD.", route);
                return;
            }

            // Actualizar posición actual del bus
            bus.LastLatitude  = data.Lat;
            bus.LastLongitude = data.Lng;
            bus.LastUpdated   = DateTime.UtcNow;

            // Guardar lectura histórica
            db.BusReadings.Add(new BusReading
            {
                BusId     = bus.Id,
                Latitude  = data.Lat,
                Longitude = data.Lng,
                Timestamp = DateTime.UtcNow,
            });

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT] Error procesando mensaje del tópico {Topic}", topic);
        }
    }
}

// DTO para deserializar el JSON enviado por el simulador: {"lat":4.628,"lng":-74.064}
internal sealed class BusLocationMessage
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}
