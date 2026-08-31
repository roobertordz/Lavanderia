using LaundryPOS.Domain.Enums;

namespace LaundryPOS.Domain.Entities;

/// <summary>
/// "Lavado por encargo" — a drop-off wash order (the customer leaves clothes
/// or a comforter and picks them up later), as opposed to the self-service
/// machine rental flow (Machine/Transaction). Supports two pricing modes,
/// selected via ServiceType:
///   - ByWeight:   TotalPrice = WeightKg * PricePerKg
///   - Comforter:  TotalPrice = ComforterCount * PricePerComforter
/// </summary>
public class LaundryOrder : AuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public LaundryOrderServiceType ServiceType { get; set; }
    public LaundryOrderStatus Status { get; set; } = LaundryOrderStatus.Received;

    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }

    // ByWeight fields
    public decimal? WeightKg { get; set; }
    public decimal? PricePerKg { get; set; }

    // Comforter fields
    public int? ComforterCount { get; set; }
    public string? ComforterSize { get; set; } // e.g. "individual", "matrimonial", "queen", "king"
    public decimal? PricePerComforter { get; set; }

    public decimal TotalPrice { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EstimatedReadyAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? Notes { get; set; }

    // Foreign Keys
    public Guid BranchId { get; set; }
    public Guid? ProcessedByUserId { get; set; }

    // Navigation properties
    public Branch Branch { get; set; } = null!;
    public User? ProcessedByUser { get; set; }
}
