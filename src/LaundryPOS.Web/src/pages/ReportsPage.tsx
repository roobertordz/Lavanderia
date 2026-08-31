import { useEffect, useState, useCallback } from 'react';
import { reportsApi } from '@/api/endpoints';
import { useBranchStore } from '@/stores';
import { formatCurrency } from '@/utils/constants';
import type { RevenueReport, MachineUsageReport } from '@/types';
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer,
  BarChart, Bar, Legend,
} from 'recharts';

function toISODate(d: Date) { return d.toISOString().split('T')[0]; }

export function ReportsPage() {
  const [revenue,   setRevenue]   = useState<RevenueReport[]>([]);
  const [usage,     setUsage]     = useState<MachineUsageReport[]>([]);
  const [loading,   setLoading]   = useState(true);
  const [fromDate,  setFromDate]  = useState(() => toISODate(new Date(Date.now() - 7 * 86400000)));
  const [toDate,    setToDate]    = useState(() => toISODate(new Date()));
  const selectedBranchId = useBranchStore((s) => s.selectedBranchId);

  const load = useCallback(async () => {
    if (!selectedBranchId) { setLoading(false); return; }
    setLoading(true);
    try {
      const from = new Date(fromDate).toISOString();
      const to   = new Date(toDate + 'T23:59:59').toISOString();
      const [revRes, useRes] = await Promise.all([
        reportsApi.dailyRevenue(selectedBranchId, from, to),
        reportsApi.machineUsage(selectedBranchId, from, to),
      ]);
      if (revRes.data.success && revRes.data.data) setRevenue(revRes.data.data);
      if (useRes.data.success && useRes.data.data) setUsage(useRes.data.data);
    } catch { /* silent */ }
    finally { setLoading(false); }
  }, [selectedBranchId, fromDate, toDate]);

  useEffect(() => { load(); }, [load]);

  const totalRevenue = revenue.reduce((s, r) => s + r.revenue, 0);
  const totalTx      = revenue.reduce((s, r) => s + r.transactionCount, 0);

  const revenueChartData = revenue.map((r) => ({
    date: new Date(r.date).toLocaleDateString('es-MX', { month: 'short', day: 'numeric' }),
    revenue: r.revenue,
    transacciones: r.transactionCount,
  }));

  const usageChartData = usage.map((u) => ({
    maquina: u.machineName.replace('Wascomat ', ''),
    usos: u.totalUses,
    ingresos: u.totalRevenue,
  }));

  if (!selectedBranchId) return <NoSelection />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Reportes</h1>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl shadow p-4 flex flex-wrap gap-4 items-end">
        <div>
          <label className="block text-xs text-gray-500 mb-1">Desde</label>
          <input
            type="date"
            value={fromDate}
            onChange={(e) => setFromDate(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
          />
        </div>
        <div>
          <label className="block text-xs text-gray-500 mb-1">Hasta</label>
          <input
            type="date"
            value={toDate}
            onChange={(e) => setToDate(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
          />
        </div>
        <button
          onClick={load}
          className="bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium px-4 py-1.5 rounded-lg"
        >
          Generar
        </button>
        {/* Quick ranges */}
        <div className="flex gap-2 ml-auto">
          {[
            { label: 'Hoy',       days: 0 },
            { label: '7 días',    days: 7 },
            { label: '30 días',   days: 30 },
          ].map(({ label, days }) => (
            <button
              key={label}
              onClick={() => {
                setFromDate(toISODate(new Date(Date.now() - days * 86400000)));
                setToDate(toISODate(new Date()));
              }}
              className="text-xs px-3 py-1.5 rounded-full border border-gray-300 text-gray-600 hover:bg-gray-50"
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      {loading ? (
        <div className="flex justify-center py-16">
          <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-indigo-600" />
        </div>
      ) : (
        <>
          {/* KPIs */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            <KPI icon="💰" label="Ingresos totales"      value={formatCurrency(totalRevenue)}  color="border-green-500" />
            <KPI icon="🧾" label="Transacciones"         value={String(totalTx)}               color="border-blue-500" />
            <KPI icon="🔧" label="Máquinas con uso"      value={String(usage.length)}          color="border-indigo-500" />
            <KPI icon="📊" label="Promedio por día"      value={formatCurrency(totalRevenue / Math.max(1, revenueChartData.length))} color="border-purple-500" />
          </div>

          {/* Revenue area chart */}
          <div className="bg-white rounded-xl shadow p-6">
            <h2 className="text-base font-semibold text-gray-800 mb-4">Ingresos por día</h2>
            {revenueChartData.length === 0 ? (
              <p className="text-sm text-gray-400 text-center py-10">Sin datos en el periodo</p>
            ) : (
              <ResponsiveContainer width="100%" height={220}>
                <AreaChart data={revenueChartData}>
                  <defs>
                    <linearGradient id="revGrad" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%"  stopColor="#6366f1" stopOpacity={0.25} />
                      <stop offset="95%" stopColor="#6366f1" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                  <XAxis dataKey="date" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} tickFormatter={(v) => `$${v}`} />
                  <Tooltip formatter={(v: number, name: string) => [
                    name === 'revenue' ? formatCurrency(v) : v,
                    name === 'revenue' ? 'Ingresos' : 'Transacciones',
                  ]} />
                  <Area type="monotone" dataKey="revenue" stroke="#6366f1" fill="url(#revGrad)" strokeWidth={2} />
                  <Area type="monotone" dataKey="transacciones" stroke="#22c55e" fill="transparent" strokeWidth={1.5} strokeDasharray="4 2" />
                </AreaChart>
              </ResponsiveContainer>
            )}
          </div>

          {/* Machine usage bar chart */}
          <div className="bg-white rounded-xl shadow p-6">
            <h2 className="text-base font-semibold text-gray-800 mb-4">Uso por máquina</h2>
            {usageChartData.length === 0 ? (
              <p className="text-sm text-gray-400 text-center py-10">Sin datos en el periodo</p>
            ) : (
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={usageChartData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f0f0f0" />
                  <XAxis dataKey="maquina" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} />
                  <Tooltip />
                  <Legend />
                  <Bar dataKey="usos"     fill="#6366f1" radius={[4, 4, 0, 0]} name="Usos" />
                  <Bar dataKey="ingresos" fill="#22c55e"  radius={[4, 4, 0, 0]} name="Ingresos $" />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>

          {/* Usage table */}
          {usage.length > 0 && (
            <div className="bg-white rounded-xl shadow overflow-hidden">
              <div className="px-6 py-4 border-b border-gray-100">
                <h2 className="text-base font-semibold text-gray-800">Detalle por máquina</h2>
              </div>
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-100">
                  <thead className="bg-gray-50">
                    <tr>
                      {['Máquina', 'Usos', 'Ingresos', 'Duración prom.', 'Errores'].map((h) => (
                        <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase">{h}</th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {usage.map((u) => (
                      <tr key={u.machineId} className="hover:bg-gray-50">
                        <td className="px-4 py-3 text-sm font-medium text-gray-800">{u.machineName}</td>
                        <td className="px-4 py-3 text-sm text-gray-700">{u.totalUses}</td>
                        <td className="px-4 py-3 text-sm font-bold text-green-700">{formatCurrency(u.totalRevenue)}</td>
                        <td className="px-4 py-3 text-sm text-gray-600">{u.averageUsageMinutes} min</td>
                        <td className="px-4 py-3">
                          <span className={`text-xs px-2 py-0.5 rounded-full ${u.errorCount > 0 ? 'bg-red-100 text-red-700' : 'bg-green-100 text-green-700'}`}>
                            {u.errorCount} errores
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function KPI({ icon, label, value, color }: { icon: string; label: string; value: string; color: string }) {
  return (
    <div className={`bg-white rounded-xl shadow p-5 border-l-4 ${color}`}>
      <div className="flex items-start justify-between">
        <div>
          <p className="text-xs text-gray-500 font-medium uppercase tracking-wide">{label}</p>
          <p className="text-2xl font-bold text-gray-900 mt-1">{value}</p>
        </div>
        <span className="text-2xl">{icon}</span>
      </div>
    </div>
  );
}

function NoSelection() {
  return (
    <div className="text-center py-12 text-gray-500">Selecciona una sucursal para ver los reportes.</div>
  );
}
