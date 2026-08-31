import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { useAuthStore } from '@/stores';
import { useEffect } from 'react';
import { UserRole } from '@/types';
import { DashboardPage } from '@/pages/DashboardPage';
import { KioskPage } from '@/pages/KioskPage';
import { LoginPage } from '@/pages/LoginPage';
import { MachinePOSPage } from '@/pages/MachinePOSPage';
import { MachinesPage } from '@/pages/MachinesPage';
import { ProductsPage } from '@/pages/ProductsPage';
import { LaundryOrdersPage } from '@/pages/LaundryOrdersPage';
import { TransactionsPage } from '@/pages/TransactionsPage';
import { MaintenancePage } from '@/pages/MaintenancePage';
import { ReportsPage } from '@/pages/ReportsPage';
import { UpdatesPage } from '@/pages/UpdatesPage';
import { Layout } from '@/components/Layout';

function ProtectedRoute({ children, roles }: { children: React.ReactNode; roles?: UserRole[] }) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const user = useAuthStore((s) => s.user);
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (roles && user && !roles.includes(user.role)) return <Navigate to="/dashboard" replace />;
  return <Layout>{children}</Layout>;
}

export default function App() {
  const loadFromStorage = useAuthStore((s) => s.loadFromStorage);

  useEffect(() => {
    loadFromStorage();
  }, [loadFromStorage]);

  return (
    <BrowserRouter>
      <Toaster position="top-right" />
      <Routes>
        {/* Auth */}
        <Route path="/login" element={<LoginPage />} />

        {/* Kiosk (public) */}
        <Route path="/kiosk" element={<KioskPage />} />

        {/* Admin routes (protected) */}
        <Route path="/dashboard"    element={<ProtectedRoute><DashboardPage /></ProtectedRoute>} />
        <Route path="/cobro"        element={<ProtectedRoute roles={[UserRole.Administrator, UserRole.Supervisor, UserRole.Employee, UserRole.Cashier]}><MachinePOSPage /></ProtectedRoute>} />
        <Route path="/machines"     element={<ProtectedRoute roles={[UserRole.Administrator, UserRole.Supervisor, UserRole.Employee, UserRole.Technician]}><MachinesPage /></ProtectedRoute>} />
        <Route path="/products"     element={<ProtectedRoute roles={[UserRole.Administrator, UserRole.Supervisor, UserRole.Employee, UserRole.Cashier]}><ProductsPage /></ProtectedRoute>} />
        <Route path="/lavado-encargo" element={<ProtectedRoute roles={[UserRole.Administrator, UserRole.Supervisor, UserRole.Employee, UserRole.Cashier]}><LaundryOrdersPage /></ProtectedRoute>} />
        <Route path="/transactions" element={<ProtectedRoute roles={[UserRole.Administrator, UserRole.Supervisor, UserRole.Employee]}><TransactionsPage /></ProtectedRoute>} />
        <Route path="/maintenance"  element={<ProtectedRoute roles={[UserRole.Administrator, UserRole.Supervisor, UserRole.Technician]}><MaintenancePage /></ProtectedRoute>} />
        <Route path="/reports"      element={<ProtectedRoute roles={[UserRole.Administrator, UserRole.Supervisor]}><ReportsPage /></ProtectedRoute>} />
        <Route path="/updates"      element={<ProtectedRoute roles={[UserRole.Administrator]}><UpdatesPage /></ProtectedRoute>} />

        {/* Default redirect */}
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

