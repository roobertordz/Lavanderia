import { useState, useEffect } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { useAuthStore, useBranchStore } from '@/stores';
import { branchesApi } from '@/api/endpoints';
import { UserRole, type Branch } from '@/types';
import toast from 'react-hot-toast';

// `roles: undefined` means the item is visible to any authenticated role.
const navItems: { to: string; label: string; icon: string; roles?: UserRole[] }[] = [
  { to: '/dashboard', label: 'Dashboard',    icon: '📊' },
  { to: '/cobro',     label: 'Cobrar / POS',  icon: '🧺', roles: [UserRole.Administrator, UserRole.Supervisor, UserRole.Employee, UserRole.Cashier] },
  { to: '/products',  label: 'Productos',     icon: '🧴', roles: [UserRole.Administrator, UserRole.Supervisor, UserRole.Employee, UserRole.Cashier] },
  { to: '/lavado-encargo', label: 'Lavado por Encargo', icon: '🛏️', roles: [UserRole.Administrator, UserRole.Supervisor, UserRole.Employee, UserRole.Cashier] },
  { to: '/machines',  label: 'Máquinas',      icon: '🔧', roles: [UserRole.Administrator, UserRole.Supervisor, UserRole.Employee, UserRole.Technician] },
  { to: '/transactions', label: 'Transacciones', icon: '💳', roles: [UserRole.Administrator, UserRole.Supervisor, UserRole.Employee] },
  { to: '/maintenance',  label: 'Mantenimiento',  icon: '🛠️', roles: [UserRole.Administrator, UserRole.Supervisor, UserRole.Technician] },
  { to: '/reports',      label: 'Reportes',       icon: '📈', roles: [UserRole.Administrator, UserRole.Supervisor] },
  { to: '/updates',      label: 'Actualizaciones', icon: '⬆️', roles: [UserRole.Administrator] },
];

export function Layout({ children }: { children: React.ReactNode }) {
  const [branches, setBranches] = useState<Branch[]>([]);
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const { user, logout } = useAuthStore();
  const { selectedBranchId, selectBranch } = useBranchStore();
  const navigate = useNavigate();
  const visibleNavItems = navItems.filter((item) => !item.roles || (user && item.roles.includes(user.role)));

  useEffect(() => {
    branchesApi.getAll().then((res) => {
      if (res.data.success && res.data.data) {
        setBranches(res.data.data);
      }
    }).catch(() => {});
  }, []);

  const handleLogout = () => {
    logout();
    toast.success('Sesión cerrada');
    navigate('/login');
  };

  return (
    <div className="flex h-screen bg-gray-100 overflow-hidden">
      {/* Sidebar */}
      <aside
        className={`${sidebarOpen ? 'w-56' : 'w-14'} bg-indigo-900 text-white flex flex-col transition-all duration-200 shrink-0`}
      >
        {/* Logo */}
        <div className="flex items-center gap-2 px-4 py-4 border-b border-indigo-700">
          <span className="text-xl">🧺</span>
          {sidebarOpen && <span className="font-bold text-sm tracking-wide">LaundryPOS</span>}
        </div>

        {/* Nav */}
        <nav className="flex-1 py-4 space-y-1 px-2">
          {visibleNavItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors ${
                  isActive
                    ? 'bg-indigo-600 text-white'
                    : 'text-indigo-200 hover:bg-indigo-800 hover:text-white'
                }`
              }
            >
              <span className="text-base shrink-0">{item.icon}</span>
              {sidebarOpen && <span>{item.label}</span>}
            </NavLink>
          ))}
        </nav>

        {/* Toggle */}
        <button
          onClick={() => setSidebarOpen(!sidebarOpen)}
          className="px-4 py-3 text-indigo-300 hover:text-white border-t border-indigo-700 text-left text-xs"
        >
          {sidebarOpen ? '◀ Colapsar' : '▶'}
        </button>
      </aside>

      {/* Main content */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Top bar */}
        <header className="bg-white shadow-sm px-6 py-3 flex items-center justify-between shrink-0">
          {/* Branch selector */}
          <div className="flex items-center gap-3">
            <label className="text-xs text-gray-500 font-medium">Sucursal:</label>
            <select
              value={selectedBranchId ?? ''}
              onChange={(e) => selectBranch(e.target.value)}
              className="border border-gray-300 rounded-md px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
            >
              <option value="" disabled>Selecciona sucursal</option>
              {branches.map((b) => (
                <option key={b.id} value={b.id}>{b.name}</option>
              ))}
            </select>
          </div>

          {/* User + logout */}
          <div className="flex items-center gap-4">
            <div className="text-right hidden sm:block">
              <p className="text-sm font-medium text-gray-800">{user?.firstName} {user?.lastName}</p>
              <p className="text-xs text-gray-500">{user?.username}</p>
            </div>
            <button
              onClick={handleLogout}
              className="bg-red-50 hover:bg-red-100 text-red-600 text-sm font-medium px-3 py-1.5 rounded-lg transition-colors"
            >
              Cerrar sesión
            </button>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-y-auto p-6">
          {children}
        </main>
      </div>
    </div>
  );
}
