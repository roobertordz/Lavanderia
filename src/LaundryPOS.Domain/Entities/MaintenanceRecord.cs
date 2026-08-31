using LaundryPOS.Domain.Enums;

namespace LaundryPOS.Domain.Entities;

public class MaintenanceRecord : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MaintenanceType Type { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;
    public DateTime ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public decimal? Cost { get; set; }
    public string? PartsReplaced { get; set; }
    public double HoursWorkedAtService { get; set; }
    public int CyclesAtService { get; set; }
    public string? Notes { get; set; }
    public string? TechnicianNotes { get; set; }

    // Foreign Keys
    public Guid MachineId { get; set; }
    public Guid? TechnicianId { get; set; }
    public Guid BranchId { get; set; }

    // Navigation properties
    public Machine Machine { get; set; } = null!;
    public User? Technician { get; set; }
    public Branch Branch { get; set; } = null!;
}
