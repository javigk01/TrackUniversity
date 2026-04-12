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
/// cada telemetría recibida en MySQL mediante Entity Framework.
///
/// Tópico esperado: universidad/bus/{plate}
/// Payload esperado: { "lat": 4.628, "lng": -74.064, "speed": 35.5 }
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
        var host     = section["Host"] ?? "mosquitto";
        var port     = section.GetValue<int>("Port", 1883);

        var clientOptsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId($"trackuniversity-backend-{Guid.NewGuid():N}")
            .WithCleanSession();

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

        // Escucha todos los buses: universidad/bus/TUB-001, TUB-002, ...
        await client.SubscribeAsync("universidad/bus/#");
        _logger.LogInformation("[MQTT] Suscrito a universidad/bus/#");

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
            var data = JsonSerializer.Deserialize<BusTelemetryMessage>(payload, _jsonOptions);
            if (data is null) return;

            // Extraer placa del tópico: universidad/bus/TUB-001 → "TUB-001"
            var parts = topic.Split('/');
            var plate = parts.Length >= 3 ? parts[2] : string.Empty;
            if (string.IsNullOrEmpty(plate)) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var bus = await db.Buses.FirstOrDefaultAsync(b => b.Plate == plate);
            if (bus is null)
            {
                _logger.LogWarning("[MQTT] No existe un bus con placa '{Plate}' en la BD.", plate);
                return;
            }

            // Actualizar telemetría actual del bus
            bus.LastLatitude  = data.Lat;
            bus.LastLongitude = data.Lng;
            bus.LastSpeed     = data.Speed;
            bus.LastUpdated   = DateTime.UtcNow;

            // Guardar lectura histórica
            db.BusReadings.Add(new BusReading
            {
                BusId     = bus.Id,
                Latitude  = data.Lat,
                Longitude = data.Lng,
                Speed     = data.Speed,
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

// DTO para deserializar el payload: {"lat":4.628,"lng":-74.064,"speed":35.5}
internal sealed class BusTelemetryMessage
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lng")]
    public double Lng { get; set; }

    [JsonPropertyName("speed")]
    public double Speed { get; set; }
}
