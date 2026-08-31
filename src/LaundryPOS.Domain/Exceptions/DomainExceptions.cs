namespace LaundryPOS.Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public class MachineNotAvailableException : DomainException
{
    public MachineNotAvailableException(Guid machineId)
        : base("MACHINE_NOT_AVAILABLE", $"Machine {machineId} is not available for use.") { }
}

public class PaymentFailedException : DomainException
{
    public PaymentFailedException(string reason)
        : base("PAYMENT_FAILED", $"Payment processing failed: {reason}") { }
}

public class IoTCommunicationException : DomainException
{
    public IoTCommunicationException(Guid controllerId, string reason)
        : base("IOT_COMMUNICATION_ERROR", $"Failed to communicate with controller {controllerId}: {reason}") { }
}

public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entity, Guid id)
        : base("ENTITY_NOT_FOUND", $"{entity} with ID {id} was not found.") { }
}

public class UnauthorizedBranchAccessException : DomainException
{
    public UnauthorizedBranchAccessException(Guid userId, Guid branchId)
        : base("UNAUTHORIZED_BRANCH", $"User {userId} does not have access to branch {branchId}.") { }
}
