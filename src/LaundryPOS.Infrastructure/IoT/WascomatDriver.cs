using System.IO.Ports;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LaundryPOS.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LaundryPOS.Infrastructure.IoT;

/// <summary>
/// Wascomat machine driver.
///
/// Supports three integration modes, selected via the connection string prefix:
///
/// 1. RELAY mode (ESP32/Arduino via HTTP REST):
///    ConnectionString = "relay:http://192.168.1.50"
///    The ESP32 board is wired to the machine's coin/payment input via a relay.
///    Each HTTP call triggers a relay pulse that simulates a coin insertion.
///    Price-per-pulse is programmed on the Wascomat machine itself.
///
/// 2. SERIAL mode (RS-232 direct — Wascomat WE/WD models):
///    ConnectionString = "serial:COM3:9600" or "serial:/dev/ttyUSB0:9600"
///    Sends ASCII framed commands directly to the machine via serial port.
///    Protocol: STX + MachineId(2) + Command(2) + Data(4) + Checksum(2) + ETX
///
/// 3. MQTT mode (ESP32 via broker — recommended, no static IP/cabling needed):
///    ConnectionString = "mqtt:{machineId-guid}"
///    Commands are published through MqttConnectionManager to
///    "{BaseTopic}/machine/{machineId}/comando"; the ESP32 firmware
///    subscribes to that topic and publishes back its status/events, which
///    MqttConnectionManager caches and persists. See MqttConnectionManager
///    for the exact topic/payload layout.
/// </summary>
public class WascomatDriver : IIoTDeviceDriver
{
    private readonly ILogger<WascomatDriver> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMqttPublisherService _mqttPublisher;

    public string DriverName => "Wascomat";

    public WascomatDriver(ILogger<WascomatDriver> logger, IHttpClientFactory httpClientFactory, IMqttPublisherService mqttPublisher)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _mqttPublisher = mqttPublisher;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Public interface
    // ──────────────────────────────────────────────────────────────────────────

    public async Task<IoTCommandResult> StartMachineAsync(
        string connectionString, int durationMinutes, CancellationToken ct = default)
    {
        _logger.LogInformation("Wascomat: START — {Connection}, {Duration} min", connectionString, durationMinutes);

        var (mode, address) = Parse(connectionString);

        return mode switch
        {
            "relay"  => await RelayStartAsync(address, durationMinutes, ct),
            "serial" => await SerialCommandAsync(address, WascomatCommand.Start, durationMinutes, ct),
            "mqtt"   => await MqttCommandAsync(address, "iniciar", durationMinutes, ct),
            _        => Result(false, "Unknown connection mode. Use 'relay:', 'serial:' or 'mqtt:' prefix.")
        };
    }

    public async Task<IoTCommandResult> StopMachineAsync(
        string connectionString, CancellationToken ct = default)
    {
        _logger.LogInformation("Wascomat: STOP — {Connection}", connectionString);
        var (mode, address) = Parse(connectionString);

        return mode switch
        {
            "relay"  => await RelayCommandAsync(address, "stop", ct),
            "serial" => await SerialCommandAsync(address, WascomatCommand.Stop, 0, ct),
            "mqtt"   => await MqttCommandAsync(address, "detener", null, ct),
            _        => Result(false, "Unknown connection mode.")
        };
    }

    public async Task<IoTCommandResult> PauseMachineAsync(
        string connectionString, CancellationToken ct = default)
    {
        _logger.LogInformation("Wascomat: PAUSE — {Connection}", connectionString);
        var (mode, address) = Parse(connectionString);

        return mode switch
        {
            "relay"  => await RelayCommandAsync(address, "pause", ct),
            "serial" => await SerialCommandAsync(address, WascomatCommand.Pause, 0, ct),
            "mqtt"   => await MqttCommandAsync(address, "pausar", null, ct),
            _        => Result(false, "Unknown connection mode.")
        };
    }

