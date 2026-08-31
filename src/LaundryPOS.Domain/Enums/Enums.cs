namespace LaundryPOS.Domain.Enums;

public enum MachineStatus
{
    Available = 0,
    Occupied = 1,
    InCycle = 2,
    Finished = 3,
    OutOfService = 4,
    Error = 5,
    Maintenance = 6
}

public enum MachineType
{
    Washer = 0,
    Dryer = 1
}

public enum PaymentMethod
{
    Cash = 0,
    CreditCard = 1,
    DebitCard = 2,
    DigitalWallet = 3,
    BankTransfer = 4
}

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Authorized = 2,
    Completed = 3,
    Failed = 4,
    Refunded = 5,
    Cancelled = 6
}

public enum TransactionStatus
{
    Created = 0,
    PaymentPending = 1,
    PaymentAuthorized = 2,
    MachineStarting = 3,
    InProgress = 4,
    Completed = 5,
    Failed = 6,
    Cancelled = 7,
    Refunded = 8
}

public enum UserRole
{
    Administrator = 0,
    Supervisor = 1,
    Employee = 2,
    Technician = 3,
    Cashier = 4
}

public enum MaintenanceType
{
    Preventive = 0,
    Corrective = 1,
    PartReplacement = 2,
    Emergency = 3
}

public enum MaintenanceStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2,
    Emergency = 3
}

public enum IoTControllerType
{
    ESP32 = 0,
    PLC = 1,
    RaspberryPi = 2,
    IndustrialGateway = 3,
    Wascomat = 4
}

public enum CommunicationStatus
{
    Online = 0,
    Offline = 1,
    Intermittent = 2,
    Error = 3
}

public enum ProductCategory
{
    Detergent = 0,
    FabricSoftener = 1,
    Bleach = 2,
    StainRemover = 3,
    Bags = 4,
    Accessories = 5,
    Other = 6
}

public enum StockMovementType
{
    InitialStock = 0,
    Purchase = 1,
    Sale = 2,
    ManualAdjustment = 3,
    Import = 4,
    Return = 5
}
