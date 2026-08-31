using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using LaundryPOS.Domain.Entities;
using LaundryPOS.Domain.Enums;
using LaundryPOS.Domain.Interfaces.Repositories;
using LaundryPOS.Domain.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;

namespace LaundryPOS.Infrastructure.IoT;

/// <summary>
/// Owns the single shared MQTT connection used to talk to the machines'
/// ESP32 controllers (wired to the Wascomat washers/dryers via relay).
///
/// Responsibilities:
///  - Connects to the broker on startup and auto-reconnects if the
///    connection drops (network hiccup, broker restart, etc.).
///  - Publishes commands (start/stop/pause) on behalf of IIoTDeviceDriver
///    implementations — see WascomatDriver's "mqtt:" connection mode.
///  - Subscribes to every machine's status/event topics, keeps an in-memory
///    cache of each machine's last known state (used by GetStatusAsync /
///    HeartbeatAsync for MQTT-connected machines, since MQTT is push-based —
///    there's no live request/response round-trip like the HTTP relay mode),
///    persists relevant changes to the database, and notifies the
///    dashboard/kiosk UI in real time over SignalR.
///
/// Topic layout (BaseTopic defaults to "laundrypos", see IoT:Mqtt:BaseTopic):
///   {BaseTopic}/machine/{machineId}/comando  -> published BY this backend.
///     Payload: {"accion":"iniciar|detener|pausar|reiniciar","minutos":32,"ciclo":"normal"}
///   {BaseTopic}/machine/{machineId}/estado   -> published BY the ESP32 (retained).
///     Payload: {"estado":"disponible|en_uso|fuera_de_servicio|error","kg":18}
///   {BaseTopic}/machine/{machineId}/evento   -> published BY the ESP32 (one-shot).
///     Payload: {"evento":"ciclo_completado|puerta_abierta|error","detalle":"..."}
/// </summary>
public class MqttConnectionManager : IHostedService, IMqttPublisherService, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly MqttOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MqttConnectionManager> _logger;
    private readonly IMqttClient _client;
    private readonly ConcurrentDictionary<Guid, MqttMachineState> _lastKnownStates = new();
    private CancellationTokenSource? _lifetimeCts;

    public MqttConnectionManager(IOptions<MqttOptions> options, IServiceScopeFactory scopeFactory, ILogger<MqttConnectionManager> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _client = new MqttFactory().CreateMqttClient();
        _client.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _client.DisconnectedAsync += OnDisconnectedAsync;
    }

    public bool IsConnected => _client.IsConnected;

    public MqttMachineState? GetLastKnownState(Guid machineId) =>
        _lastKnownStates.TryGetValue(machineId, out var state) ? state : null;

    // ─── IHostedService ─────────────────────────────────────────────────────

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _lifetimeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await TryConnectAsync(_lifetimeCts.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _lifetimeCts?.Cancel();
        if (_client.IsConnected)
        {
            try { await _client.DisconnectAsync(cancellationToken: cancellationToken); }
            catch { /* best-effort on shutdown */ }
        }
    }

    public ValueTask DisposeAsync()
    {
        _lifetimeCts?.Dispose();
        _client.Dispose();
        return ValueTask.CompletedTask;
    }

    // ─── Connection management ─────────────────────────────────────────────

    private async Task TryConnectAsync(CancellationToken ct)
    {
        var clientId = $"laundrypos-api-{Guid.NewGuid():N}";
        if (clientId.Length > 48) clientId = clientId[..48];

        var optionsBuilder = new MqttClientOptionsBuilder()
            .WithTcpServer(_options.BrokerHost, _options.BrokerPort)
            .WithClientId(clientId)
            .WithCleanSession();

        if (!string.IsNullOrWhiteSpace(_options.Username))
            optionsBuilder = optionsBuilder.WithCredentials(_options.Username, _options.Password);

        try
        {
            await _client.ConnectAsync(optionsBuilder.Build(), ct);
            _logger.LogInformation("MQTT: Conectado al broker {Host}:{Port}", _options.BrokerHost, _options.BrokerPort);
            await SubscribeAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MQTT: No se pudo conectar a {Host}:{Port}, reintentando en 5s", _options.BrokerHost, _options.BrokerPort);
            ScheduleReconnect(ct);
        }
    }

    private async Task SubscribeAsync(CancellationToken ct)
    {
        var estadoTopic = $"{_options.BaseTopic}/machine/+/estado";
        var eventoTopic = $"{_options.BaseTopic}/machine/+/evento";

        await _client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(estadoTopic).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .WithTopicFilter(f => f.WithTopic(eventoTopic).WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce))
            .Build(), ct);

        _logger.LogInformation("MQTT: Suscrito a {Estado} y {Evento}", estadoTopic, eventoTopic);
    }

    private Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        _logger.LogWarning("MQTT: Desconectado del broker ({Reason}). Reintentando en 5s...", args.Reason);
        ScheduleReconnect(_lifetimeCts?.Token ?? CancellationToken.None);
        return Task.CompletedTask;
    }

    private void ScheduleReconnect(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                if (!ct.IsCancellationRequested && !_client.IsConnected)
                    await TryConnectAsync(ct);
            }
            catch (TaskCanceledException) { /* apagando el servicio */ }
        }, ct);
    }

    // ─── Publicación de comandos ────────────────────────────────────────────

    public async Task PublishCommandAsync(Guid machineId, string action, int? durationMinutes = null, string? cycle = null, CancellationToken ct = default)
    {
        if (!_client.IsConnected)
            throw new InvalidOperationException("No hay conexión con el broker MQTT. El comando no fue enviado.");

        var payload = JsonSerializer.Serialize(new { accion = action, minutos = durationMinutes, ciclo = cycle });
        var topic = $"{_options.BaseTopic}/machine/{machineId}/comando";

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(payload))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await _client.PublishAsync(message, ct);
        _logger.LogInformation("MQTT: Publicado '{Action}' en {Topic}", action, topic);
    }

    // ─── Mensajes entrantes ─────────────────────────────────────────────────

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        try
        {
            var topic = args.ApplicationMessage.Topic;
            var parts = topic.Split('/');
            var machineIdx = Array.IndexOf(parts, "machine");
            if (machineIdx < 0 || machineIdx + 2 >= parts.Length) return;
            if (!Guid.TryParse(parts[machineIdx + 1], out var machineId)) return;

            var kind = parts[machineIdx + 2]; // "estado" o "evento"
            var payload = GetPayloadString(args.ApplicationMessage);

            if (kind == "estado")
                await HandleEstadoAsync(machineId, payload);
            else if (kind == "evento")
                await HandleEventoAsync(machineId, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MQTT: Error procesando mensaje entrante en {Topic}", args.ApplicationMessage.Topic);
        }
    }

    private async Task HandleEstadoAsync(Guid machineId, string payload)
    {
        var dto = JsonSerializer.Deserialize<EstadoPayload>(payload, JsonOpts);
        if (dto is null) return;

        var (status, isRunning) = MapEstado(dto.Estado);

        _lastKnownStates[machineId] = new MqttMachineState
        {
            IsAlive = true,
            IsRunning = isRunning,
            CurrentState = dto.Estado ?? "unknown",
            CapacityKg = dto.Kg,
            Timestamp = DateTime.UtcNow
        };

        await UpdateMachineAsync(machineId, status);
    }

    private async Task HandleEventoAsync(Guid machineId, string payload)
    {
        var dto = JsonSerializer.Deserialize<EventoPayload>(payload, JsonOpts);
        if (dto is null) return;

        _logger.LogInformation("MQTT: Evento '{Evento}' de máquina {MachineId} — {Detalle}", dto.Evento, machineId, dto.Detalle);

        switch (dto.Evento)
        {
            case "ciclo_completado":
                var previous = _lastKnownStates.TryGetValue(machineId, out var prev) ? prev : new MqttMachineState();
                _lastKnownStates[machineId] = previous with
                {
                    IsAlive = true,
                    IsRunning = false,
                    CurrentState = "finalizado",
                    RemainingMinutes = 0,
                    Timestamp = DateTime.UtcNow
                };
                await UpdateMachineAsync(machineId, MachineStatus.Finished);
                break;

            case "puerta_abierta":
            case "error":
                await RaiseAlertAsync(machineId, dto.Evento, dto.Detalle);
                break;
        }
    }

    private async Task UpdateMachineAsync(Guid machineId, MachineStatus status)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notifier = scope.ServiceProvider.GetRequiredService<IRealTimeNotificationService>();

        var machine = await unitOfWork.Machines.GetByIdAsync(machineId);
        if (machine is null) return;

        machine.CommunicationStatus = CommunicationStatus.Online;
        machine.LastHeartbeat = DateTime.UtcNow;
        machine.Status = status;

        await unitOfWork.Machines.UpdateAsync(machine);
        await unitOfWork.SaveChangesAsync();

        await notifier.NotifyMachineStatusChangedAsync(machine.BranchId, machine.Id, status);
    }

    private async Task RaiseAlertAsync(Guid machineId, string evento, string? detalle)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notifier = scope.ServiceProvider.GetRequiredService<IRealTimeNotificationService>();

        var machine = await unitOfWork.Machines.GetByIdAsync(machineId);
        if (machine is null) return;

        var severity = evento == "error" ? AlertSeverity.Critical : AlertSeverity.Warning;
        var title = evento == "error" ? "Error reportado por la máquina" : "Puerta abierta durante el ciclo";
        var message = detalle ?? $"Máquina #{machine.Number} ({machine.Brand} {machine.Capacity}) reportó: {evento}";

        var alert = new MachineAlert
        {
            Title = title,
            Message = message,
            Severity = severity,
            MachineId = machine.Id,
            BranchId = machine.BranchId
        };

        await unitOfWork.Alerts.AddAsync(alert);
        await unitOfWork.SaveChangesAsync();

        await notifier.NotifyAlertCreatedAsync(machine.BranchId, alert.Id, severity, message);
    }

    private static (MachineStatus status, bool isRunning) MapEstado(string? estado) => estado switch
    {
        "disponible" => (MachineStatus.Available, false),
        "en_uso" => (MachineStatus.InCycle, true),
        "fuera_de_servicio" => (MachineStatus.OutOfService, false),
        "error" => (MachineStatus.Error, false),
        _ => (MachineStatus.Error, false)
    };

    private static string GetPayloadString(MqttApplicationMessage message)
    {
        var segment = message.PayloadSegment;
        return segment.Array is null || segment.Count == 0
            ? string.Empty
            : Encoding.UTF8.GetString(segment.Array, segment.Offset, segment.Count);
    }

    private record EstadoPayload
    {
        public string? Estado { get; init; }
        public int? Kg { get; init; }
    }

    private record EventoPayload
    {
        public string? Evento { get; init; }
        public string? Detalle { get; init; }
    }
}