    public async Task<IoTCommandResult> RestartControllerAsync(
        string connectionString, CancellationToken ct = default)
    {
        var (mode, address) = Parse(connectionString);
        if (mode == "relay")
            return await RelayCommandAsync(address, "restart", ct);
        if (mode == "mqtt")
            return await MqttCommandAsync(address, "reiniciar", null, ct);

        return Result(true, null); // Serial mode: no restart command
    }

    public async Task<IoTHeartbeatResult> HeartbeatAsync(
        string connectionString, CancellationToken ct = default)
    {
        var (mode, address) = Parse(connectionString);

        if (mode == "relay")
        {
            try
            {
                var http = _httpClientFactory.CreateClient("WascomatRelay");
                var response = await http.GetAsync($"{address}/api/ping", ct);
                return new IoTHeartbeatResult
                {
                    IsAlive = response.IsSuccessStatusCode,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch
            {
                return new IoTHeartbeatResult { IsAlive = false, Timestamp = DateTime.UtcNow };
            }
        }

        if (mode == "mqtt")
        {
            // MQTT is push-based: "alive" means the ESP32 has reported its
            // status recently (see MqttConnectionManager), not a live ping.
            var state = Guid.TryParse(address, out var machineId) ? _mqttPublisher.GetLastKnownState(machineId) : null;
            return new IoTHeartbeatResult { IsAlive = state?.IsAlive ?? false, Timestamp = state?.Timestamp ?? DateTime.UtcNow };
        }

        // Serial mode: basic port availability check
        var portName = address.Split(':')[0];
        return new IoTHeartbeatResult
        {
            IsAlive = SerialPort.GetPortNames().Contains(portName),
            Timestamp = DateTime.UtcNow
        };
    }

    public async Task<IoTStatusResult> GetStatusAsync(
        string connectionString, CancellationToken ct = default)
    {
        var (mode, address) = Parse(connectionString);

        if (mode == "relay")
        {
            try
            {
                var http = _httpClientFactory.CreateClient("WascomatRelay");
                var response = await http.GetFromJsonAsync<RelayStatusResponse>(
                    $"{address}/api/status", ct);

                return new IoTStatusResult
                {
                    IsRunning = response?.Running ?? false,
                    RemainingMinutes = response?.RemainingMinutes,
                    CurrentState = response?.State ?? "unknown",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Wascomat: Could not read status from relay controller");
                return new IoTStatusResult { IsRunning = false, CurrentState = "unreachable", Timestamp = DateTime.UtcNow };
            }
        }

        if (mode == "mqtt")
        {
            var state = Guid.TryParse(address, out var machineId) ? _mqttPublisher.GetLastKnownState(machineId) : null;
            return new IoTStatusResult
            {
                IsRunning = state?.IsRunning ?? false,
                RemainingMinutes = state?.RemainingMinutes,
                CurrentState = state?.CurrentState ?? "unknown",
                Timestamp = state?.Timestamp ?? DateTime.UtcNow
            };
        }

        return new IoTStatusResult { IsRunning = false, CurrentState = "unknown", Timestamp = DateTime.UtcNow };
    }

    // ──────────────────────────────────────────────────────────────────────────
    // MQTT mode (ESP32 via broker)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// address is expected to be the machine's Guid (Machine.Id), e.g.
    /// ConnectionString = "mqtt:3fa85f64-5717-4562-b3fc-2c963f66afa6".
    /// </summary>
    private async Task<IoTCommandResult> MqttCommandAsync(
        string address, string action, int? durationMinutes, CancellationToken ct)
    {
        if (!Guid.TryParse(address, out var machineId))
            return Result(false, "Para el modo 'mqtt:' la conexión debe ser el Id (Guid) de la máquina.");

        try
        {
            await _mqttPublisher.PublishCommandAsync(machineId, action, durationMinutes, ct: ct);
            return Result(true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wascomat MQTT: Error al publicar '{Action}' para {MachineId}", action, machineId);
            return Result(false, ex.Message);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Relay mode (ESP32 via HTTP)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a START command to the ESP32 relay board.
    /// The ESP32 firmware receives the duration and triggers the relay N times
    /// to simulate coin insertion, based on the cost-per-pulse setting.
    /// </summary>
    private async Task<IoTCommandResult> RelayStartAsync(
        string baseUrl, int durationMinutes, CancellationToken ct)
    {
        try
        {
            var http = _httpClientFactory.CreateClient("WascomatRelay");
            var payload = new { action = "start", duration_minutes = durationMinutes };
            var response = await http.PostAsJsonAsync($"{baseUrl}/api/machine/start", payload, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Wascomat relay: START OK — {Url}", baseUrl);
                return Result(true, null);
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Wascomat relay: START failed ({Status}) — {Body}", response.StatusCode, body);
            return Result(false, $"Relay controller returned HTTP {(int)response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Wascomat relay: Network error reaching {Url}", baseUrl);
            return Result(false, $"Cannot reach relay controller: {ex.Message}");
        }
    }

    private async Task<IoTCommandResult> RelayCommandAsync(
        string baseUrl, string action, CancellationToken ct)
    {
        try
        {
            var http = _httpClientFactory.CreateClient("WascomatRelay");
            var payload = new { action };
            var response = await http.PostAsJsonAsync($"{baseUrl}/api/machine/command", payload, ct);
            return Result(response.IsSuccessStatusCode, null);
        }
        catch (Exception ex)
        {
            return Result(false, ex.Message);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Serial mode (RS-232 direct)
    // Protocol: STX | MachineId(2 bytes) | Cmd(2 bytes) | Data(4 bytes) | CRC(2 bytes) | ETX
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<IoTCommandResult> SerialCommandAsync(
        string portConfig, WascomatCommand command, int data, CancellationToken ct)
    {
        // portConfig = "COM3:9600" or "/dev/ttyUSB0:9600:01" (last part = machine address)
        var parts = portConfig.Split(':');
        var portName = parts[0];
        var baudRate = parts.Length > 1 && int.TryParse(parts[1], out var b) ? b : 9600;
        var machineId = parts.Length > 2 ? byte.Parse(parts[2]) : (byte)0x01;

        try
        {
            return await Task.Run(() =>
            {
                using var port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 3000,
                    WriteTimeout = 3000
                };

                port.Open();

                var frame = BuildFrame(machineId, command, data);
                port.Write(frame, 0, frame.Length);

                _logger.LogInformation("Wascomat serial: Sent {Command} to machine {Id} on {Port}",
                    command, machineId, portName);

                // Read ACK (1 byte: 0x06 = ACK, 0x15 = NACK)
                var ack = port.ReadByte();
                port.Close();

                return ack == 0x06
                    ? Result(true, null)
                    : Result(false, "Machine returned NACK — check machine state or cable.");
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wascomat serial: Error on {Port}", portName);
            return Result(false, $"Serial error: {ex.Message}");
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static (string mode, string address) Parse(string connectionString)
    {
        var idx = connectionString.IndexOf(':');
        if (idx < 0) return ("relay", connectionString);
        return (connectionString[..idx].ToLower(), connectionString[(idx + 1)..]);
    }

    private static byte[] BuildFrame(byte machineId, WascomatCommand command, int data)
    {
        // Frame: STX(1) + MachineId(1) + Cmd(1) + Data(2) + CRC(1) + ETX(1)
        var frame = new byte[7];
        frame[0] = 0x02; // STX
        frame[1] = machineId;
        frame[2] = (byte)command;
        frame[3] = (byte)((data >> 8) & 0xFF);
        frame[4] = (byte)(data & 0xFF);
        frame[5] = Crc(frame[1..5]);
        frame[6] = 0x03; // ETX
        return frame;
    }

    private static byte Crc(byte[] data)
    {
        byte crc = 0;
        foreach (var b in data) crc ^= b;
        return crc;
    }

    private static IoTCommandResult Result(bool success, string? error) =>
        new() { Success = success, ErrorMessage = error, Timestamp = DateTime.UtcNow };

    private enum WascomatCommand : byte
    {
        Start  = 0x01,
        Stop   = 0x02,
        Pause  = 0x03,
        Status = 0x04,
    }

    private record RelayStatusResponse(bool Running, int? RemainingMinutes, string State);
}
