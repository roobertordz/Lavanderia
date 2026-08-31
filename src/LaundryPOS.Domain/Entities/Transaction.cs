using LaundryPOS.Domain.Enums;

namespace LaundryPOS.Domain.Entities;

public class Transaction : AuditableEntity
{
    public string TransactionNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public TransactionStatus Status { get; set; } = TransactionStatus.Created;
    public string? PaymentGateway { get; set; }
    public string? AuthorizationNumber { get; set; }
    public string? PaymentReference { get; set; }
    public string? GatewayTransactionId { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Notes { get; set; }

    // Foreign Keys
    public Guid MachineId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? ProcessedByUserId { get; set; }
    public Guid? PromotionId { get; set; }

    // Navigation properties
    public Machine Machine { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
    public User? ProcessedByUser { get; set; }
    public Promotion? Promotion { get; set; }
}
