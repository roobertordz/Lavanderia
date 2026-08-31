import api from './client';
import type {
  ApiResponse,
  AuthResponse,
  Branch,
  DashboardData,
  Machine,
  MaintenanceRecord,
  MachineUsageReport,
  RevenueReport,
  Transaction,
  Product,
  StockMovement,
  ProductImportSummary,
  LaundryOrder,
  LaundryOrderStatus,
} from '@/types';

// ─── System / Updates ───
export const systemApi = {
  getVersion: () => api.get<ApiResponse<{ version: string; environment: string }>>('/system/version'),
  checkUpdate: () =>
    api.get<ApiResponse<{ currentVersion: string; latestVersion: string; updateAvailable: boolean }>>(
      '/system/check-update'
    ),
  applyUpdate: (tag?: string) =>
    api.post<ApiResponse<unknown>>('/system/apply-update', null, { params: tag ? { tag } : undefined }),
  getUpdateStatus: () =>
    api.get<ApiResponse<{ running: boolean; log: string }>>('/system/update-status'),
};

// ─── Auth ───
export const authApi = {
  login: (username: string, password: string) =>
    api.post<ApiResponse<AuthResponse>>('/auth/login', { username, password }),
  refresh: (accessToken: string, refreshToken: string) =>
    api.post<ApiResponse<AuthResponse>>('/auth/refresh', { accessToken, refreshToken }),
  register: (data: Record<string, unknown>) =>
    api.post<ApiResponse<unknown>>('/auth/register', data),
};

// ─── Dashboard ───
export const dashboardApi = {
  get: (branchId: string) =>
    api.get<ApiResponse<DashboardData>>(`/dashboard/${branchId}`),
};

// ─── Machines ───
export const machinesApi = {
  getByBranch: (branchId: string) =>
    api.get<ApiResponse<Machine[]>>(`/machines/branch/${branchId}`),
  getAvailable: (branchId: string) =>
    api.get<ApiResponse<Machine[]>>(`/machines/branch/${branchId}/available`),
  getById: (id: string) =>
    api.get<ApiResponse<Machine>>(`/machines/${id}`),
  create: (data: Record<string, unknown>) =>
    api.post<ApiResponse<Machine>>('/machines', data),
  update: (id: string, data: Record<string, unknown>) =>
    api.put<ApiResponse<Machine>>(`/machines/${id}`, { id, ...data }),
  changeStatus: (id: string, status: number) =>
    api.patch(`/machines/${id}/status`, status),
  delete: (id: string) =>
    api.delete(`/machines/${id}`),
};

// ─── Payments ───
export const paymentsApi = {
  process: (data: {
    machineId: string;
    branchId: string;
    paymentMethod: number;
    paymentGateway: string;
    promotionId?: string;
  }) => api.post<ApiResponse<Transaction>>('/payments/process', data),
};

// ─── Transactions ───
export const transactionsApi = {
  getByBranch: (branchId: string, from: string, to: string) =>
    api.get<ApiResponse<Transaction[]>>(`/transactions/branch/${branchId}`, { params: { from, to } }),
  getById: (id: string) =>
    api.get<ApiResponse<Transaction>>(`/transactions/${id}`),
};

// ─── Branches ───
export const branchesApi = {
  getAll: () => api.get<ApiResponse<Branch[]>>('/branches'),
  getById: (id: string) => api.get<ApiResponse<Branch>>(`/branches/${id}`),
  create: (data: Record<string, unknown>) => api.post<ApiResponse<Branch>>('/branches', data),
  update: (id: string, data: Record<string, unknown>) =>
    api.put<ApiResponse<Branch>>(`/branches/${id}`, { id, ...data }),
};

// ─── Maintenance ───
export const maintenanceApi = {
  getByBranch: (branchId: string, from: string, to: string) =>
    api.get<ApiResponse<MaintenanceRecord[]>>(`/maintenance/branch/${branchId}`, { params: { from, to } }),
  getScheduled: (branchId: string) =>
    api.get<ApiResponse<MaintenanceRecord[]>>(`/maintenance/branch/${branchId}/scheduled`),
  getByMachine: (machineId: string) =>
    api.get<ApiResponse<MaintenanceRecord[]>>(`/maintenance/machine/${machineId}`),
  create: (data: Record<string, unknown>) =>
    api.post<ApiResponse<MaintenanceRecord>>('/maintenance', data),
  complete: (id: string, data: Record<string, unknown>) =>
    api.patch<ApiResponse<unknown>>(`/maintenance/${id}/complete`, { maintenanceId: id, ...data }),
};

