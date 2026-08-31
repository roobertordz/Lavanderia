-- ============================================
-- LaundryPOS - Database Initialization Script
-- SQL Server - Complete Schema
-- ============================================

-- Create database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'LaundryPOS')
BEGIN
    CREATE DATABASE LaundryPOS;
END
GO

USE LaundryPOS;
GO

-- ─── Branches ───
CREATE TABLE Branches (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Name NVARCHAR(200) NOT NULL,
    Code NVARCHAR(20) NOT NULL,
    Address NVARCHAR(500) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    [State] NVARCHAR(100) NOT NULL,
    ZipCode NVARCHAR(20),
    Country NVARCHAR(100) DEFAULT 'México',
    Phone NVARCHAR(20),
    Email NVARCHAR(200),
    TimeZone NVARCHAR(50),
    OpeningTime NVARCHAR(10),
    ClosingTime NVARCHAR(10),
    TaxRate DECIMAL(5,2) DEFAULT 16.00,
    Currency NVARCHAR(10) DEFAULT 'MXN',
    GracePeriodMinutes INT DEFAULT 5,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100),
    UpdatedBy NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1,
    DeletedAt DATETIME2 NULL,
    DeletedBy NVARCHAR(100),
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_Branches_Code UNIQUE (Code)
);
GO

-- ─── IoT Controllers ───
CREATE TABLE IoTControllers (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Name NVARCHAR(100) NOT NULL,
    ControllerType INT NOT NULL, -- 0=ESP32, 1=PLC, 2=RaspberryPi, 3=IndustrialGateway
    IpAddress NVARCHAR(50),
    MacAddress NVARCHAR(20),
    FirmwareVersion NVARCHAR(50),
    ProtocolType NVARCHAR(20), -- MQTT, REST, SignalR
    ConnectionString NVARCHAR(500),
    MqttTopic NVARCHAR(200),
    [Status] INT NOT NULL DEFAULT 1, -- 0=Online, 1=Offline
    LastHeartbeat DATETIME2 NULL,
    LastCommandSent DATETIME2 NULL,
    LastCommandResult NVARCHAR(MAX),
    BranchId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100),
    UpdatedBy NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1,
    DeletedAt DATETIME2 NULL,
    DeletedBy NVARCHAR(100),
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_IoTControllers_Branch FOREIGN KEY (BranchId) REFERENCES Branches(Id)
);
GO

-- ─── Machines ───
CREATE TABLE Machines (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Number INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    [Type] INT NOT NULL, -- 0=Washer, 1=Dryer
    Capacity NVARCHAR(50),
    Price DECIMAL(10,2) NOT NULL,
    DurationMinutes INT NOT NULL,
    [Status] INT NOT NULL DEFAULT 0, -- MachineStatus enum
    Location NVARCHAR(200),
    IpAddress NVARCHAR(50),
    Model NVARCHAR(100),
    Brand NVARCHAR(100),
    SerialNumber NVARCHAR(100),
    LastMaintenanceDate DATETIME2 NULL,
    TotalCycles INT NOT NULL DEFAULT 0,
    TotalHoursWorked FLOAT NOT NULL DEFAULT 0,
    CommunicationStatus INT NOT NULL DEFAULT 1, -- 0=Online, 1=Offline
    LastHeartbeat DATETIME2 NULL,
    BranchId UNIQUEIDENTIFIER NOT NULL,
    IoTControllerId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100),
    UpdatedBy NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1,
    DeletedAt DATETIME2 NULL,
    DeletedBy NVARCHAR(100),
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_Machines_Number_Branch UNIQUE (Number, BranchId),
    CONSTRAINT FK_Machines_Branch FOREIGN KEY (BranchId) REFERENCES Branches(Id),
    CONSTRAINT FK_Machines_Controller FOREIGN KEY (IoTControllerId) REFERENCES IoTControllers(Id)
);
GO

-- ─── Users ───
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(200) NOT NULL,
    PasswordHash NVARCHAR(200) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20),
    [Role] INT NOT NULL, -- 0=Admin, 1=Supervisor, 2=Employee, 3=Technician
    RefreshToken NVARCHAR(500),
    RefreshTokenExpiryTime DATETIME2 NULL,
    LastLoginAt DATETIME2 NULL,
    FailedLoginAttempts INT NOT NULL DEFAULT 0,
    LockoutEnd DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100),
    UpdatedBy NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1,
    DeletedAt DATETIME2 NULL,
    DeletedBy NVARCHAR(100),
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

-- ─── User-Branch Mapping ───
CREATE TABLE UserBranches (
    UserId UNIQUEIDENTIFIER NOT NULL,
    BranchId UNIQUEIDENTIFIER NOT NULL,
    IsPrimary BIT NOT NULL DEFAULT 0,
    CONSTRAINT PK_UserBranches PRIMARY KEY (UserId, BranchId),
    CONSTRAINT FK_UserBranches_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserBranches_Branch FOREIGN KEY (BranchId) REFERENCES Branches(Id) ON DELETE CASCADE
);
GO

