using LaundryPOS.Domain.Enums;

namespace LaundryPOS.Domain.Entities;

public class Machine : AuditableEntity
{
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public MachineType Type { get; set; }
    public string Capacity { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; }
    public MachineStatus Status { get; set; } = MachineStatus.Available;
    public string Location { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Model { get; set; }
    public string? Brand { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? LastMaintenanceDate { get; set; }
    public int TotalCycles { get; set; }
    public double TotalHoursWorked { get; set; }
    public CommunicationStatus CommunicationStatus { get; set; } = CommunicationStatus.Offline;
    public DateTime? LastHeartbeat { get; set; }

    // Foreign Keys
    public Guid BranchId { get; set; }
    public Guid? IoTControllerId { get; set; }

    // Navigation properties
    public Branch Branch { get; set; } = null!;
    public IoTController? IoTController { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = new List<MaintenanceRecord>();
    public ICollection<MachineAlert> Alerts { get; set; } = new List<MachineAlert>();
}
