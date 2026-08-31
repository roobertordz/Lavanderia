// ─── Enums ───
export enum MachineStatus {
  Available = 0,
  Occupied = 1,
  InCycle = 2,
  Finished = 3,
  OutOfService = 4,
  Error = 5,
  Maintenance = 6,
}

export enum MachineType {
  Washer = 0,
  Dryer = 1,
}

export enum PaymentMethod {
  Cash = 0,
  CreditCard = 1,
  DebitCard = 2,
  DigitalWallet = 3,
  BankTransfer = 4,
}

export enum TransactionStatus {
  Created = 0,
  PaymentPending = 1,
  PaymentAuthorized = 2,
  MachineStarting = 3,
  InProgress = 4,
  Completed = 5,
  Failed = 6,
  Cancelled = 7,
  Refunded = 8,
}

export enum AlertSeverity {
  Info = 0,
  Warning = 1,
  Critical = 2,
  Emergency = 3,
}

export enum UserRole {
  Administrator = 0,
  Supervisor = 1,
  Employee = 2,
  Technician = 3,
  Cashier = 4,
}

// ─── Models ───
export interface Machine {
  id: string;
  number: number;
  name: string;
  type: MachineType;
  capacity: string;
  price: number;
  durationMinutes: number;
  status: MachineStatus;
  location: string;
  ipAddress?: string;
  model?: string;
  brand?: string;
  serialNumber?: string;
  communicationStatus: number;
  lastHeartbeat?: string;
  totalCycles: number;
  totalHoursWorked: number;
  branchId: string;
  branchName: string;
  ioTControllerId?: string;
}

export interface Transaction {
  id: string;
  transactionNumber: string;
  transactionDate: string;
  amount: number;
  taxAmount: number;
  totalAmount: number;
  discountAmount?: number;
  paymentMethod: PaymentMethod;
  paymentStatus: number;
  status: TransactionStatus;
  paymentGateway?: string;
  authorizationNumber?: string;
  durationMinutes: number;
  startTime?: string;
  endTime?: string;
  machineId: string;
  machineName: string;
  machineNumber: number;
  branchId: string;
  branchName: string;
}

export interface Branch {
  id: string;
  name: string;
  code: string;
  address: string;
  city: string;
  state: string;
  phone: string;
  currency: string;
  taxRate: number;
  totalMachines: number;
  availableMachines: number;
  isActive: boolean;
}

export interface DashboardData {
  todaySales: number;
  monthSales: number;
  totalRevenue: number;
  occupiedMachines: number;
  availableMachines: number;
  outOfServiceMachines: number;
  maintenanceMachines: number;
  totalMachines: number;
  todayTransactions: number;
  activeAlerts: number;
  machineStatuses: MachineStatusSummary[];
  recentTransactions: RecentTransaction[];
  recentAlerts: Alert[];
}

export interface MachineStatusSummary {
  machineId: string;
  number: number;
  name: string;
  type: MachineType;
  status: MachineStatus;
  communicationStatus: number;
  remainingMinutes?: number;
}

export interface RecentTransaction {
  transactionNumber: string;
  transactionDate: string;
  totalAmount: number;
  machineName: string;
  status: TransactionStatus;
}

export interface Alert {
  id: string;
  title: string;
  message: string;
  severity: AlertSeverity;
  createdAt: string;
  machineName: string;
  isResolved: boolean;
}

export interface User {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  role: UserRole;
  isActive: boolean;
  lastLoginAt?: string;
  branchIds: string[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  errorCode?: string;
}

export interface MaintenanceRecord {
  id: string;
  title: string;
  description: string;
  type: number;
  status: number;
  scheduledDate: string;
  completedDate?: string;
  cost?: number;
  partsReplaced?: string;
  machineId: string;
  machineName: string;
  technicianId?: string;
  technicianName?: string;
}

export interface RevenueReport {
  date: string;
  revenue: number;
  transactionCount: number;
}

export interface MachineUsageReport {
  machineId: string;
  machineName: string;
  totalUses: number;
  totalRevenue: number;
  averageUsageMinutes: number;
  errorCount: number;
}

// ─── Products ───
export enum ProductCategory {
  Detergent = 0,
  FabricSoftener = 1,
  Bleach = 2,
  StainRemover = 3,
  Bags = 4,
  Accessories = 5,
  Other = 6,
}

export enum StockMovementType {
  InitialStock = 0,
  Purchase = 1,
  Sale = 2,
  ManualAdjustment = 3,
  Import = 4,
  Return = 5,
}

export interface Product {
  id: string;
  name: string;
  brand: string;
  category: ProductCategory;
  presentation: string;
  sku?: string;
  barcode?: string;
  imageUrl?: string;
  purchasePrice: number;
  salePrice: number;
  stockQuantity: number;
  minStockThreshold: number;
  isLowStock: boolean;
  notes?: string;
  isActive: boolean;
  branchId: string;
  branchName: string;
}

export interface StockMovement {
  id: string;
  productId: string;
  productName: string;
  type: StockMovementType;
  quantity: number;
  previousStock: number;
  newStock: number;
  reason?: string;
  userName?: string;
  createdAt: string;
}

export interface ProductImportSummary {
  imported: number;
  failed: number;
  errors: string[];
}