-- ─── User Permissions ───
CREATE TABLE UserPermissions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Module NVARCHAR(50) NOT NULL,
    CanRead BIT NOT NULL DEFAULT 0,
    CanWrite BIT NOT NULL DEFAULT 0,
    CanDelete BIT NOT NULL DEFAULT 0,
    CanExport BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100),
    UpdatedBy NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_UserPermissions UNIQUE (UserId, Module),
    CONSTRAINT FK_UserPermissions_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
GO

-- ─── Promotions ───
CREATE TABLE Promotions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Name NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(500),
    DiscountPercentage DECIMAL(5,2) DEFAULT 0,
    DiscountFixedAmount DECIMAL(10,2) NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    ApplicableDays NVARCHAR(200),
    ApplicableHoursStart NVARCHAR(10),
    ApplicableHoursEnd NVARCHAR(10),
    MaxUsageCount INT NULL,
    CurrentUsageCount INT NOT NULL DEFAULT 0,
    BranchId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100),
    UpdatedBy NVARCHAR(100),
    CONSTRAINT FK_Promotions_Branch FOREIGN KEY (BranchId) REFERENCES Branches(Id)
);
GO

-- ─── Transactions ───
CREATE TABLE Transactions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    TransactionNumber NVARCHAR(50) NOT NULL,
    TransactionDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    Amount DECIMAL(10,2) NOT NULL,
    TaxAmount DECIMAL(10,2) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(10,2) NOT NULL,
    DiscountAmount DECIMAL(10,2) NULL,
    PaymentMethod INT NOT NULL, -- PaymentMethod enum
    PaymentStatus INT NOT NULL DEFAULT 0, -- PaymentStatus enum
    [Status] INT NOT NULL DEFAULT 0, -- TransactionStatus enum
    PaymentGateway NVARCHAR(50),
    AuthorizationNumber NVARCHAR(100),
    PaymentReference NVARCHAR(200),
    GatewayTransactionId NVARCHAR(200),
    DurationMinutes INT NOT NULL,
    StartTime DATETIME2 NULL,
    EndTime DATETIME2 NULL,
    ErrorMessage NVARCHAR(500),
    Notes NVARCHAR(1000),
    MachineId UNIQUEIDENTIFIER NOT NULL,
    BranchId UNIQUEIDENTIFIER NOT NULL,
    ProcessedByUserId UNIQUEIDENTIFIER NULL,
    PromotionId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100),
    UpdatedBy NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1,
    DeletedAt DATETIME2 NULL,
    DeletedBy NVARCHAR(100),
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_Transactions_Number UNIQUE (TransactionNumber),
    CONSTRAINT FK_Transactions_Machine FOREIGN KEY (MachineId) REFERENCES Machines(Id),
    CONSTRAINT FK_Transactions_Branch FOREIGN KEY (BranchId) REFERENCES Branches(Id),
    CONSTRAINT FK_Transactions_User FOREIGN KEY (ProcessedByUserId) REFERENCES Users(Id),
    CONSTRAINT FK_Transactions_Promotion FOREIGN KEY (PromotionId) REFERENCES Promotions(Id)
);
GO

-- ─── Maintenance Records ───
CREATE TABLE MaintenanceRecords (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Title NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(2000),
    [Type] INT NOT NULL, -- MaintenanceType enum
    [Status] INT NOT NULL DEFAULT 0, -- MaintenanceStatus enum
    ScheduledDate DATETIME2 NOT NULL,
    CompletedDate DATETIME2 NULL,
    Cost DECIMAL(10,2) NULL,
    PartsReplaced NVARCHAR(1000),
    HoursWorkedAtService FLOAT NOT NULL DEFAULT 0,
    CyclesAtService INT NOT NULL DEFAULT 0,
    Notes NVARCHAR(2000),
    TechnicianNotes NVARCHAR(2000),
    MachineId UNIQUEIDENTIFIER NOT NULL,
    TechnicianId UNIQUEIDENTIFIER NULL,
    BranchId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100),
    UpdatedBy NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1,
    DeletedAt DATETIME2 NULL,
    DeletedBy NVARCHAR(100),
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Maintenance_Machine FOREIGN KEY (MachineId) REFERENCES Machines(Id),
    CONSTRAINT FK_Maintenance_Technician FOREIGN KEY (TechnicianId) REFERENCES Users(Id),
    CONSTRAINT FK_Maintenance_Branch FOREIGN KEY (BranchId) REFERENCES Branches(Id)
);
GO

-- ─── Machine Alerts ───
CREATE TABLE MachineAlerts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Title NVARCHAR(200) NOT NULL,
    [Message] NVARCHAR(1000) NOT NULL,
    Severity INT NOT NULL, -- AlertSeverity enum
    IsRead BIT NOT NULL DEFAULT 0,
    IsResolved BIT NOT NULL DEFAULT 0,
    ResolvedAt DATETIME2 NULL,
    ResolvedBy NVARCHAR(100),
    MachineId UNIQUEIDENTIFIER NOT NULL,
    BranchId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100),
    UpdatedBy NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_Alerts_Machine FOREIGN KEY (MachineId) REFERENCES Machines(Id),
    CONSTRAINT FK_Alerts_Branch FOREIGN KEY (BranchId) REFERENCES Branches(Id)
);
GO

