# Historias de Usuario - LaundryPOS

---

## Epic: Proceso de Pago (Core)

### HU-001: Seleccionar máquina desde kiosco
**Como** cliente de la lavandería  
**Quiero** ver las máquinas disponibles en una pantalla  
**Para** elegir la que necesito sin preguntar a nadie  

**Criterios de Aceptación:**
- [ ] Se muestran solo máquinas con estado "Disponible"
- [ ] Cada máquina muestra: número, tipo, capacidad, precio, duración
- [ ] Se diferencia visualmente entre lavadoras y secadoras
- [ ] Las máquinas ocupadas/en mantenimiento NO aparecen

### HU-002: Pagar servicio de lavado
**Como** cliente de la lavandería  
**Quiero** pagar directamente desde el kiosco  
**Para** que la máquina comience automáticamente sin tarjetas ni fichas  

**Criterios de Aceptación:**
- [ ] Se muestra el costo total incluyendo impuestos
- [ ] Se ofrecen múltiples formas de pago
- [ ] El pago se procesa en menos de 10 segundos
- [ ] Si el pago falla, se muestra error claro y se puede reintentar
- [ ] Si la máquina no inicia, se hace reembolso automático

### HU-003: Confirmación de inicio
**Como** cliente de la lavandería  
**Quiero** recibir confirmación visual de que la máquina inició  
**Para** saber que el proceso comenzó correctamente  

**Criterios de Aceptación:**
- [ ] Se muestra pantalla de éxito con número de máquina y duración
- [ ] Se puede iniciar una nueva transacción desde la misma pantalla
- [ ] La máquina física comienza a funcionar dentro de 5 segundos

---

## Epic: Dashboard Administrativo

### HU-004: Ver ventas en tiempo real
**Como** administrador de la lavandería  
**Quiero** ver las ventas del día y del mes en el dashboard  
**Para** monitorear el rendimiento del negocio  

**Criterios de Aceptación:**
- [ ] Se muestra total de ventas del día actual
- [ ] Se muestra total de ventas del mes actual
- [ ] Los totales se actualizan en tiempo real sin recargar la página
- [ ] Se muestra el número de transacciones del día

### HU-005: Ver estado de máquinas en mapa visual
**Como** supervisor  
**Quiero** ver todas las máquinas en un mapa con colores  
**Para** identificar rápidamente cuáles están disponibles, ocupadas o con problema  

**Criterios de Aceptación:**
- [ ] Cada máquina se muestra como tarjeta con color según estado
- [ ] Verde = disponible, Azul = en ciclo, Rojo = error/fuera de servicio, Amarillo = mantenimiento
- [ ] Se actualiza en tiempo real via WebSocket
- [ ] Se muestra indicador de comunicación (online/offline)
- [ ] Para máquinas en ciclo, se muestra tiempo restante

### HU-006: Recibir alertas en tiempo real
**Como** administrador  
**Quiero** recibir alertas cuando una máquina tenga problemas  
**Para** actuar rápidamente y minimizar el tiempo fuera de servicio  

**Criterios de Aceptación:**
- [ ] Las alertas aparecen en el dashboard sin recargar
- [ ] Se clasifican por severidad: Info, Warning, Critical, Emergency
- [ ] Se puede marcar una alerta como resuelta
- [ ] Se muestra historial de alertas

---

## Epic: Administración de Máquinas

### HU-007: Alta de máquina
**Como** administrador  
**Quiero** registrar una nueva máquina en el sistema  
**Para** que esté disponible para los clientes  

**Criterios de Aceptación:**
- [ ] Se pueden registrar lavadoras y secadoras
- [ ] Campos requeridos: número, nombre, tipo, precio, duración, sucursal
- [ ] No se permiten números duplicados en la misma sucursal
- [ ] Se puede asignar un controlador IoT

### HU-008: Modificar precio y duración
**Como** administrador  
**Quiero** cambiar el precio o duración de una máquina  
**Para** ajustar según la demanda o costos operativos  

**Criterios de Aceptación:**
- [ ] Los cambios aplican a partir de la siguiente transacción
- [ ] Se registra el cambio en audit log

---

## Epic: Reportes

### HU-009: Generar reporte de ingresos
**Como** administrador  
**Quiero** generar reportes de ingresos por período  
**Para** analizar el rendimiento financiero del negocio  

**Criterios de Aceptación:**
- [ ] Se puede seleccionar rango de fechas
- [ ] Se muestra desglose diario o mensual
- [ ] Se puede filtrar por sucursal
- [ ] Se puede exportar a PDF, Excel y CSV

### HU-010: Ver máquinas más utilizadas
**Como** supervisor  
**Quiero** ver qué máquinas se usan más  
**Para** planificar mantenimiento y distribución de carga  

**Criterios de Aceptación:**
- [ ] Se muestra ranking de máquinas por número de usos
- [ ] Se incluye ingreso generado por máquina
- [ ] Se muestra tiempo promedio de uso
- [ ] Se incluye número de errores/fallas

---

## Epic: Mantenimiento

### HU-011: Programar mantenimiento preventivo
**Como** supervisor  
**Quiero** programar mantenimientos preventivos  
**Para** prevenir fallas y extender la vida útil de las máquinas  

**Criterios de Aceptación:**
- [ ] Se puede crear registro con fecha programada
- [ ] Se puede asignar técnico
- [ ] La máquina se marca como "En Mantenimiento"
- [ ] Se registran horas y ciclos al momento del servicio

### HU-012: Alerta automática de mantenimiento
**Como** sistema  
**Quiero** detectar cuando una máquina necesita mantenimiento  
**Para** notificar al personal antes de que falle  

**Criterios de Aceptación:**
- [ ] Alerta cuando supera X ciclos (configurable)
- [ ] Alerta cuando supera X horas de trabajo (configurable)
- [ ] Se genera alerta en el dashboard

---

## Epic: Multi-Sucursal

### HU-013: Administrar múltiples sucursales
**Como** dueño/administrador general  
**Quiero** gestionar varias lavanderías desde un solo sistema  
**Para** centralizar la operación del negocio  

**Criterios de Aceptación:**
- [ ] Se pueden crear, editar y desactivar sucursales
- [ ] Cada sucursal tiene su propia configuración (impuesto, moneda, horarios)
- [ ] Los usuarios se asignan a una o más sucursales
- [ ] Los reportes se pueden filtrar o agregar por sucursal

---

## Epic: Seguridad

### HU-014: Login seguro
**Como** usuario del sistema  
**Quiero** acceder con usuario y contraseña seguros  
**Para** proteger la información del negocio  

**Criterios de Aceptación:**
- [ ] Password con mínimo 8 caracteres, mayúscula, minúscula, número, carácter especial
- [ ] Bloqueo tras 5 intentos fallidos
- [ ] Sesiones con JWT de 1 hora + refresh token de 7 días
- [ ] No se almacenan contraseñas en texto plano

### HU-015: Permisos por rol
**Como** administrador  
**Quiero** que cada rol tenga acceso solo a lo que le corresponde  
**Para** controlar quién puede hacer qué en el sistema  

**Criterios de Aceptación:**
- [ ] Admin: acceso total
- [ ] Supervisor: dashboard, reportes, máquinas, mantenimiento
- [ ] Empleado: dashboard, cambiar estado de máquinas
- [ ] Técnico: mantenimiento, estado de máquinas
