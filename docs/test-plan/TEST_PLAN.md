# Plan de Pruebas - LaundryPOS

---

## 1. Estrategia de Testing

| Nivel | Framework | Cobertura Objetivo |
|-------|-----------|-------------------|
| Unit Tests | xUnit + Moq | 80%+ en Domain y Application |
| Integration Tests | xUnit + TestContainers | API Endpoints + DB |
| E2E Tests | Playwright | Flujos críticos de negocio |
| Load Tests | k6 / JMeter | Concurrencia y performance |

---

## 2. Casos de Prueba por Módulo

### CP-001: Proceso de Pago (Crítico)

| ID | Caso | Entrada | Resultado Esperado |
|----|------|---------|-------------------|
| CP-001-01 | Pago exitoso con tarjeta | Máquina disponible, pago válido | Transacción completada, máquina en ciclo |
| CP-001-02 | Máquina no disponible | Máquina ocupada | Error MACHINE_NOT_AVAILABLE |
| CP-001-03 | Pago rechazado | Tarjeta inválida | Error PAYMENT_FAILED, máquina sigue disponible |
| CP-001-04 | IoT falla al iniciar | Controlador offline | Reembolso automático, error MACHINE_START_FAILED |
| CP-001-05 | Máquina sin controlador | Máquina sin IoT asignado | Error NO_CONTROLLER |
| CP-001-06 | Pago con descuento | Promoción activa | Monto con descuento aplicado |
| CP-001-07 | Pago en efectivo | PaymentMethod=Cash | Transacción completada sin gateway externo |
| CP-001-08 | Concurrencia: 2 pagos misma máquina | 2 requests simultáneos | Solo 1 exitoso, otro rechazado |

### CP-002: Autenticación

| ID | Caso | Entrada | Resultado Esperado |
|----|------|---------|-------------------|
| CP-002-01 | Login exitoso | Credenciales válidas | JWT + Refresh Token |
| CP-002-02 | Login fallido | Password incorrecto | Error INVALID_CREDENTIALS |
| CP-002-03 | Lockout tras 5 intentos | 5 passwords incorrectos | Error ACCOUNT_LOCKED |
| CP-002-04 | Refresh token válido | Refresh token no expirado | Nuevo JWT |
| CP-002-05 | Refresh token expirado | Token de más de 7 días | Error INVALID_REFRESH_TOKEN |
| CP-002-06 | Acceso sin token | Request sin Authorization | HTTP 401 |
| CP-002-07 | Acceso con rol insuficiente | Employee intenta crear máquina | HTTP 403 |

### CP-003: CRUD Máquinas

| ID | Caso | Entrada | Resultado Esperado |
|----|------|---------|-------------------|
| CP-003-01 | Crear máquina válida | Datos completos | Máquina creada con status Available |
| CP-003-02 | Número duplicado | Mismo número en misma sucursal | Error DUPLICATE_NUMBER |
| CP-003-03 | Actualizar precio | Nuevo precio válido | Máquina actualizada |
| CP-003-04 | Soft delete | Eliminar máquina | IsDeleted=true, Status=OutOfService |
| CP-003-05 | Listar por sucursal | BranchId válido | Lista de máquinas activas |
| CP-003-06 | Máquinas disponibles | BranchId válido | Solo máquinas con Status=Available |

### CP-004: Dashboard

| ID | Caso | Entrada | Resultado Esperado |
|----|------|---------|-------------------|
| CP-004-01 | Dashboard con datos | Sucursal con transacciones | KPIs calculados correctamente |
| CP-004-02 | Dashboard vacío | Sucursal nueva sin datos | Valores en 0 |
| CP-004-03 | Actualización real-time | Nueva transacción | Dashboard se actualiza via SignalR |

### CP-005: Mantenimiento

| ID | Caso | Entrada | Resultado Esperado |
|----|------|---------|-------------------|
| CP-005-01 | Crear mantenimiento | Datos válidos | Registro creado, máquina en Maintenance |
| CP-005-02 | Completar mantenimiento | Mantenimiento activo | Estado Completed, máquina Available |
| CP-005-03 | Historial por máquina | MachineId válido | Lista ordenada por fecha |

### CP-006: Reportes

| ID | Caso | Entrada | Resultado Esperado |
|----|------|---------|-------------------|
| CP-006-01 | Reporte ingresos diarios | Rango de fechas | Datos agrupados por día |
| CP-006-02 | Reporte uso máquinas | Sucursal + rango | Ranking de máquinas |
| CP-006-03 | Exportar PDF | Tipo de reporte | Archivo PDF descargable |
| CP-006-04 | Exportar Excel | Tipo de reporte | Archivo XLSX descargable |
| CP-006-05 | Exportar CSV | Tipo de reporte | Archivo CSV descargable |

### CP-007: Sucursales

| ID | Caso | Entrada | Resultado Esperado |
|----|------|---------|-------------------|
| CP-007-01 | Crear sucursal | Datos válidos | Sucursal creada |
| CP-007-02 | Código duplicado | Código existente | Error DUPLICATE_CODE |
| CP-007-03 | Listar sucursales | Usuario autenticado | Lista de sucursales activas |

### CP-008: IoT

| ID | Caso | Entrada | Resultado Esperado |
|----|------|---------|-------------------|
| CP-008-01 | Heartbeat exitoso | Controlador online | IsAlive=true |
| CP-008-02 | Heartbeat fallido | Controlador offline | Timeout, status Offline |
| CP-008-03 | Start machine | Controlador online | Command Success |
| CP-008-04 | Stop machine | Máquina en ciclo | Command Success |

---

## 3. Pruebas de Seguridad

| ID | Caso | Descripción |
|----|------|-------------|
| SEC-01 | SQL Injection | Verificar que EF Core parametriza todas las queries |
| SEC-02 | XSS | Verificar sanitización de inputs en frontend |
| SEC-03 | CSRF | Verificar tokens anti-forgery en mutations |
| SEC-04 | JWT Tampering | Verificar que tokens modificados son rechazados |
| SEC-05 | Rate Limiting | Verificar protección contra fuerza bruta en login |
| SEC-06 | Sensitive Data | Verificar que passwords nunca se retornan en responses |
| SEC-07 | CORS | Verificar que solo orígenes permitidos acceden la API |

---

## 4. Pruebas de Performance

| Escenario | Usuarios Concurrentes | Tiempo Respuesta Esperado |
|-----------|-----------------------|--------------------------|
| Dashboard load | 100 | < 500ms |
| Payment processing | 50 | < 3s |
| Machine listing | 200 | < 200ms |
| Report generation | 20 | < 5s |
| WebSocket connections | 500 | Stable |

---

## 5. Estructura de Tests

```
tests/
├── LaundryPOS.Domain.Tests/
│   ├── Entities/
│   └── Validators/
├── LaundryPOS.Application.Tests/
│   ├── Machines/
│   ├── Payments/
│   ├── Dashboard/
│   └── Users/
├── LaundryPOS.Infrastructure.Tests/
│   ├── Repositories/
│   └── Services/
└── LaundryPOS.API.Tests/
    ├── Controllers/
    └── Integration/
```
