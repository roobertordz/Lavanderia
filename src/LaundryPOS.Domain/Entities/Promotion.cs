namespace LaundryPOS.Domain.Entities;

public class Promotion : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal? DiscountFixedAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ApplicableDays { get; set; } // JSON array: ["Monday","Tuesday"]
    public string? ApplicableHoursStart { get; set; }
    public string? ApplicableHoursEnd { get; set; }
    public int? MaxUsageCount { get; set; }
    public int CurrentUsageCount { get; set; }

    // Foreign Keys
    public Guid? BranchId { get; set; } // null = all branches

    // Navigation properties
    public Branch? Branch { get; set; }
}

public class SystemSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string DataType { get; set; } = "string"; // string, int, decimal, bool, json

    // Foreign Keys
    public Guid? BranchId { get; set; } // null = global setting

    // Navigation properties
    public Branch? Branch { get; set; }
}
