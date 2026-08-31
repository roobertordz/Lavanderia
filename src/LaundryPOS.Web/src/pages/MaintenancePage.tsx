import { useEffect, useState, useCallback } from 'react';
import { maintenanceApi, machinesApi } from '@/api/endpoints';
import { useBranchStore } from '@/stores';
import { formatDate } from '@/utils/constants';
import type { MaintenanceRecord, Machine } from '@/types';

const MAINT_TYPE: Record<number, string> = {
  0: 'Preventivo',
  1: 'Correctivo',
  2: 'Predictivo',
  3: 'Emergencia',
};

const MAINT_STATUS: Record<number, { label: string; badge: string }> = {
  0: { label: 'Programado',   badge: 'bg-blue-100 text-blue-800' },
  1: { label: 'En progreso',  badge: 'bg-indigo-100 text-indigo-800' },
  2: { label: 'Completado',   badge: 'bg-green-100 text-green-800' },
  3: { label: 'Cancelado',    badge: 'bg-gray-200 text-gray-600' },
};

function toISODate(d: Date) { return d.toISOString().split('T')[0]; }

export function MaintenancePage() {
  const [records, setRecords]   = useState<MaintenanceRecord[]>([]);
  const [machines, setMachines] = useState<Machine[]>([]);
  const [loading, setLoading]   = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [fromDate, setFromDate] = useState(() => toISODate(new Date(Date.now() - 30 * 86400000)));
  const [toDate, setToDate]     = useState(() => toISODate(new Date(Date.now() + 14 * 86400000)));
  const selectedBranchId = useBranchStore((s) => s.selectedBranchId);

  const load = useCallback(async () => {
    if (!selectedBranchId) { setLoading(false); return; }
    setLoading(true);
    try {
      const [recRes, mRes] = await Promise.all([
        maintenanceApi.getByBranch(
          selectedBranchId,
          new Date(fromDate).toISOString(),
          new Date(toDate + 'T23:59:59').toISOString(),
        ),
        machinesApi.getByBranch(selectedBranchId),
      ]);
      if (recRes.data.success && recRes.data.data) setRecords(recRes.data.data);
      if (mRes.data.success && mRes.data.data) setMachines(mRes.data.data);
    } catch { /* silent */ }
    finally { setLoading(false); }
  }, [selectedBranchId, fromDate, toDate]);

  useEffect(() => { load(); }, [load]);

  const handleComplete = async (id: string) => {
    try {
      await maintenanceApi.complete(id, { technicianNotes: 'Completado desde panel', cost: 0 });
      load();
    } catch { /* silent */ }
  };

  if (!selectedBranchId) return <NoSelection />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Mantenimiento</h1>
        <button
          onClick={() => setShowForm(true)}
          className="bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold px-4 py-2 rounded-lg transition"
        >
          + Nuevo registro
        </button>
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
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
          />
        </div>
        <button
          onClick={load}
          className="bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium px-4 py-1.5 rounded-lg"
        >
          Buscar
        </button>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl shadow overflow-hidden">
        {loading ? (
          <div className="flex justify-center py-16">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600" />
          </div>
        ) : records.length === 0 ? (
          <div className="text-center py-16 text-gray-400">Sin registros en el periodo seleccionado.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-100">
              <thead className="bg-gray-50">
                <tr>
                  {['Máquina', 'Título', 'Tipo', 'Fecha programada', 'Estado', 'Acciones'].map((h) => (
                    <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {records.map((r) => {
                  const s = MAINT_STATUS[r.status] ?? { label: String(r.status), badge: 'bg-gray-100 text-gray-600' };
                  return (
                    <tr key={r.id} className="hover:bg-gray-50 transition">
                      <td className="px-4 py-3 text-sm font-medium text-gray-800">{r.machineName}</td>
                      <td className="px-4 py-3">
                        <p className="text-sm font-medium text-gray-800">{r.title}</p>
                        <p className="text-xs text-gray-400 truncate max-w-xs">{r.description}</p>
                      </td>
                      <td className="px-4 py-3 text-sm text-gray-600">{MAINT_TYPE[r.type] ?? r.type}</td>
                      <td className="px-4 py-3 text-sm text-gray-500">{formatDate(r.scheduledDate)}</td>
                      <td className="px-4 py-3">
                        <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${s.badge}`}>{s.label}</span>
                      </td>
                      <td className="px-4 py-3">
                        {r.status === 0 || r.status === 1 ? (
                          <button
                            onClick={() => handleComplete(r.id)}
                            className="text-xs bg-green-600 hover:bg-green-700 text-white px-3 py-1 rounded-lg"
                          >
                            ✓ Completar
                          </button>
                        ) : (
                          <span className="text-xs text-gray-400">—</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Create record modal */}
      {showForm && (
        <CreateMaintenanceModal
          machines={machines}
          branchId={selectedBranchId}
          onClose={() => setShowForm(false)}
          onCreated={() => { setShowForm(false); load(); }}
        />
      )}
    </div>
  );
}

// ─── Create Modal ───────────────────────────────────────────────────────────
function CreateMaintenanceModal({
  machines,
  branchId,
  onClose,
  onCreated,
}: {
  machines: Machine[];
  branchId: string;
  onClose: () => void;
  onCreated: () => void;
}) {
  const [form, setForm] = useState({
    machineId: machines[0]?.id ?? '',
    title: '',
    description: '',
    type: 0,
    scheduledDate: new Date().toISOString().split('T')[0],
  });
  const [saving, setSaving] = useState(false);
  const [error, setError]   = useState('');

  const handleSubmit = async () => {
    if (!form.title || !form.machineId) { setError('Completa los campos requeridos.'); return; }
    setSaving(true);
    try {
      await maintenanceApi.create({
        ...form,
        branchId,
        scheduledDate: new Date(form.scheduledDate).toISOString(),
      });
      onCreated();
    } catch {
      setError('Error al guardar. Intenta de nuevo.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md p-6 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-bold text-gray-900">Nuevo registro de mantenimiento</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl">✕</button>
        </div>

        <div className="space-y-3">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Máquina *</label>
            <select
              value={form.machineId}
              onChange={(e) => setForm({ ...form, machineId: e.target.value })}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
            >
              {machines.map((m) => (
                <option key={m.id} value={m.id}>#{m.number} {m.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Título *</label>
            <input
              type="text"
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
              placeholder="Ej: Revisión de rodamientos"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Descripción</label>
            <textarea
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              rows={2}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Tipo</label>
              <select
                value={form.type}
                onChange={(e) => setForm({ ...form, type: Number(e.target.value) })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
              >
                {Object.entries(MAINT_TYPE).map(([v, l]) => (
                  <option key={v} value={v}>{l}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Fecha programada</label>
              <input
                type="date"
                value={form.scheduledDate}
                onChange={(e) => setForm({ ...form, scheduledDate: e.target.value })}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400"
              />
            </div>
          </div>
          {error && <p className="text-sm text-red-600">{error}</p>}
        </div>

        <div className="flex gap-3 pt-2">
          <button
            onClick={onClose}
            className="flex-1 border border-gray-300 text-gray-700 py-2 rounded-lg text-sm hover:bg-gray-50"
          >
            Cancelar
          </button>
          <button
            onClick={handleSubmit}
            disabled={saving}
            className="flex-1 bg-indigo-600 hover:bg-indigo-700 text-white py-2 rounded-lg text-sm font-semibold disabled:opacity-50"
          >
            {saving ? 'Guardando…' : 'Guardar'}
          </button>
        </div>
      </div>
    </div>
  );
}

function NoSelection() {
  return (
    <div className="text-center py-12 text-gray-500">Selecciona una sucursal para ver el mantenimiento.</div>
  );
}
