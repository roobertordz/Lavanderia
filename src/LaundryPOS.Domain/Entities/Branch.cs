using LaundryPOS.Domain.Enums;

namespace LaundryPOS.Domain.Entities;

public class Branch : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? TimeZone { get; set; }
    public string? OpeningTime { get; set; }
    public string? ClosingTime { get; set; }
    public decimal TaxRate { get; set; }
    public string Currency { get; set; } = "MXN";
    public int GracePeriodMinutes { get; set; } = 5;

    // Navigation properties
    public ICollection<Machine> Machines { get; set; } = new List<Machine>();
    public ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
