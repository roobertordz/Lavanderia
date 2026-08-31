using LaundryPOS.Domain.Entities;
using LaundryPOS.Domain.Enums;

namespace LaundryPOS.Domain.Interfaces.Services;

/// <summary>
/// Abstraction for payment processing. Each payment gateway implements this interface.
/// </summary>
public interface IPaymentGateway
{
    string GatewayName { get; }
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct = default);
    Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, CancellationToken ct = default);
    Task<PaymentStatusResult> GetPaymentStatusAsync(string transactionId, CancellationToken ct = default);
}

public interface IPaymentGatewayFactory
{
    IPaymentGateway GetGateway(string gatewayName);
    IReadOnlyList<string> GetAvailableGateways();
}

public record PaymentRequest
{
    public string TransactionNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "MXN";
    public PaymentMethod Method { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

public record PaymentResult
{
    public bool Success { get; init; }
    public string? AuthorizationNumber { get; init; }
    public string? GatewayTransactionId { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public PaymentStatus Status { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

public record PaymentStatusResult
{
    public string GatewayTransactionId { get; init; } = string.Empty;
    public PaymentStatus Status { get; init; }
    public string? StatusDetail { get; init; }
}

/// <summary>
/// Abstraction for IoT device communication. Each controller type implements this interface.
/// </summary>
public interface IIoTDeviceDriver
{
    string DriverName { get; }
    Task<IoTCommandResult> StartMachineAsync(string connectionString, int durationMinutes, CancellationToken ct = default);
    Task<IoTCommandResult> StopMachineAsync(string connectionString, CancellationToken ct = default);
    Task<IoTCommandResult> PauseMachineAsync(string connectionString, CancellationToken ct = default);
    Task<IoTCommandResult> RestartControllerAsync(string connectionString, CancellationToken ct = default);
    Task<IoTHeartbeatResult> HeartbeatAsync(string connectionString, CancellationToken ct = default);
    Task<IoTStatusResult> GetStatusAsync(string connectionString, CancellationToken ct = default);
}

public interface IIoTDriverFactory
{
    IIoTDeviceDriver GetDriver(string driverType);
    IReadOnlyList<string> GetAvailableDrivers();
}

public record IoTCommandResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record IoTHeartbeatResult
{
    public bool IsAlive { get; init; }
    public string? FirmwareVersion { get; init; }
    public double? TemperatureCelsius { get; init; }
    public int? UptimeSeconds { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record IoTStatusResult
{
    public bool IsRunning { get; init; }
    public int? RemainingMinutes { get; init; }
    public string? CurrentState { get; init; }
    public string? ErrorCode { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Publishes commands to laundry machines over MQTT and exposes the last
/// known state reported by each machine's ESP32 controller (MQTT is
/// push-based, so there is no live request/response call like HTTP —
/// drivers read the last state cached from the subscribed status topic).
/// </summary>
public interface IMqttPublisherService
{
    bool IsConnected { get; }
    Task PublishCommandAsync(Guid machineId, string action, int? durationMinutes = null, string? cycle = null, CancellationToken ct = default);
    MqttMachineState? GetLastKnownState(Guid machineId);
}

public record MqttMachineState
{
    public bool IsAlive { get; init; }
    public bool IsRunning { get; init; }
    public string CurrentState { get; init; } = "unknown";
    public int? RemainingMinutes { get; init; }
    public int? CapacityKg { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Real-time notification service abstraction (SignalR, WebSocket, etc.)
/// </summary>
public interface IRealTimeNotificationService
{
    Task NotifyMachineStatusChangedAsync(Guid branchId, Guid machineId, MachineStatus status, CancellationToken ct = default);
    Task NotifyTransactionCompletedAsync(Guid branchId, Guid transactionId, CancellationToken ct = default);
    Task NotifyAlertCreatedAsync(Guid branchId, Guid alertId, AlertSeverity severity, string message, CancellationToken ct = default);
    Task NotifyDashboardUpdateAsync(Guid branchId, CancellationToken ct = default);
}

/// <summary>
/// JWT token generation and validation
/// </summary>
public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string username, string role, IEnumerable<Guid> branchIds);
    string GenerateRefreshToken();
    (Guid userId, string username, string role)? ValidateAccessToken(string token);
}

/// <summary>
/// Password hashing service
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>
/// Report export service
/// </summary>
public interface IReportExportService
{
    Task<byte[]> ExportToPdfAsync(string templateName, object data, CancellationToken ct = default);
    Task<byte[]> ExportToExcelAsync(string sheetName, object data, CancellationToken ct = default);
    Task<byte[]> ExportToCsvAsync(object data, CancellationToken ct = default);
}

/// <summary>
/// Excel import/export for the product catalog
/// </summary>
public interface IProductExcelService
{
    byte[] ExportProducts(IEnumerable<Product> products);
    ProductImportResult ImportProducts(Stream fileStream);
}

public record ProductImportRow(
    string Name,
    string Brand,
    string Category,
    string Presentation,
    string? Sku,
    string? Barcode,
    decimal PurchasePrice,
    decimal SalePrice,
    int StockQuantity,
    int MinStockThreshold);

public class ProductImportResult
{
    public List<ProductImportRow> Rows { get; } = new();
    public List<string> Errors { get; } = new();
}

/// <summary>
/// Current user context
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Username { get; }
    string? Role { get; }
    IEnumerable<Guid> BranchIds { get; }
}
