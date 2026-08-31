# Casos de Uso - LaundryPOS

---

## CU-01: Procesar Pago e Iniciar Máquina (Flujo Principal)

**Actor**: Cliente (sin registro)  
**Precondiciones**: Existe al menos una máquina disponible en la sucursal  
**Postcondiciones**: La máquina inicia el ciclo, la transacción queda registrada  

### Flujo Principal
1. El cliente se acerca al kiosco/pantalla táctil
2. El sistema muestra las máquinas disponibles (lavadoras/secadoras)
3. El cliente selecciona una máquina
4. El sistema muestra el costo, tipo de máquina y duración
5. El cliente confirma la selección
6. El sistema muestra las formas de pago disponibles
7. El cliente selecciona forma de pago
8. El sistema procesa el pago con la pasarela correspondiente
9. La pasarela autoriza el pago
10. El sistema registra la transacción
11. El sistema envía comando `StartMachine()` al controlador IoT
12. El controlador confirma el inicio
13. El sistema actualiza el estado de la máquina a "En Lavado"
14. El sistema notifica en tiempo real al dashboard
15. El kiosco muestra confirmación al cliente

### Flujos Alternativos

**FA-01**: Máquina no disponible
- En paso 3, si la máquina ya no está disponible, el sistema muestra mensaje y regresa al paso 2

**FA-02**: Pago rechazado
- En paso 9, si el pago es rechazado, el sistema muestra error y regresa al paso 6

**FA-03**: Fallo al iniciar máquina
- En paso 12, si el controlador no responde, el sistema inicia reembolso automático y muestra error al cliente

---

## CU-02: Visualizar Dashboard en Tiempo Real

**Actor**: Administrador, Supervisor  
**Precondiciones**: Usuario autenticado con permisos de dashboard  

### Flujo Principal
1. El usuario accede al dashboard
2. El sistema muestra KPIs: ventas del día, ventas del mes, máquinas por estado
3. El sistema muestra mapa visual de máquinas con colores según estado
4. El sistema establece conexión WebSocket
5. Cuando cambia el estado de una máquina, se actualiza en tiempo real
6. Cuando se completa una transacción, se actualizan los totales
7. Las alertas aparecen en tiempo real

---

## CU-03: Administrar Máquinas (CRUD)

**Actor**: Administrador  
**Precondiciones**: Usuario autenticado con rol Administrador  

### Flujo: Alta de Máquina
1. El admin selecciona "Nueva Máquina"
2. Ingresa: número, nombre, tipo, capacidad, precio, duración, ubicación
3. Asigna controlador IoT
4. El sistema valida que el número no exista en la sucursal
5. El sistema crea la máquina con estado "Disponible"

### Flujo: Modificación
1. El admin selecciona una máquina existente
2. Modifica campos permitidos (precio, duración, nombre, controlador)
3. El sistema valida y actualiza

### Flujo: Baja (Soft Delete)
1. El admin selecciona "Eliminar" en una máquina
2. El sistema confirma la acción
3. El sistema marca la máquina como eliminada y fuera de servicio

---

## CU-04: Gestionar Mantenimiento

**Actor**: Técnico, Supervisor  

### Flujo: Programar Mantenimiento Preventivo
1. El técnico crea un nuevo registro de mantenimiento
2. Selecciona máquina, tipo (preventivo/correctivo), fecha
3. La máquina se marca como "En Mantenimiento"
4. Se registran horas trabajadas y ciclos al momento del servicio

### Flujo: Completar Mantenimiento
1. El técnico marca el mantenimiento como completado
2. Registra: costo, piezas cambiadas, notas técnicas
3. La máquina regresa a estado "Disponible"

### Flujo: Alerta Automática
1. El sistema detecta que una máquina superó los ciclos/horas de umbral
2. Se crea alerta automática de mantenimiento
3. Se notifica en tiempo real al dashboard

---

## CU-05: Generar Reportes

**Actor**: Administrador, Supervisor  

### Flujo Principal
1. El usuario selecciona tipo de reporte
2. Define rango de fechas y sucursal
3. El sistema genera el reporte con datos agregados
4. El usuario puede exportar a PDF, Excel o CSV

### Tipos de Reporte
- Ingresos diarios / mensuales
- Uso de máquinas (más/menos utilizadas)
- Máquinas con más fallas
- Ingresos por sucursal
- Ingresos por máquina
- Tiempo promedio de uso
- Consumo estimado

---

## CU-06: Autenticación y Autorización

**Actor**: Todos los usuarios del sistema  

### Flujo: Login
1. El usuario ingresa credenciales (username/password)
2. El sistema verifica credenciales con BCrypt
3. Si son válidas, genera JWT + Refresh Token
4. Retorna tokens y perfil del usuario

### Flujo: Refresh Token
1. El access token expira
2. El frontend envía el refresh token
3. El sistema genera nuevos tokens
4. Retorna nuevos tokens

### Flujo: Lockout
1. El usuario falla 5 veces el login
2. La cuenta se bloquea por 15 minutos
3. Después del lockout, puede intentar nuevamente

---

## CU-07: Administrar Sucursales

**Actor**: Administrador  

### Flujo Principal
1. El admin crea una nueva sucursal
2. Configura: nombre, código, dirección, impuesto, moneda, horarios
3. Asigna empleados a la sucursal
4. Agrega máquinas a la sucursal
5. La sucursal aparece en el selector de sucursal del dashboard

---

## CU-08: Monitoreo IoT

**Actor**: Sistema (automático)  

### Flujo: Heartbeat
1. El sistema envía heartbeat cada 30 segundos a cada controlador
2. Si no responde, marca como "Offline"
3. Si estaba offline y responde, marca como "Online"
4. Se generan alertas para cambios de estado

### Flujo: Fin de Ciclo
1. El controlador IoT notifica fin de ciclo
2. El sistema actualiza la máquina a "Terminó"
3. Después del período de gracia, cambia a "Disponible"
4. Se notifica al dashboard en tiempo real
