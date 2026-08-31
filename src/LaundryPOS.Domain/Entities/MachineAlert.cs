using LaundryPOS.Domain.Enums;

namespace LaundryPOS.Domain.Entities;

public class MachineAlert : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public bool IsRead { get; set; } = false;
    public bool IsResolved { get; set; } = false;
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }

    // Foreign Keys
    public Guid MachineId { get; set; }
    public Guid BranchId { get; set; }

    // Navigation properties
    public Machine Machine { get; set; } = null!;
    public Branch Branch { get; set; } = null!;
}
