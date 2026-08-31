# Arquitectura del Sistema LaundryPOS

## 1. Principios de Diseño

### Clean Architecture (Arquitectura Limpia)
El sistema sigue estrictamente Clean Architecture con 4 capas concéntricas:

```
┌─────────────────────────────────────────────┐
│              PRESENTATION                    │
│        (API Controllers, SignalR)            │
├─────────────────────────────────────────────┤
│             INFRASTRUCTURE                   │
│    (EF Core, Payments, IoT, Auth)           │
├─────────────────────────────────────────────┤
│              APPLICATION                     │
│    (CQRS Handlers, Validators, DTOs)        │
├─────────────────────────────────────────────┤
│                DOMAIN                        │
│   (Entities, Interfaces, Events, Enums)     │
└─────────────────────────────────────────────┘
```

**Regla de dependencia**: Las capas internas NUNCA dependen de las externas.

### Principios SOLID Aplicados

| Principio | Aplicación |
|-----------|-----------|
| **S** - Single Responsibility | Cada handler maneja una sola operación CQRS |
| **O** - Open/Closed | Payment gateways e IoT drivers extensibles sin modificar código existente |
| **L** - Liskov Substitution | Todas las implementaciones son intercambiables a través de interfaces |
| **I** - Interface Segregation | Interfaces específicas por repositorio (IMachineRepository vs IRepository) |
| **D** - Dependency Inversion | Todo se inyecta via DI; el dominio define interfaces, infrastructure implementa |

### Patrones de Diseño Utilizados

| Patrón | Uso |
|--------|-----|
| **CQRS** | Separación de comandos y consultas via MediatR |
| **Repository** | Abstracción del acceso a datos |
| **Unit of Work** | Transaccionalidad atómica entre repositorios |
| **Factory** | PaymentGatewayFactory, IoTDriverFactory |
| **Strategy** | Diferentes gateways de pago / drivers IoT |
| **Observer** | Eventos de dominio + SignalR real-time |
| **Mediator** | MediatR para desacoplamiento de handlers |
| **Pipeline** | Validation + Logging behaviors en MediatR |

---

## 2. Diagrama de Capas

```mermaid
graph TB
    subgraph Presentation["Presentation Layer"]
        API[API Controllers]
        HUBS[SignalR Hubs]
        MW[Middleware]
    end

    subgraph Application["Application Layer"]
        CMD[Commands]
        QRY[Queries]
        VAL[Validators]
        BHV[Behaviors]
    end

    subgraph Domain["Domain Layer"]
        ENT[Entities]
        IFACE[Interfaces]
        EVT[Domain Events]
        ENUM[Enums]
    end

    subgraph Infrastructure["Infrastructure Layer"]
        EF[EF Core / DbContext]
        REPO[Repositories]
        PAY[Payment Gateways]
        IOT[IoT Drivers]
        AUTH[JWT / BCrypt]
    end

    subgraph External["External Systems"]
        DB[(SQL Server)]
        MQTT[MQTT Broker]
        STRIPE[Stripe API]
        ESP[ESP32 / PLC]
    end

    API --> CMD
    API --> QRY
    HUBS --> QRY
    CMD --> IFACE
    QRY --> IFACE
    VAL --> CMD
    BHV --> CMD
    BHV --> QRY
    REPO --> EF
    EF --> DB
    PAY --> STRIPE
    IOT --> MQTT
    IOT --> ESP
    REPO -.implements.-> IFACE
    PAY -.implements.-> IFACE
    IOT -.implements.-> IFACE
    AUTH -.implements.-> IFACE
```

---

## 3. Flujo de Pago (Core Business Flow)

```mermaid
sequenceDiagram
    participant K as Kiosco/Cliente
    participant API as API Server
    participant DB as Database
    participant PG as Payment Gateway
    participant IOT as IoT Controller
    participant WS as SignalR Hub

    K->>API: POST /api/payments/process
    API->>DB: Validar máquina disponible
    DB-->>API: Machine (Available, IoT controller)
    API->>DB: Crear transacción (Pending)
    API->>PG: ProcessPayment()
    PG-->>API: PaymentResult (Auth #)
    API->>DB: Actualizar transacción (Authorized)
    API->>IOT: StartMachine(duration)
    IOT-->>API: CommandResult (Success)
    API->>DB: Actualizar máquina (InCycle)
    API->>DB: Actualizar transacción (InProgress)
    API->>WS: NotifyMachineStatusChanged()
    API->>WS: NotifyDashboardUpdate()
    API-->>K: TransactionDto (Success)
    
    Note over IOT: Ciclo de lavado...
    IOT->>API: Status: Completed
    API->>DB: Máquina → Available
    API->>WS: NotifyMachineStatusChanged()
```

---

## 4. Modelo de Datos

