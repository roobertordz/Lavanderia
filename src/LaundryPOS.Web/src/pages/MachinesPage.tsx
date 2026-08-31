import { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { machinesApi } from '@/api/endpoints';
import { useBranchStore } from '@/stores';
import { machineStatusLabels, machineTypeLabels } from '@/utils/constants';
import type { Machine } from '@/types';
import { MachineStatus, MachineType } from '@/types';

const statusBadge: Record<MachineStatus, string> = {
  [MachineStatus.Available]:    'bg-green-100 text-green-800',
  [MachineStatus.Occupied]:     'bg-blue-100 text-blue-800',
  [MachineStatus.InCycle]:      'bg-indigo-100 text-indigo-800',
  [MachineStatus.Finished]:     'bg-cyan-100 text-cyan-800',
  [MachineStatus.OutOfService]: 'bg-red-100 text-red-800',
  [MachineStatus.Error]:        'bg-red-200 text-red-900',
  [MachineStatus.Maintenance]:  'bg-yellow-100 text-yellow-800',
};

export function MachinesPage() {
  const [machines, setMachines] = useState<Machine[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<'all' | 'washer' | 'dryer'>('all');
  const selectedBranchId = useBranchStore((s) => s.selectedBranchId);
  const navigate = useNavigate();

  const load = useCallback(async () => {
    if (!selectedBranchId) { setLoading(false); return; }
    try {
      const res = await machinesApi.getByBranch(selectedBranchId);
      if (res.data.success && res.data.data) setMachines(res.data.data);
    } catch { /* silent */ }
    finally { setLoading(false); }
  }, [selectedBranchId]);

  useEffect(() => { load(); }, [load]);

  const filtered = machines.filter((m) => {
    if (filter === 'washer') return m.type === MachineType.Washer;
    if (filter === 'dryer')  return m.type === MachineType.Dryer;
    return true;
  });

  const counts = {
    available:    machines.filter((m) => m.status === MachineStatus.Available).length,
    inCycle:      machines.filter((m) => m.status === MachineStatus.InCycle || m.status === MachineStatus.Occupied).length,
    maintenance:  machines.filter((m) => m.status === MachineStatus.Maintenance).length,
    outOfService: machines.filter((m) => m.status === MachineStatus.OutOfService || m.status === MachineStatus.Error).length,
  };

  if (loading) return <Spinner />;
  if (!selectedBranchId) return <NoSelection />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Máquinas</h1>
        <button
          onClick={() => navigate('/cobro')}
          className="bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold px-4 py-2 rounded-lg transition"
        >
          + Cobrar / Activar
        </button>
      </div>

      {/* Summary pills */}
      <div className="flex flex-wrap gap-3">
        <Pill label="Disponibles"    value={counts.available}    color="bg-green-100 text-green-800" />
        <Pill label="En ciclo"        value={counts.inCycle}      color="bg-indigo-100 text-indigo-800" />
        <Pill label="Mantenimiento"  value={counts.maintenance}  color="bg-yellow-100 text-yellow-800" />
        <Pill label="Fuera servicio" value={counts.outOfService} color="bg-red-100 text-red-800" />
      </div>

      {/* Filter tabs */}
      <div className="flex gap-2">
        {(['all', 'washer', 'dryer'] as const).map((f) => (
          <button
            key={f}
            onClick={() => setFilter(f)}
            className={`px-4 py-1.5 rounded-full text-sm font-medium transition ${
              filter === f ? 'bg-indigo-600 text-white' : 'bg-white text-gray-600 border border-gray-300 hover:bg-gray-50'
            }`}
          >
            {f === 'all' ? 'Todas' : f === 'washer' ? '🫧 Lavadoras' : '🌀 Secadoras'}
          </button>
        ))}
      </div>

      {/* Machine grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        {filtered.map((m) => (
          <MachineCard key={m.id} machine={m} onActivate={() => navigate('/cobro', { state: { machineId: m.id } })} />
        ))}
      </div>
    </div>
  );
}

function MachineCard({ machine: m, onActivate }: { machine: Machine; onActivate: () => void }) {
  const isAvailable = m.status === MachineStatus.Available;
  const isActive    = m.status === MachineStatus.InCycle || m.status === MachineStatus.Occupied;
  const isAlert     = m.status === MachineStatus.Maintenance || m.status === MachineStatus.OutOfService || m.status === MachineStatus.Error;

  return (
    <div className={`bg-white rounded-xl shadow p-5 border-l-4 ${
      isAvailable ? 'border-green-500' : isActive ? 'border-blue-500' : isAlert ? 'border-yellow-500' : 'border-gray-300'
    }`}>
      <div className="flex items-start justify-between mb-3">
        <div>
          <p className="text-xs text-gray-400 font-medium">#{m.number}</p>
          <p className="font-bold text-gray-900">{m.name}</p>
          <p className="text-xs text-gray-500">{machineTypeLabels[m.type]} · {m.brand} {m.model}</p>
        </div>
        <span className="text-2xl">{m.type === MachineType.Washer ? '🫧' : '🌀'}</span>
      </div>

      <div className="space-y-1 mb-4">
        <div className="flex items-center justify-between text-xs">
          <span className="text-gray-500">Estado</span>
          <span className={`px-2 py-0.5 rounded-full font-medium ${statusBadge[m.status]}`}>
            {machineStatusLabels[m.status]}
          </span>
        </div>
        <div className="flex items-center justify-between text-xs">
          <span className="text-gray-500">Ciclos totales</span>
          <span className="font-medium text-gray-700">{m.totalCycles.toLocaleString()}</span>
        </div>
        <div className="flex items-center justify-between text-xs">
          <span className="text-gray-500">Precio</span>
          <span className="font-medium text-gray-700">${m.price.toFixed(2)} / {m.durationMinutes} min</span>
        </div>
      </div>

      {/* Action button */}
      {isAvailable ? (
        <button
          onClick={onActivate}
          className="w-full bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold py-2 rounded-lg transition"
        >
          Cobrar y activar
        </button>
      ) : isActive ? (
        <div className="w-full text-center text-sm text-blue-600 font-medium py-2 bg-blue-50 rounded-lg">
          ● En uso
        </div>
      ) : m.status === MachineStatus.Maintenance ? (
        <div className="w-full text-center text-sm text-yellow-700 font-medium py-2 bg-yellow-50 rounded-lg">
          🛠 En mantenimiento
        </div>
      ) : (
        <div className="w-full text-center text-sm text-red-600 font-medium py-2 bg-red-50 rounded-lg">
          ⚠ No disponible
        </div>
      )}
    </div>
  );
}

function Pill({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <span className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-sm font-semibold ${color}`}>
      {value} {label}
    </span>
  );
}

function Spinner() {
  return (
    <div className="flex items-center justify-center h-64">
      <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-indigo-600" />
    </div>
  );
}

function NoSelection() {
  return (
    <div className="text-center py-12 text-gray-500">Selecciona una sucursal para ver las máquinas.</div>
  );
}
