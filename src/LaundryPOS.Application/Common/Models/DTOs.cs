using LaundryPOS.Domain.Enums;

namespace LaundryPOS.Application.Common.Models;

// ─── Machine DTOs ───
public record MachineDto
{
    public Guid Id { get; init; }
    public int Number { get; init; }
    public string Name { get; init; } = string.Empty;
    public MachineType Type { get; init; }
    public string Capacity { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int DurationMinutes { get; init; }
    public MachineStatus Status { get; init; }
    public string Location { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? Model { get; init; }
    public string? Brand { get; init; }
    public string? SerialNumber { get; init; }
    public CommunicationStatus CommunicationStatus { get; init; }
    public DateTime? LastHeartbeat { get; init; }
    public int TotalCycles { get; init; }
    public double TotalHoursWorked { get; init; }
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public Guid? IoTControllerId { get; init; }
}

// ─── Transaction DTOs ───
public record TransactionDto
{
    public Guid Id { get; init; }
    public string TransactionNumber { get; init; } = string.Empty;
    public DateTime TransactionDate { get; init; }
    public decimal Amount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal? DiscountAmount { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public TransactionStatus Status { get; init; }
    public string? PaymentGateway { get; init; }
    public string? AuthorizationNumber { get; init; }
    public int DurationMinutes { get; init; }
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public Guid MachineId { get; init; }
    public string MachineName { get; init; } = string.Empty;
    public int MachineNumber { get; init; }
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
}

// ─── Branch DTOs ───
public record BranchDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
    public decimal TaxRate { get; init; }
    public int TotalMachines { get; init; }
    public int AvailableMachines { get; init; }
    public bool IsActive { get; init; }
}

// ─── Dashboard DTOs ───
public record DashboardDto
{
    public decimal TodaySales { get; init; }
    public decimal MonthSales { get; init; }
    public decimal TotalRevenue { get; init; }
    public int OccupiedMachines { get; init; }
    public int AvailableMachines { get; init; }
    public int OutOfServiceMachines { get; init; }
    public int MaintenanceMachines { get; init; }
    public int TotalMachines { get; init; }
    public int TodayTransactions { get; init; }
    public int ActiveAlerts { get; init; }
    public IReadOnlyList<MachineStatusSummaryDto> MachineStatuses { get; init; } = [];
    public IReadOnlyList<RecentTransactionDto> RecentTransactions { get; init; } = [];
    public IReadOnlyList<AlertDto> RecentAlerts { get; init; } = [];
}

public record MachineStatusSummaryDto
{
    public Guid MachineId { get; init; }
    public int Number { get; init; }
    public string Name { get; init; } = string.Empty;
    public MachineType Type { get; init; }
    public MachineStatus Status { get; init; }
    public CommunicationStatus CommunicationStatus { get; init; }
    public int? RemainingMinutes { get; init; }
}

public record RecentTransactionDto
{
    public string TransactionNumber { get; init; } = string.Empty;
    public DateTime TransactionDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string MachineName { get; init; } = string.Empty;
    public TransactionStatus Status { get; init; }
}

public record AlertDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public AlertSeverity Severity { get; init; }
    public DateTime CreatedAt { get; init; }
    public string MachineName { get; init; } = string.Empty;
    public bool IsResolved { get; init; }
}

// ─── User DTOs ───
public record UserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public UserRole Role { get; init; }
    public bool IsActive { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public IReadOnlyList<Guid> BranchIds { get; init; } = [];
}

public record AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserDto User { get; init; } = null!;
}

// ─── Maintenance DTOs ───
public record MaintenanceRecordDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public MaintenanceType Type { get; init; }
    public MaintenanceStatus Status { get; init; }
    public DateTime ScheduledDate { get; init; }
    public DateTime? CompletedDate { get; init; }
    public decimal? Cost { get; init; }
    public string? PartsReplaced { get; init; }
    public Guid MachineId { get; init; }
    public string MachineName { get; init; } = string.Empty;
    public Guid? TechnicianId { get; init; }
    public string? TechnicianName { get; init; }
}

// ─── Report DTOs ───
public record RevenueReportDto
{
    public DateTime Date { get; init; }
    public decimal Revenue { get; init; }
    public int TransactionCount { get; init; }
}

public record MachineUsageReportDto
{
    public Guid MachineId { get; init; }
    public string MachineName { get; init; } = string.Empty;
    public int TotalUses { get; init; }
    public decimal TotalRevenue { get; init; }
    public double AverageUsageMinutes { get; init; }
    public int ErrorCount { get; init; }
}

public record BranchRevenueReportDto
{
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public decimal TotalRevenue { get; init; }
    public int TotalTransactions { get; init; }
}

// ─── Product DTOs ───
public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public ProductCategory Category { get; init; }
    public string Presentation { get; init; } = string.Empty;
    public string? Sku { get; init; }
    public string? Barcode { get; init; }
    public string? ImageUrl { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal SalePrice { get; init; }
    public int StockQuantity { get; init; }
    public int MinStockThreshold { get; init; }
    public bool IsLowStock { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
}

public record StockMovementDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public StockMovementType Type { get; init; }
    public int Quantity { get; init; }
    public int PreviousStock { get; init; }
    public int NewStock { get; init; }
    public string? Reason { get; init; }
    public string? UserName { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ProductImportSummaryDto
{
    public int Imported { get; init; }
    public int Failed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}

// ─── LaundryOrder DTOs ("Lavado por encargo") ───
public record LaundryOrderDto
{
    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public LaundryOrderServiceType ServiceType { get; init; }
    public LaundryOrderStatus Status { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public decimal? WeightKg { get; init; }
    public decimal? PricePerKg { get; init; }
    public int? ComforterCount { get; init; }
    public string? ComforterSize { get; init; }
    public decimal? PricePerComforter { get; init; }
    public decimal TotalPrice { get; init; }
    public PaymentMethod? PaymentMethod { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public DateTime ReceivedAt { get; init; }
    public DateTime? EstimatedReadyAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public string? Notes { get; init; }
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public string? ProcessedByUserName { get; init; }
}

