# LaundryPOS - Sistema de Administración de Lavanderías de Autoservicio

## Visión General

**LaundryPOS** es un sistema SaaS profesional para la administración integral de lavanderías de autoservicio. Elimina completamente el uso de tarjetas RFID, monederos electrónicos, fichas y códigos QR. Todo el funcionamiento se administra desde el software.

### Flujo del Cliente
1. El cliente llega a la lavandería
2. Selecciona una máquina disponible desde el kiosco/pantalla táctil
3. Realiza el pago (tarjeta, efectivo, wallet digital)
4. La máquina inicia automáticamente
5. No requiere registro, tarjetas ni recargas

---

## Arquitectura

```
┌─────────────────────────────────────────────────────┐
│                    FRONTEND                          │
│         React + TypeScript + TailwindCSS             │
│     Dashboard Admin │ Kiosco │ Reportes             │
│              SignalR WebSocket Client                │
└──────────────────────┬──────────────────────────────┘
                       │ HTTPS / WSS
┌──────────────────────▼──────────────────────────────┐
│                  API GATEWAY                         │
│             ASP.NET Core 9 Web API                  │
│    Controllers │ SignalR Hubs │ JWT Auth             │
│           Swagger │ Serilog │ Middleware             │
├─────────────────────────────────────────────────────┤
│               APPLICATION LAYER                      │
│          MediatR (CQRS) │ FluentValidation          │
│     Commands │ Queries │ Behaviors │ DTOs           │
├─────────────────────────────────────────────────────┤
│                 DOMAIN LAYER                         │
│    Entities │ Enums │ Events │ Exceptions           │
│         Interfaces (Repositories │ Services)         │
├─────────────────────────────────────────────────────┤
│              INFRASTRUCTURE LAYER                    │
│   EF Core │ SQL Server │ Repository Pattern         │
│   JWT │ BCrypt │ Payment Gateways │ IoT Drivers     │
└───────┬──────────────────┬──────────────────────────┘
        │                  │
┌───────▼───────┐  ┌───────▼───────────┐
│  SQL Server   │  │   IoT Controllers  │
│   Database    │  │  ESP32 │ PLC │ RPi │
│               │  │    MQTT │ REST     │
└───────────────┘  └───────────────────┘
```

---

## Stack Tecnológico

| Capa | Tecnología |
|------|-----------|
| Backend | ASP.NET Core 9, C# |
| ORM | Entity Framework Core 9 |
| Database | SQL Server 2022 |
| Frontend | React 19, TypeScript, TailwindCSS, Vite |
| Real-time | SignalR (WebSockets) |
| Auth | JWT + Refresh Tokens, BCrypt |
| IoT | ESP32, MQTT (MQTTnet), REST API |
| Patterns | Clean Architecture, CQRS, Repository, Unit of Work |
| Validation | FluentValidation |
| Mediator | MediatR |
| Logging | Serilog |
| API Docs | Swagger/OpenAPI |
| Containers | Docker, Docker Compose |
| CI/CD | GitHub Actions |

---

## Estructura del Proyecto

```
LaundryPOS/
├── src/
│   ├── LaundryPOS.Domain/          # Entidades, Enums, Interfaces, Eventos
│   ├── LaundryPOS.Application/     # CQRS, Handlers, Validadores, DTOs
│   ├── LaundryPOS.Infrastructure/  # EF Core, Repos, Pagos, IoT, Auth
│   ├── LaundryPOS.IoT/            # Drivers de controladores IoT
│   ├── LaundryPOS.API/            # Controllers, Hubs, Middleware
│   └── LaundryPOS.Web/            # React Frontend (Vite)
├── tests/
│   ├── LaundryPOS.Domain.Tests/
│   ├── LaundryPOS.Application.Tests/
│   ├── LaundryPOS.Infrastructure.Tests/
│   └── LaundryPOS.API.Tests/
├── scripts/database/               # SQL Scripts
├── docker/                         # Dockerfiles
├── docs/                           # Documentación
├── docker-compose.yml
└── LaundryPOS.sln
```

---

## Módulos

