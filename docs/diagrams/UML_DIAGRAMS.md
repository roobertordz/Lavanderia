# Diagramas UML - LaundryPOS

## 1. Diagrama de Casos de Uso

```mermaid
graph LR
    subgraph Actores
        C((Cliente))
        A((Administrador))
        S((Supervisor))
        E((Empleado))
        T((Técnico))
        SYS((Sistema))
    end

    subgraph "Proceso de Pago"
        UC1[Seleccionar Máquina]
        UC2[Realizar Pago]
        UC3[Iniciar Máquina]
    end

    subgraph "Administración"
        UC4[Gestionar Máquinas]
        UC5[Gestionar Sucursales]
        UC6[Gestionar Usuarios]
        UC7[Configurar Sistema]
    end

    subgraph "Monitoreo"
        UC8[Ver Dashboard]
        UC9[Ver Mapa de Máquinas]
        UC10[Gestionar Alertas]
    end

    subgraph "Reportes"
        UC11[Generar Reportes]
        UC12[Exportar Reportes]
    end

    subgraph "Mantenimiento"
        UC13[Programar Mantenimiento]
        UC14[Registrar Mantenimiento]
        UC15[Alerta Automática]
    end

    subgraph "Auth"
        UC16[Iniciar Sesión]
        UC17[Gestionar Permisos]
    end

    C --> UC1
    C --> UC2
    UC2 --> UC3

    A --> UC4
    A --> UC5
    A --> UC6
    A --> UC7
    A --> UC8
    A --> UC11
    A --> UC16
    A --> UC17

    S --> UC8
    S --> UC9
    S --> UC10
    S --> UC11
    S --> UC12
    S --> UC13
    S --> UC16

    E --> UC8
    E --> UC9
    E --> UC16

    T --> UC14
    T --> UC16

    SYS --> UC15
    SYS --> UC3
```

## 2. Diagrama de Clases (Domain Layer)

```mermaid
classDiagram
    class BaseEntity {
        +Guid Id
        +DateTime CreatedAt
        +DateTime? UpdatedAt
        +string? CreatedBy
        +string? UpdatedBy
        +bool IsActive
    }

    class AuditableEntity {
        +DateTime? DeletedAt
        +string? DeletedBy
        +bool IsDeleted
    }

    class Branch {
        +string Name
        +string Code
        +string Address
        +string City
        +string State
        +decimal TaxRate
        +string Currency
        +int GracePeriodMinutes
        +List~Machine~ Machines
    }

    class Machine {
        +int Number
        +string Name
        +MachineType Type
        +decimal Price
        +int DurationMinutes
        +MachineStatus Status
        +CommunicationStatus CommStatus
        +int TotalCycles
        +double TotalHoursWorked
        +Guid BranchId
        +Guid? IoTControllerId
    }

    class Transaction {
        +string TransactionNumber
        +DateTime TransactionDate
        +decimal Amount
        +decimal TaxAmount
        +decimal TotalAmount
        +PaymentMethod PaymentMethod
        +PaymentStatus PaymentStatus
        +TransactionStatus Status
        +string? AuthorizationNumber
        +DateTime? StartTime
        +DateTime? EndTime
        +Guid MachineId
        +Guid BranchId
    }

    class User {
        +string Username
        +string Email
        +string PasswordHash
        +string FirstName
        +string LastName
        +UserRole Role
        +string? RefreshToken
    }

    class IoTController {
        +string Name
        +IoTControllerType ControllerType
        +string? IpAddress
        +string? ConnectionString
        +CommunicationStatus Status
        +Guid BranchId
    }

    class MaintenanceRecord {
        +string Title
        +string Description
        +MaintenanceType Type
        +MaintenanceStatus Status
        +DateTime ScheduledDate
        +decimal? Cost
        +Guid MachineId
        +Guid? TechnicianId
    }

    class MachineAlert {
        +string Title
        +string Message
        +AlertSeverity Severity
        +bool IsResolved
        +Guid MachineId
    }

    BaseEntity <|-- AuditableEntity
    AuditableEntity <|-- Branch
    AuditableEntity <|-- Machine
    AuditableEntity <|-- Transaction
    AuditableEntity <|-- User
    AuditableEntity <|-- IoTController
    AuditableEntity <|-- MaintenanceRecord
    BaseEntity <|-- MachineAlert

    Branch "1" --> "*" Machine : contains
    Branch "1" --> "*" Transaction : has
    Machine "1" --> "*" Transaction : generates
    Machine "*" --> "0..1" IoTController : controlled by
    Machine "1" --> "*" MaintenanceRecord : tracks
    Machine "1" --> "*" MachineAlert : triggers
    User "1" --> "*" MaintenanceRecord : performs
```

## 3. Diagrama de Secuencia - Proceso de Pago