// ─── Reports ───
export const reportsApi = {
  dailyRevenue: (branchId: string, from: string, to: string) =>
    api.get<ApiResponse<RevenueReport[]>>(`/reports/revenue/daily/${branchId}`, { params: { from, to } }),
  machineUsage: (branchId: string, from: string, to: string) =>
    api.get<ApiResponse<MachineUsageReport[]>>(`/reports/machines/usage/${branchId}`, { params: { from, to } }),
  branchRevenue: (from: string, to: string) =>
    api.get<ApiResponse<unknown>>('/reports/revenue/branches', { params: { from, to } }),
  exportPdf: (branchId: string, reportType: string, from: string, to: string) =>
    api.get(`/reports/export/pdf/${branchId}`, {
      params: { reportType, from, to },
      responseType: 'blob',
    }),
  exportExcel: (branchId: string, reportType: string, from: string, to: string) =>
    api.get(`/reports/export/excel/${branchId}`, {
      params: { reportType, from, to },
      responseType: 'blob',
    }),
  exportCsv: (branchId: string, reportType: string, from: string, to: string) =>
    api.get(`/reports/export/csv/${branchId}`, {
      params: { reportType, from, to },
      responseType: 'blob',
    }),
};

// ─── Products ───
export const productsApi = {
  getByBranch: (branchId: string) =>
    api.get<ApiResponse<Product[]>>(`/products/branch/${branchId}`),
  getLowStock: (branchId: string) =>
    api.get<ApiResponse<Product[]>>(`/products/branch/${branchId}/low-stock`),
  getById: (id: string) =>
    api.get<ApiResponse<Product>>(`/products/${id}`),
  getMovements: (id: string) =>
    api.get<ApiResponse<StockMovement[]>>(`/products/${id}/movements`),
  create: (data: Record<string, unknown>) =>
    api.post<ApiResponse<Product>>('/products', data),
  update: (id: string, data: Record<string, unknown>) =>
    api.put<ApiResponse<Product>>(`/products/${id}`, { id, ...data }),
  delete: (id: string) =>
    api.delete(`/products/${id}`),
  adjustStock: (id: string, quantity: number, reason: string) =>
    api.patch<ApiResponse<Product>>(`/products/${id}/stock`, { quantity, reason }),
  sell: (id: string, quantity: number) =>
    api.post<ApiResponse<Product>>(`/products/${id}/sell`, { quantity }),
  exportExcel: (branchId: string) =>
    api.get(`/products/branch/${branchId}/export`, { responseType: 'blob' }),
  importExcel: (branchId: string, file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return api.post<ApiResponse<ProductImportSummary>>(`/products/branch/${branchId}/import`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
};

// ─── Lavado por encargo (drop-off wash orders) ───
export const laundryOrdersApi = {
  getByBranch: (branchId: string, status?: LaundryOrderStatus) =>
    api.get<ApiResponse<LaundryOrder[]>>(`/laundryorders/branch/${branchId}`, {
      params: status !== undefined ? { status } : undefined,
    }),
  getById: (id: string) =>
    api.get<ApiResponse<LaundryOrder>>(`/laundryorders/${id}`),
  create: (data: Record<string, unknown>) =>
    api.post<ApiResponse<LaundryOrder>>('/laundryorders', data),
  updateStatus: (id: string, status: LaundryOrderStatus) =>
    api.patch<ApiResponse<LaundryOrder>>(`/laundryorders/${id}/status`, { status }),
  registerPayment: (id: string, paymentMethod: number) =>
    api.post<ApiResponse<LaundryOrder>>(`/laundryorders/${id}/payment`, { paymentMethod }),
  delete: (id: string) =>
    api.delete(`/laundryorders/${id}`),
};
