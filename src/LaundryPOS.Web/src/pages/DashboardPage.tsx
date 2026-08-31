import { useEffect, useState, useCallback } from 'react';
import { dashboardApi } from '@/api/endpoints';
import { useBranchStore } from '@/stores';
import { useSignalR } from '@/hooks/useSignalR';
import { MachineMap } from '@/components/machines/MachineMap';
import { formatCurrency, formatDate, alertSeverityColors, transactionStatusLabels } from '@/utils/constants';
import type { DashboardData } from '@/types';
import { TransactionStatus } from '@/types';
import {
  PieChart, Pie, Cell, Tooltip,
  BarChart, Bar, XAxis, YAxis, CartesianGrid, ResponsiveContainer,
} from 'recharts';
import { useNavigate } from 'react-router-dom';

export function DashboardPage() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const selectedBranchId = useBranchStore((s) => s.selectedBranchId);
  const navigate = useNavigate();

  const loadDashboard = useCallback(async () => {
    if (!selectedBranchId) {
      setLoading(false);
      return;
    }
    try {
      const response = await dashboardApi.get(selectedBranchId);
      if (response.data.success && response.data.data) {
        setData(response.data.data);
      }
    } catch {
      // silent
    } finally {
      setLoading(false);
    }
  }, [selectedBranchId]);

  useEffect(() => {
    loadDashboard();
  }, [loadDashboard]);

  // Real-time updates via SignalR
  useSignalR('/hubs/dashboard', {
    DashboardUpdate: () => loadDashboard(),
    TransactionCompleted: () => loadDashboard(),
    AlertCreated: () => loadDashboard(),
  });

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-indigo-600" />
      </div>
    );
  }

  if (!data) {
    return (
      <div className="text-center py-12 text-gray-500">
        Selecciona una sucursal para ver el dashboard.
      </div>
    );
  }

  // Donut chart — machine status distribution
  const statusChartData = [
    { name: 'Disponibles',    value: data.availableMachines,    color: '#22c55e' },
    { name: 'Ocupadas',       value: data.occupiedMachines,     color: '#3b82f6' },
    { name: 'Mantenimiento',  value: data.maintenanceMachines,  color: '#eab308' },
    { name: 'Fuera servicio', value: data.outOfServiceMachines, color: '#ef4444' },
  ].filter((d) => d.value > 0);

  // Bar chart — revenue from recent transactions (grouped by day)
  const revenueByDay: Record<string, number> = {};
  data.recentTransactions.forEach((t) => {
    const day = new Date(t.transactionDate).toLocaleDateString('es-MX', {
      weekday: 'short',
      day: 'numeric',
    });
    revenueByDay[day] = (revenueByDay[day] ?? 0) + t.totalAmount;
  });
  const revenueChartData = Object.entries(revenueByDay).map(([day, total]) => ({ day, total }));

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <button
          onClick={() => navigate('/cobro')}
          className="bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold px-4 py-2 rounded-lg transition"
        >
          + Cobrar máquina
        </button>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <KPICard
          title="Ventas del día"
          value={formatCurrency(data.todaySales)}
          sub={`${data.todayTransactions} transacciones`}
          icon="💰"
          borderColor="border-green-500"
        />
        <KPICard
          title="Ventas del mes"
          value={formatCurrency(data.monthSales)}
          sub="mes actual"
          icon="📅"
          borderColor="border-blue-500"
        />
        <KPICard
          title="Máquinas activas"
          value={`${data.occupiedMachines} / ${data.totalMachines}`}
          sub={`${data.availableMachines} disponibles`}
          icon="🔵"
          borderColor="border-indigo-500"
        />
        <KPICard
          title="Alertas activas"
          value={String(data.activeAlerts)}
          sub="sin resolver"
          icon={data.activeAlerts > 0 ? '🔴' : '✅'}
          borderColor={data.activeAlerts > 0 ? 'border-red-500' : 'border-gray-300'}
        />
      </div>

      {/* Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Donut — machine distribution */}
        <div className="bg-white rounded-xl shadow p-6">
          <h2 className="text-base font-semibold text-gray-800 mb-4">Estado de máquinas</h2>
          <div className="flex items-center gap-6">
            <ResponsiveContainer width="50%" height={180}>
              <PieChart>
                <Pie
                  data={statusChartData}
                  cx="50%"
                  cy="50%"
                  innerRadius={50}
                  outerRadius={80}
                  dataKey="value"
                  paddingAngle={3}
                >
                  {statusChartData.map((entry, i) => (
                    <Cell key={i} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip formatter={(v: number) => [`${v} máquinas`]} />
              </PieChart>
            </ResponsiveContainer>
            <div className="space-y-2 text-sm flex-1">
              {statusChartData.map((d) => (
                <div key={d.name} className="flex items-center gap-2">
                  <div className="w-3 h-3 rounded-full shrink-0" style={{ background: d.color }} />
                  <span className="text-gray-600">{d.name}</span>
                  <span className="font-bold text-gray-800 ml-auto">{d.value}</span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Bar chart — revenue */}
        <div className="bg-white rounded-xl shadow p-6">
          <h2 className="text-base font-semibold text-gray-800 mb-4">Ingresos recientes</h2>
          {revenueChartData.length > 0 ? (
            <ResponsiveContainer width="100%" height={180}>
              <BarChart data={revenueChartData}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                <XAxis dataKey="day" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} tickFormatter={(v) => `$${v}`} />
                <Tooltip formatter={(v: number) => [formatCurrency(v), 'Ingresos']} />
                <Bar dataKey="total" fill="#6366f1" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          ) : (
            <p className="text-sm text-gray-400 text-center py-10">Sin transacciones recientes</p>
          )}
        </div>
      </div>

      {/* Machine Map */}
      <MachineMap machines={data.machineStatuses} />

      {/* Bottom row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Recent transactions */}
        <div className="bg-white rounded-xl shadow p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-base font-semibold text-gray-800">Transacciones del día</h2>
            <button
              onClick={() => navigate('/transactions')}
              className="text-xs text-indigo-600 hover:underline"
            >
              Ver todas →
            </button>
          </div>
          {data.recentTransactions.length === 0 ? (
            <p className="text-sm text-gray-400 text-center py-6">Sin transacciones hoy</p>
          ) : (
            <div className="divide-y divide-gray-50">
              {data.recentTransactions.map((tx) => (
                <div key={tx.transactionNumber} className="flex items-center justify-between py-3">
                  <div>
                    <p className="text-sm font-medium text-gray-800">{tx.machineName}</p>
                    <p className="text-xs text-gray-400">{formatDate(tx.transactionDate)}</p>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-bold text-gray-900">{formatCurrency(tx.totalAmount)}</p>
                    <span
                      className={`text-xs px-2 py-0.5 rounded-full ${
                        tx.status === TransactionStatus.Completed
                          ? 'bg-green-100 text-green-700'
                          : 'bg-yellow-100 text-yellow-700'
                      }`}
                    >
                      {transactionStatusLabels[tx.status]}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Alerts */}
        <div className="bg-white rounded-xl shadow p-6">
          <h2 className="text-base font-semibold text-gray-800 mb-4">Alertas recientes</h2>
          {data.recentAlerts.length === 0 ? (
            <p className="text-sm text-gray-400 text-center py-6">Sin alertas activas ✅</p>
          ) : (
            <div className="space-y-2">
              {data.recentAlerts.map((a) => (
                <div
                  key={a.id}
                  className={`flex items-start gap-3 p-3 rounded-lg ${alertSeverityColors[a.severity]}`}
                >
                  <div>
                    <p className="text-sm font-semibold">{a.title}</p>
                    <p className="text-xs mt-0.5 opacity-80">
                      {a.machineName} · {a.message}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function KPICard({
  title,
  value,
  sub,
  icon,
  borderColor,
}: {
  title: string;
  value: string;
  sub: string;
  icon: string;
  borderColor: string;
}) {
  return (
    <div className={`bg-white rounded-xl shadow p-5 border-l-4 ${borderColor}`}>
      <div className="flex items-start justify-between">
        <div>
          <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">{title}</p>
          <p className="text-2xl font-bold text-gray-900 mt-1">{value}</p>
          <p className="text-xs text-gray-400 mt-1">{sub}</p>
        </div>
        <span className="text-2xl">{icon}</span>
      </div>
    </div>
  );
}
