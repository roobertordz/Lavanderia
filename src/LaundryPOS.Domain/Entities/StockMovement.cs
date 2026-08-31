using LaundryPOS.Domain.Enums;

namespace LaundryPOS.Domain.Entities;

/// <summary>
/// Audit trail entry for any change to a product's stock quantity
/// (manual adjustment, sale, purchase, Excel import, etc.).
/// </summary>
public class StockMovement : BaseEntity
{
    public Guid ProductId { get; set; }
    public StockMovementType Type { get; set; }
    public int Quantity { get; set; } // signed: positive = in, negative = out
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    public string? Reason { get; set; }
    public Guid? UserId { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
    public User? User { get; set; }
}