```mermaid
erDiagram
    Branch ||--o{ Machine : contains
    Branch ||--o{ Transaction : has
    Branch ||--o{ UserBranch : assigned
    Branch ||--o{ IoTController : manages
    
    Machine ||--o{ Transaction : generates
    Machine ||--o{ MaintenanceRecord : tracks
    Machine ||--o{ MachineAlert : triggers
    Machine }o--|| IoTController : controlled_by
    
    User ||--o{ UserBranch : assigned
    User ||--o{ UserPermission : has
    
    Transaction }o--o| Promotion : uses
    Transaction }o--o| User : processed_by
    
    MaintenanceRecord }o--o| User : technician

    Branch {
        guid Id PK
        string Name
        string Code UK
        string Address
        decimal TaxRate
        string Currency
    }

    Machine {
        guid Id PK
        int Number
        string Name
        enum Type
        decimal Price
        int DurationMinutes
        enum Status
        guid BranchId FK
        guid IoTControllerId FK
    }

    Transaction {
        guid Id PK
        string TransactionNumber UK
        decimal TotalAmount
        enum PaymentMethod
        enum Status
        string AuthorizationNumber
        guid MachineId FK
        guid BranchId FK
    }

    User {
        guid Id PK
        string Username UK
        string Email UK
        string PasswordHash
        enum Role
    }

    IoTController {
        guid Id PK
        string Name
        enum ControllerType
        string ConnectionString
        enum Status
        guid BranchId FK
    }

    MaintenanceRecord {
        guid Id PK
        string Title
        enum Type
        enum Status
        guid MachineId FK
        guid TechnicianId FK
    }

    MachineAlert {
        guid Id PK
        string Message
        enum Severity
        bool IsResolved
        guid MachineId FK
    }
```

---

## 5. Comunicación IoT

```mermaid
graph LR
    subgraph Server["Servidor LaundryPOS"]
        API[API Server]
        DRIVER[IoT Driver Factory]
    end

    subgraph Protocols["Protocolos"]
        MQTT[MQTT Broker]
        REST[REST API]
        SIGNALR[SignalR]
    end

    subgraph Devices["Controladores"]
        ESP32[ESP32]
        PLC[PLC Industrial]
        RPI[Raspberry Pi]
        GW[Gateway Industrial]
    end

    API --> DRIVER
    DRIVER --> MQTT
    DRIVER --> REST
    DRIVER --> SIGNALR
    MQTT --> ESP32
    MQTT --> RPI
    REST --> ESP32
    REST --> RPI
    REST --> PLC
    SIGNALR --> GW
```

### Protocolo de Comunicación

| Comando | Descripción | Parámetros |
|---------|-------------|-----------|
| `StartMachine` | Iniciar ciclo | `duration_minutes` |
| `StopMachine` | Detener máquina | - |
| `PauseMachine` | Pausar ciclo | - |
| `RestartController` | Reiniciar controlador | - |
| `Heartbeat` | Verificar conexión | - |
| `Status` | Consultar estado | - |

### Tópicos MQTT

```
laundrypos/{branchId}/{machineId}/command    → Enviar comandos
laundrypos/{branchId}/{machineId}/status     → Recibir estados
laundrypos/{branchId}/{machineId}/heartbeat  → Heartbeat
laundrypos/{branchId}/{machineId}/alert      → Alertas
```

---

## 6. Seguridad

### Autenticación y Autorización

```mermaid
sequenceDiagram
    participant C as Cliente
    participant API as API Server
    participant JWT as JWT Service
    participant DB as Database

    C->>API: POST /auth/login (user, pass)
    API->>DB: Buscar usuario
    API->>API: Verificar BCrypt hash
    API->>JWT: Generar Access Token (1h)
    API->>JWT: Generar Refresh Token (7d)
    API->>DB: Guardar Refresh Token
    API-->>C: { accessToken, refreshToken, user }

    Note over C: Token expira...
    C->>API: POST /auth/refresh
    API->>DB: Validar Refresh Token
    API->>JWT: Nuevo Access Token
    API-->>C: { newAccessToken, newRefreshToken }
```

### Políticas de Autorización

| Política | Roles |
|----------|-------|
| `AdminOnly` | Administrator |
| `SupervisorOrAbove` | Administrator, Supervisor |
| `EmployeeOrAbove` | Administrator, Supervisor, Employee |
| `TechnicianAccess` | Administrator, Supervisor, Technician |

### Medidas de Seguridad
- Passwords hasheados con BCrypt (work factor 12)
- JWT con firma HMAC-SHA256
- Refresh tokens con rotación
- Lockout automático tras 5 intentos fallidos (15 min)
- Soft-delete en todas las entidades
- Audit log de todas las operaciones
- Query filters para datos eliminados
- CORS configurado
- HTTPS enforced

---

## 7. Escalabilidad

### Estrategia Multi-Tenant (por Sucursal)

```
┌─────────────────────────────────────┐
│         Load Balancer (Nginx)       │
├──────────┬──────────┬───────────────┤
│  API #1  │  API #2  │    API #N    │
│  (Pod)   │  (Pod)   │    (Pod)     │
├──────────┴──────────┴───────────────┤
│          SQL Server Cluster         │
│      (Branch-level partitioning)    │
└─────────────────────────────────────┘
```

- **Horizontal**: Múltiples instancias API detrás de load balancer
- **Vertical**: SQL Server con partitioning por BranchId
- **Cache**: Redis para dashboard data y configuraciones
- **Queue**: RabbitMQ/Azure Service Bus para procesamiento asíncrono
- **Microservicios**: Preparado para extraer módulos (pagos, IoT) como servicios independientes
