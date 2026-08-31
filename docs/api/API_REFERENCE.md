# API Reference - LaundryPOS

Base URL: `https://api.laundrypos.com/api` (production) | `http://localhost:5000/api` (dev)

## Authentication

All endpoints except kiosk and login require JWT Bearer token.

```
Authorization: Bearer <access_token>
```

---

## POST /auth/login

Authenticate and receive JWT tokens.

**Request:**
```json
{
  "username": "admin",
  "password": "Admin@123456"
}
```

**Response 200:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "abc123def456...",
    "expiresAt": "2026-07-28T14:00:00Z",
    "user": {
      "id": "b0000001-0000-0000-0000-000000000001",
      "username": "admin",
      "email": "admin@laundrypos.com",
      "firstName": "Administrador",
      "lastName": "Sistema",
      "role": 0,
      "isActive": true,
      "branchIds": ["a0000001-0000-0000-0000-000000000001"]
    }
  }
}
```

---

## POST /payments/process

**Core endpoint** - Process payment and start machine. Used by kiosk.

**Request:**
```json
{
  "machineId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "branchId": "a0000001-0000-0000-0000-000000000001",
  "paymentMethod": 1,
  "paymentGateway": "Stripe",
  "promotionId": null
}
```

**Payment Methods:** 0=Cash, 1=CreditCard, 2=DebitCard, 3=DigitalWallet, 4=BankTransfer

**Response 200 (Success):**
```json
{
  "success": true,
  "data": {
    "id": "7fa85f64-5717-4562-b3fc-2c963f66afa6",
    "transactionNumber": "TX-20260728-00001",
    "transactionDate": "2026-07-28T13:00:00Z",
    "amount": 85.00,
    "taxAmount": 13.60,
    "totalAmount": 98.60,
    "paymentMethod": 1,
    "paymentStatus": 3,
    "status": 4,
    "paymentGateway": "Stripe",
    "authorizationNumber": "STRIPE-ABC123DEF456",
    "durationMinutes": 50,
    "startTime": "2026-07-28T13:00:05Z",
    "endTime": "2026-07-28T13:50:05Z",
    "machineId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "machineName": "Lavadora Grande",
    "machineNumber": 2,
    "branchId": "a0000001-0000-0000-0000-000000000001",
    "branchName": "Sucursal Centro"
  }
}
```

**Response 400 (Machine not available):**
```json
{
  "success": false,
  "error": "Machine is not available.",
  "errorCode": "MACHINE_NOT_AVAILABLE"
}
```

---

## GET /dashboard/{branchId}

Real-time dashboard data.

**Response 200:**
```json
{
  "success": true,
  "data": {
    "todaySales": 4520.50,
    "monthSales": 68200.00,
    "totalRevenue": 68200.00,
    "occupiedMachines": 3,
    "availableMachines": 5,
    "outOfServiceMachines": 1,
    "maintenanceMachines": 1,
    "totalMachines": 10,
    "todayTransactions": 52,
    "activeAlerts": 2,
    "machineStatuses": [
      {
        "machineId": "...",
        "number": 1,
        "name": "Lavadora Pequeña",
        "type": 0,
        "status": 0,
        "communicationStatus": 0,
        "remainingMinutes": null
      }
    ],
    "recentTransactions": [],
    "recentAlerts": []
  }
}
```

---

## GET /machines/branch/{branchId}/available

Get available machines for kiosk display. **No authentication required.**

**Response 200:**
```json
{
  "success": true,
  "data": [
    {
      "id": "3fa85f64-...",
      "number": 1,
      "name": "Lavadora Pequeña",
      "type": 0,
      "capacity": "15 kg",
      "price": 65.00,
      "durationMinutes": 45,
      "status": 0,
      "location": "Pasillo A",
      "branchId": "a0000001-...",
      "branchName": "Sucursal Centro",
      "communicationStatus": 0
    }
  ]
}
```

---

## Error Response Format

All errors follow this format:

```json
{
  "success": false,
  "error": "Human-readable error message",
  "errorCode": "MACHINE_CODE",
  "details": ["Optional array of validation errors"]
}
```

### Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `VALIDATION_ERROR` | 400 | Input validation failed |
| `NOT_FOUND` | 404 | Entity not found |
| `DUPLICATE_NUMBER` | 400 | Machine number already exists |
| `DUPLICATE_CODE` | 400 | Branch code already exists |
| `DUPLICATE_USERNAME` | 400 | Username already exists |
| `MACHINE_NOT_AVAILABLE` | 400 | Machine is occupied/offline |
| `NO_CONTROLLER` | 400 | Machine has no IoT controller |
| `PAYMENT_FAILED` | 400 | Payment gateway rejected |
| `MACHINE_START_FAILED` | 400 | IoT controller failed to start |
| `INVALID_CREDENTIALS` | 400 | Wrong username/password |
| `ACCOUNT_LOCKED` | 400 | Too many failed attempts |
| `INVALID_TOKEN` | 401 | JWT token invalid |
| `INVALID_REFRESH_TOKEN` | 401 | Refresh token expired |
| `UNAUTHORIZED` | 401 | Not authenticated |
| `INTERNAL_ERROR` | 500 | Unexpected server error |

---

## SignalR Hubs

### /hubs/machines
| Event | Payload | Description |
|-------|---------|-------------|
| `MachineStatusChanged` | `{ machineId, status, timestamp }` | Machine changed state |

### /hubs/dashboard
| Event | Payload | Description |
|-------|---------|-------------|
| `DashboardUpdate` | `{ timestamp }` | Refresh dashboard data |
| `TransactionCompleted` | `{ transactionId, timestamp }` | New transaction |
| `AlertCreated` | `{ alertId, severity, message, timestamp }` | New alert |