| # | Módulo | Descripción |
|---|--------|-------------|
| 1 | Dashboard | KPIs en tiempo real, ventas, estados de máquinas |
| 2 | Máquinas | CRUD, configuración, precios, tiempos |
| 3 | Control Real-Time | Mapa visual con WebSockets |
| 4 | Pagos | Flujo completo: selección → pago → inicio |
| 5 | IoT | Comunicación desacoplada con controladores |
| 6 | Historial | Registro completo de transacciones |
| 7 | Reportes | Ventas, uso, ingresos + exportación PDF/Excel/CSV |
| 8 | Mantenimiento | Preventivo, correctivo, bitácora |
| 9 | Usuarios | Roles y permisos (Admin, Supervisor, Empleado, Técnico) |
| 10 | Sucursales | Multi-branch desde un solo sistema |
| 11 | Configuración | Impuestos, moneda, horarios, promociones |

---

## Inicio Rápido

### Con Docker (Recomendado)
```bash
git clone <repo>
cd LaundryPOS
docker compose up -d
```

- **API**: http://localhost:5000
- **Frontend**: http://localhost:3000
- **Swagger**: http://localhost:5000/swagger
- **SQL Server**: localhost:1433
- **MQTT**: localhost:1883

### Credenciales Iniciales
- **Usuario**: admin
- **Password**: Admin@123456

### Sin Docker
```bash
# Backend (aplica migraciones EF Core y crea el usuario admin automáticamente al iniciar)
cd src/LaundryPOS.API
dotnet restore
dotnet run

# Frontend
cd src/LaundryPOS.Web
npm install
npm run dev
```

---

## APIs REST

### Autenticación
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/auth/login` | Login con JWT |
| POST | `/api/auth/refresh` | Renovar token |
| POST | `/api/auth/register` | Crear usuario (Admin) |

### Dashboard
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/dashboard/{branchId}` | Dashboard en tiempo real |

### Máquinas
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/machines/branch/{branchId}` | Listar máquinas |
| GET | `/api/machines/branch/{branchId}/available` | Máquinas disponibles |
| GET | `/api/machines/{id}` | Detalle de máquina |
| POST | `/api/machines` | Crear máquina |
| PUT | `/api/machines/{id}` | Actualizar máquina |
| PATCH | `/api/machines/{id}/status` | Cambiar estado |
| DELETE | `/api/machines/{id}` | Eliminar (soft) |

### Pagos
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| POST | `/api/payments/process` | Procesar pago + iniciar máquina |

### Transacciones
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/transactions/branch/{branchId}` | Historial |
| GET | `/api/transactions/{id}` | Detalle |

### Sucursales
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/branches` | Listar |
| GET | `/api/branches/{id}` | Detalle |
| POST | `/api/branches` | Crear |
| PUT | `/api/branches/{id}` | Actualizar |

### Mantenimiento
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/maintenance/branch/{branchId}` | Registros |
| GET | `/api/maintenance/branch/{branchId}/scheduled` | Programados |
| POST | `/api/maintenance` | Crear |
| PATCH | `/api/maintenance/{id}/complete` | Completar |

### Reportes
| Método | Endpoint | Descripción |
|--------|----------|-------------|
| GET | `/api/reports/revenue/daily/{branchId}` | Ingresos diarios |
| GET | `/api/reports/machines/usage/{branchId}` | Uso de máquinas |
| GET | `/api/reports/revenue/branches` | Ingresos por sucursal |
| GET | `/api/reports/export/pdf/{branchId}` | Exportar PDF |
| GET | `/api/reports/export/excel/{branchId}` | Exportar Excel |
| GET | `/api/reports/export/csv/{branchId}` | Exportar CSV |

### SignalR Hubs
| Hub | Endpoint | Eventos |
|-----|----------|---------|
| MachineHub | `/hubs/machines` | MachineStatusChanged |
| DashboardHub | `/hubs/dashboard` | DashboardUpdate, TransactionCompleted, AlertCreated |

---

## Licencia

Roberto Rodriguez Ortiz. Todos los derechos reservados.