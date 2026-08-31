using LaundryPOS.Domain.Enums;

namespace LaundryPOS.Domain.Events;

public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public class MachineStatusChangedEvent : DomainEvent
{
    public Guid MachineId { get; init; }
    public Guid BranchId { get; init; }
    public MachineStatus OldStatus { get; init; }
    public MachineStatus NewStatus { get; init; }
}

public class TransactionCompletedEvent : DomainEvent
{
    public Guid TransactionId { get; init; }
    public Guid MachineId { get; init; }
    public Guid BranchId { get; init; }
    public decimal Amount { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
}

public class PaymentAuthorizedEvent : DomainEvent
{
    public Guid TransactionId { get; init; }
    public string AuthorizationNumber { get; init; } = string.Empty;
    public string Gateway { get; init; } = string.Empty;
}

public class MachineStartedEvent : DomainEvent
{
    public Guid MachineId { get; init; }
    public Guid TransactionId { get; init; }
    public int DurationMinutes { get; init; }
}

public class MachineCycleCompletedEvent : DomainEvent
{
    public Guid MachineId { get; init; }
    public Guid TransactionId { get; init; }
}

public class MachineErrorEvent : DomainEvent
{
    public Guid MachineId { get; init; }
    public Guid BranchId { get; init; }
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}

public class AlertCreatedEvent : DomainEvent
{
    public Guid AlertId { get; init; }
    public Guid MachineId { get; init; }
    public Guid BranchId { get; init; }
    public AlertSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class MaintenanceRequiredEvent : DomainEvent
{
    public Guid MachineId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public int TotalCycles { get; init; }
    public double TotalHours { get; init; }
}
