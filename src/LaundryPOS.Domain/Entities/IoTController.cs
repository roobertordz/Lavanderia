using LaundryPOS.Domain.Enums;

namespace LaundryPOS.Domain.Entities;

public class IoTController : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public IoTControllerType ControllerType { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? ProtocolType { get; set; } // MQTT, REST, SignalR
    public string? ConnectionString { get; set; }
    public string? MqttTopic { get; set; }
    public CommunicationStatus Status { get; set; } = CommunicationStatus.Offline;
    public DateTime? LastHeartbeat { get; set; }
    public DateTime? LastCommandSent { get; set; }
    public string? LastCommandResult { get; set; }

    // Foreign Keys
    public Guid BranchId { get; set; }

    // Navigation properties
    public Branch Branch { get; set; } = null!;
    public ICollection<Machine> Machines { get; set; } = new List<Machine>();
}
