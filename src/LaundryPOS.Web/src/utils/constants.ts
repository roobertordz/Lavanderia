import { MachineStatus, MachineType, TransactionStatus, AlertSeverity, ProductCategory, StockMovementType, LaundryOrderServiceType, LaundryOrderStatus } from '@/types';

export const machineStatusLabels: Record<MachineStatus, string> = {
  [MachineStatus.Available]: 'Disponible',
  [MachineStatus.Occupied]: 'Ocupada',
  [MachineStatus.InCycle]: 'En Lavado',
  [MachineStatus.Finished]: 'Terminó',
  [MachineStatus.OutOfService]: 'Fuera de Servicio',
  [MachineStatus.Error]: 'Error',
  [MachineStatus.Maintenance]: 'Mantenimiento',
};

export const machineStatusColors: Record<MachineStatus, string> = {
  [MachineStatus.Available]: 'bg-green-500',
  [MachineStatus.Occupied]: 'bg-blue-500',
  [MachineStatus.InCycle]: 'bg-indigo-500',
  [MachineStatus.Finished]: 'bg-cyan-500',
  [MachineStatus.OutOfService]: 'bg-red-500',
  [MachineStatus.Error]: 'bg-red-700',
  [MachineStatus.Maintenance]: 'bg-yellow-500',
};

export const machineTypeLabels: Record<MachineType, string> = {
  [MachineType.Washer]: 'Lavadora',
  [MachineType.Dryer]: 'Secadora',
};

export const transactionStatusLabels: Record<TransactionStatus, string> = {
  [TransactionStatus.Created]: 'Creada',
  [TransactionStatus.PaymentPending]: 'Pago Pendiente',
  [TransactionStatus.PaymentAuthorized]: 'Pago Autorizado',
  [TransactionStatus.MachineStarting]: 'Iniciando Máquina',
  [TransactionStatus.InProgress]: 'En Progreso',
  [TransactionStatus.Completed]: 'Completada',
  [TransactionStatus.Failed]: 'Fallida',
  [TransactionStatus.Cancelled]: 'Cancelada',
  [TransactionStatus.Refunded]: 'Reembolsada',
};

export const alertSeverityColors: Record<AlertSeverity, string> = {
  [AlertSeverity.Info]: 'bg-blue-100 text-blue-800',
  [AlertSeverity.Warning]: 'bg-yellow-100 text-yellow-800',
  [AlertSeverity.Critical]: 'bg-red-100 text-red-800',
  [AlertSeverity.Emergency]: 'bg-red-200 text-red-900',
};

export const productCategoryLabels: Record<ProductCategory, string> = {
  [ProductCategory.Detergent]: 'Detergente',
  [ProductCategory.FabricSoftener]: 'Suavizante',
  [ProductCategory.Bleach]: 'Blanqueador',
  [ProductCategory.StainRemover]: 'Quitamanchas',
  [ProductCategory.Bags]: 'Bolsas',
  [ProductCategory.Accessories]: 'Accesorios',
  [ProductCategory.Other]: 'Otros',
};

export const productCategoryIcons: Record<ProductCategory, string> = {
  [ProductCategory.Detergent]: '🧴',
  [ProductCategory.FabricSoftener]: '🧺',
  [ProductCategory.Bleach]: '🧪',
  [ProductCategory.StainRemover]: '✨',
  [ProductCategory.Bags]: '🛍️',
  [ProductCategory.Accessories]: '🧷',
  [ProductCategory.Other]: '📦',
};

export const stockMovementTypeLabels: Record<StockMovementType, string> = {
  [StockMovementType.InitialStock]: 'Stock inicial',
  [StockMovementType.Purchase]: 'Compra',
  [StockMovementType.Sale]: 'Venta',
  [StockMovementType.ManualAdjustment]: 'Ajuste manual',
  [StockMovementType.Import]: 'Importación',
  [StockMovementType.Return]: 'Devolución',
};

export function formatCurrency(amount: number, currency = 'MXN'): string {
  return new Intl.NumberFormat('es-MX', { style: 'currency', currency }).format(amount);
}

export function formatDate(date: string): string {
  return new Date(date).toLocaleDateString('es-MX', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export const laundryOrderServiceTypeLabels: Record<LaundryOrderServiceType, string> = {
  [LaundryOrderServiceType.ByWeight]: 'Por kilo',
  [LaundryOrderServiceType.Comforter]: 'Edredón',
};

export const laundryOrderStatusLabels: Record<LaundryOrderStatus, string> = {
  [LaundryOrderStatus.Received]: 'Recibido',
  [LaundryOrderStatus.InProgress]: 'En proceso',
  [LaundryOrderStatus.Ready]: 'Listo para entregar',
  [LaundryOrderStatus.Delivered]: 'Entregado',
  [LaundryOrderStatus.Cancelled]: 'Cancelado',
};

export const laundryOrderStatusColors: Record<LaundryOrderStatus, string> = {
  [LaundryOrderStatus.Received]: 'bg-blue-100 text-blue-800',
  [LaundryOrderStatus.InProgress]: 'bg-indigo-100 text-indigo-800',
  [LaundryOrderStatus.Ready]: 'bg-green-100 text-green-800',
  [LaundryOrderStatus.Delivered]: 'bg-gray-100 text-gray-800',
  [LaundryOrderStatus.Cancelled]: 'bg-red-100 text-red-800',
};
