using LaundryPOS.Domain.Enums;

namespace LaundryPOS.Domain.Entities;

/// <summary>
/// Retail product (detergent, fabric softener, bleach, etc.) sold to customers
/// alongside the machine cycles.
/// </summary>
public class Product : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public ProductCategory Category { get; set; }
    public string Presentation { get; set; } = string.Empty; // e.g. "1L", "5L", "1kg"
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public string? ImageUrl { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public int StockQuantity { get; set; }
    public int MinStockThreshold { get; set; } = 5;
    public string? Notes { get; set; }

    // Foreign Keys
    public Guid BranchId { get; set; }

    // Navigation properties
    public Branch Branch { get; set; } = null!;
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public bool IsLowStock => StockQuantity <= MinStockThreshold;
}