-- ─── System Settings ───
CREATE TABLE SystemSettings (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    [Key] NVARCHAR(100) NOT NULL,
    [Value] NVARCHAR(2000) NOT NULL,
    [Description] NVARCHAR(500),
    Category NVARCHAR(50) NOT NULL,
    DataType NVARCHAR(20) DEFAULT 'string',
    BranchId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100),
    UpdatedBy NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Settings_Key_Branch UNIQUE ([Key], BranchId),
    CONSTRAINT FK_Settings_Branch FOREIGN KEY (BranchId) REFERENCES Branches(Id) ON DELETE CASCADE
);
GO

-- ─── Audit Logs ───
CREATE TABLE AuditLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    EntityName NVARCHAR(100) NOT NULL,
    EntityId UNIQUEIDENTIFIER NOT NULL,
    [Action] NVARCHAR(50) NOT NULL,
    OldValues NVARCHAR(MAX),
    NewValues NVARCHAR(MAX),
    UserId NVARCHAR(100),
    UserName NVARCHAR(100),
    IpAddress NVARCHAR(50),
    BranchId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CreatedBy NVARCHAR(100),
    UpdatedBy NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- ─── Performance Indexes ───
CREATE INDEX IX_Transactions_Branch_Date ON Transactions(BranchId, TransactionDate);
CREATE INDEX IX_Transactions_Machine_Date ON Transactions(MachineId, TransactionDate);
CREATE INDEX IX_Machines_Branch_Status ON Machines(BranchId, [Status]) WHERE IsDeleted = 0;
CREATE INDEX IX_Alerts_Branch_Resolved ON MachineAlerts(BranchId, IsResolved);
CREATE INDEX IX_AuditLogs_Entity ON AuditLogs(EntityName, EntityId);
CREATE INDEX IX_MaintenanceRecords_Machine ON MaintenanceRecords(MachineId);
CREATE INDEX IX_MaintenanceRecords_Branch_Date ON MaintenanceRecords(BranchId, ScheduledDate);
GO

-- ============================================
-- Seed Data
-- ============================================

-- Default admin user (password: Admin@123456)
-- BCrypt hash for: Admin@123456
INSERT INTO Branches (Id, Name, Code, Address, City, [State], ZipCode, Country, Phone, Email, TaxRate, Currency, GracePeriodMinutes, OpeningTime, ClosingTime)
VALUES (
    'A0000001-0000-0000-0000-000000000001',
    'Sucursal Centro',
    'SUC-001',
    'Av. Reforma 100, Col. Centro',
    'Ciudad de México',
    'CDMX',
    '06000',
    'México',
    '55-1234-5678',
    'centro@laundrypos.com',
    16.00,
    'MXN',
    5,
    '07:00',
    '22:00'
);
GO

INSERT INTO Users (Id, Username, Email, PasswordHash, FirstName, LastName, [Role])
VALUES (
    'B0000001-0000-0000-0000-000000000001',
    'admin',
    'admin@laundrypos.com',
    '$2a$12$AB9MW9anH5aRq/eVEQU3Xe.hKtbK2cXuMNo4kxBTFk3ayOrxhd8oO', -- Admin@123456
    'Administrador',
    'Sistema',
    0 -- Administrator
);
GO

INSERT INTO UserBranches (UserId, BranchId, IsPrimary)
VALUES ('B0000001-0000-0000-0000-000000000001', 'A0000001-0000-0000-0000-000000000001', 1);
GO

-- Default system settings
INSERT INTO SystemSettings ([Key], [Value], [Description], Category, DataType) VALUES
('system.name', 'LaundryPOS', 'Nombre del sistema', 'General', 'string'),
('system.currency', 'MXN', 'Moneda predeterminada', 'General', 'string'),
('system.tax_rate', '16', 'Tasa de impuesto predeterminada (%)', 'General', 'decimal'),
('system.grace_period', '5', 'Minutos de gracia después de terminar el ciclo', 'General', 'int'),
('iot.heartbeat_interval', '30', 'Intervalo de heartbeat en segundos', 'IoT', 'int'),
('iot.command_timeout', '10', 'Timeout de comandos IoT en segundos', 'IoT', 'int'),
('maintenance.cycle_alert', '1000', 'Ciclos para alerta de mantenimiento preventivo', 'Maintenance', 'int'),
('maintenance.hours_alert', '500', 'Horas para alerta de mantenimiento preventivo', 'Maintenance', 'int'),
('notifications.email_enabled', 'false', 'Habilitar notificaciones por email', 'Notifications', 'bool'),
('notifications.sms_enabled', 'false', 'Habilitar notificaciones por SMS', 'Notifications', 'bool');
GO

PRINT 'LaundryPOS database created successfully.';
GO