```mermaid
sequenceDiagram
    actor C as Cliente
    participant K as Kiosco (React)
    participant API as API Server
    participant VAL as Validator
    participant DB as Database
    participant PGW as Payment Gateway
    participant IOT as IoT Controller
    participant SR as SignalR Hub

    C->>K: Toca pantalla
    K->>API: GET /machines/branch/{id}/available
    API->>DB: Query machines WHERE status=Available
    DB-->>API: List<Machine>
    API-->>K: Available machines
    K-->>C: Muestra máquinas disponibles

    C->>K: Selecciona máquina #2
    K-->>C: Muestra precio y detalles

    C->>K: Selecciona "Tarjeta de Crédito"
    C->>K: Toca "Pagar"
    K->>API: POST /payments/process

    API->>VAL: Validate request
    VAL-->>API: OK

    API->>DB: Get machine with controller
    DB-->>API: Machine (Available)

    API->>DB: Calculate tax, create Transaction
    DB-->>API: Transaction created

    API->>PGW: ProcessPayment($98.60 MXN)
    PGW-->>API: Success (Auth: STRIPE-ABC123)

    API->>DB: Update transaction (Authorized)

    API->>IOT: StartMachine(50 minutes)
    IOT-->>API: Success

    API->>DB: Machine.Status = InCycle
    API->>DB: Transaction.Status = InProgress

    API->>SR: NotifyMachineStatusChanged(InCycle)
    API->>SR: NotifyDashboardUpdate()

    API-->>K: TransactionDto (Success)
    K-->>C: ¡Listo! Máquina #2 iniciando - 50 min
```

## 4. Diagrama de Estados - Máquina

```mermaid
stateDiagram-v2
    [*] --> Available : Creación

    Available --> InCycle : Pago exitoso + IoT Start
    Available --> Maintenance : Mantenimiento programado
    Available --> OutOfService : Admin desactiva

    InCycle --> Finished : Ciclo completado
    InCycle --> Error : Falla durante ciclo
    InCycle --> Available : Stop forzado (Admin)

    Finished --> Available : Período de gracia terminado
    Finished --> Available : Admin libera

    Error --> Available : Error resuelto
    Error --> OutOfService : Falla grave

    Maintenance --> Available : Mantenimiento completado

    OutOfService --> Available : Admin reactiva
    OutOfService --> [*] : Eliminación (soft delete)
```

## 5. Diagrama de Componentes

```mermaid
graph TB
    subgraph Frontend["Frontend (React)"]
        DASH[Dashboard Component]
        KIOSK[Kiosk Component]
        MMAP[Machine Map Component]
        STORE[Zustand Store]
        SRHOOK[SignalR Hook]
    end

    subgraph API["API Layer (ASP.NET Core)"]
        AUTH_C[Auth Controller]
        MACH_C[Machines Controller]
        PAY_C[Payments Controller]
        DASH_C[Dashboard Controller]
        REP_C[Reports Controller]
        MAINT_C[Maintenance Controller]
        M_HUB[Machine Hub]
        D_HUB[Dashboard Hub]
    end

    subgraph Application["Application Layer"]
        M_CMD[Machine Commands]
        M_QRY[Machine Queries]
        P_CMD[Payment Commands]
        D_QRY[Dashboard Queries]
        R_QRY[Report Queries]
        MT_CMD[Maintenance Commands]
        U_CMD[User Commands]
    end

    subgraph Infrastructure["Infrastructure"]
        UOW[Unit of Work]
        REPOS[Repositories]
        JWT_SVC[JWT Service]
        PGW_F[Payment Gateway Factory]
        IOT_F[IoT Driver Factory]
        NOTIF[SignalR Notification Service]
    end

    subgraph External["External"]
        SQLDB[(SQL Server)]
        MQTTB[MQTT Broker]
        STRIPE_E[Stripe]
        MP_E[Mercado Pago]
        ESP32_E[ESP32]
    end

    DASH --> DASH_C
    KIOSK --> PAY_C
    SRHOOK --> M_HUB
    SRHOOK --> D_HUB

    AUTH_C --> U_CMD
    MACH_C --> M_CMD
    MACH_C --> M_QRY
    PAY_C --> P_CMD
    DASH_C --> D_QRY
    REP_C --> R_QRY
    MAINT_C --> MT_CMD

    P_CMD --> PGW_F
    P_CMD --> IOT_F
    P_CMD --> UOW
    P_CMD --> NOTIF

    UOW --> REPOS
    REPOS --> SQLDB
    PGW_F --> STRIPE_E
    PGW_F --> MP_E
    IOT_F --> MQTTB
    IOT_F --> ESP32_E
    NOTIF --> M_HUB
    NOTIF --> D_HUB
```

## 6. Diagrama de Despliegue

```mermaid
graph TB
    subgraph Cloud["Producción (Docker / Kubernetes)"]
        subgraph LB["Load Balancer"]
            NGINX[Nginx]
        end

        subgraph App["Application Servers"]
            API1[API Pod 1]
            API2[API Pod 2]
            WEB1[Web Pod]
        end

        subgraph Data["Data Layer"]
            SQL[(SQL Server)]
            REDIS[(Redis Cache)]
        end

        subgraph IoT["IoT Layer"]
            MQTT[Mosquitto MQTT]
        end
    end

    subgraph Devices["Dispositivos en Sucursal"]
        KIOSK_D[Kiosco / Tablet]
        ESP_D[ESP32 Controllers]
        PLC_D[PLC Industrial]
    end

    subgraph Admin["Administradores"]
        BROWSER[Navegador Web]
    end

    BROWSER --> NGINX
    KIOSK_D --> NGINX
    NGINX --> WEB1
    NGINX --> API1
    NGINX --> API2
    API1 --> SQL
    API2 --> SQL
    API1 --> REDIS
    API2 --> REDIS
    API1 --> MQTT
    API2 --> MQTT
    MQTT --> ESP_D
    MQTT --> PLC_D
```
