import { useEffect, useState, useCallback } from 'react';
import toast from 'react-hot-toast';
import { laundryOrdersApi } from '@/api/endpoints';
import { useBranchStore } from '@/stores';
import {
  laundryOrderServiceTypeLabels,
  laundryOrderStatusLabels,
  laundryOrderStatusColors,
  formatCurrency,
  formatDate,
} from '@/utils/constants';
import type { LaundryOrder } from '@/types';
import { LaundryOrderServiceType, LaundryOrderStatus, PaymentMethod } from '@/types';

const statusOptions = Object.values(LaundryOrderStatus).filter((v) => typeof v === 'number') as LaundryOrderStatus[];
const DEFAULT_PRICE_PER_KG = 25;
const DEFAULT_PRICE_PER_COMFORTER = 120;
const COMFORTER_SIZES = ['Individual', 'Matrimonial', 'Queen', 'King'];

export function LaundryOrdersPage() {
  const [orders, setOrders] = useState<LaundryOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState<'all' | LaundryOrderStatus>('all');
  const [showForm, setShowForm] = useState(false);
  const selectedBranchId = useBranchStore((s) => s.selectedBranchId);

  const load = useCallback(async () => {
    if (!selectedBranchId) { setLoading(false); return; }
    try {
      const res = await laundryOrdersApi.getByBranch(selectedBranchId);
      if (res.data.success && res.data.data) setOrders(res.data.data);
    } catch { /* silent */ }
    finally { setLoading(false); }
  }, [selectedBranchId]);

  useEffect(() => { load(); }, [load]);

  const filtered = statusFilter === 'all' ? orders : orders.filter((o) => o.status === statusFilter);

  const counts = {
    total: orders.length,
    pendientes: orders.filter((o) => o.status !== LaundryOrderStatus.Delivered && o.status !== LaundryOrderStatus.Cancelled).length,
    listos: orders.filter((o) => o.status === LaundryOrderStatus.Ready).length,
  };

  const handleAdvanceStatus = async (order: LaundryOrder, newStatus: LaundryOrderStatus) => {
    try {
      await laundryOrdersApi.updateStatus(order.id, newStatus);
      toast.success('Estado actualizado.');
      load();
    } catch {
      toast.error('Error al actualizar el estado.');
    }
  };

  const handleRegisterPayment = async (order: LaundryOrder, method: PaymentMethod) => {
    try {
      await laundryOrdersApi.registerPayment(order.id, method);
      toast.success('Pago registrado.');
      load();
    } catch {
      toast.error('Error al registrar el pago.');
    }
  };

  const handleCancel = async (order: LaundryOrder) => {
    if (!confirm(`¿Cancelar el encargo ${order.orderNumber}?`)) return;
    try {
      await laundryOrdersApi.delete(order.id);
      toast.success('Encargo cancelado.');
      load();
    } catch {
      toast.error('Error al cancelar el encargo.');
    }
  };

  if (loading) return <Spinner />;
  if (!selectedBranchId) return <NoSelection />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Lavado por Encargo</h1>
        <button
          onClick={() => setShowForm(true)}
          className="bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold px-4 py-2 rounded-lg transition"
        >
          + Nuevo Encargo
        </button>
      </div>

      <div className="flex flex-wrap gap-3">
        <Pill label="encargos" value={counts.total} color="bg-indigo-100 text-indigo-800" />
        <Pill label="pendientes" value={counts.pendientes} color="bg-yellow-100 text-yellow-800" />
        <Pill label="listos para entregar" value={counts.listos} color="bg-green-100 text-green-800" />
      </div>

      <div className="flex flex-wrap gap-2">
        <button
          onClick={() => setStatusFilter('all')}
          className={`px-4 py-1.5 rounded-full text-sm font-medium transition ${
            statusFilter === 'all' ? 'bg-indigo-600 text-white' : 'bg-white text-gray-600 border border-gray-300 hover:bg-gray-50'
          }`}
        >
          Todos
        </button>
        {statusOptions.map((s) => (
          <button
            key={s}
            onClick={() => setStatusFilter(s)}
            className={`px-4 py-1.5 rounded-full text-sm font-medium transition ${
              statusFilter === s ? 'bg-indigo-600 text-white' : 'bg-white text-gray-600 border border-gray-300 hover:bg-gray-50'
            }`}
          >
            {laundryOrderStatusLabels[s]}
          </button>
        ))}
      </div>

      <div className="bg-white rounded-xl shadow overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <Th>Encargo</Th>
              <Th>Cliente</Th>
              <Th>Servicio</Th>
              <Th>Detalle</Th>
              <Th>Total</Th>
              <Th>Estado</Th>
              <Th>Pago</Th>
              <Th></Th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {filtered.map((o) => (
              <OrderRow
                key={o.id}
                order={o}
                onAdvance={(s) => handleAdvanceStatus(o, s)}
                onPay={(m) => handleRegisterPayment(o, m)}
                onCancel={() => handleCancel(o)}
              />
            ))}
            {filtered.length === 0 && (
              <tr>
                <td colSpan={8} className="text-center py-12 text-gray-500">No hay encargos que coincidan.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {showForm && (
        <NewOrderModal
          branchId={selectedBranchId}
          onClose={() => setShowForm(false)}
          onCreated={() => { setShowForm(false); load(); }}
        />
      )}
    </div>
  );
}

// ─── Order Row ──────────────────────────────────────────────────────────────
function OrderRow({
  order: o,
  onAdvance,
  onPay,
  onCancel,
}: {
  order: LaundryOrder;
  onAdvance: (status: LaundryOrderStatus) => void;
  onPay: (method: PaymentMethod) => void;
  onCancel: () => void;
}) {
  const isFinal = o.status === LaundryOrderStatus.Delivered || o.status === LaundryOrderStatus.Cancelled;
  const nextStatus =
    o.status === LaundryOrderStatus.Received ? LaundryOrderStatus.InProgress :
    o.status === LaundryOrderStatus.InProgress ? LaundryOrderStatus.Ready :
    o.status === LaundryOrderStatus.Ready ? LaundryOrderStatus.Delivered : null;

  const detail = o.serviceType === LaundryOrderServiceType.ByWeight
    ? `${o.weightKg} kg × ${formatCurrency(o.pricePerKg ?? 0)}/kg`
    : `${o.comforterCount} edredón(es)${o.comforterSize ? ` (${o.comforterSize})` : ''} × ${formatCurrency(o.pricePerComforter ?? 0)}`;

  return (
    <tr>
      <Td>
        <p className="font-semibold text-gray-900">{o.orderNumber}</p>
        <p className="text-xs text-gray-500">{formatDate(o.receivedAt)}</p>
      </Td>
      <Td>
        <p className="text-gray-900">{o.customerName}</p>
        <p className="text-xs text-gray-500">{o.customerPhone}</p>
      </Td>
      <Td>{laundryOrderServiceTypeLabels[o.serviceType]}</Td>
      <Td className="text-xs text-gray-600">{detail}</Td>
      <Td className="font-semibold">{formatCurrency(o.totalPrice)}</Td>
      <Td>
        <span className={`inline-flex px-2 py-1 rounded-full text-xs font-semibold ${laundryOrderStatusColors[o.status]}`}>
          {laundryOrderStatusLabels[o.status]}
        </span>
      </Td>
      <Td>
        {o.paymentStatus === 3 ? (
          <span className="text-xs text-green-700 font-semibold">Pagado</span>
        ) : (
          <button
            onClick={() => onPay(PaymentMethod.Cash)}
            className="text-xs text-indigo-600 hover:underline font-semibold"
          >
            Registrar pago
          </button>
        )}
      </Td>
      <Td>
        <div className="flex gap-2 justify-end">
          {!isFinal && nextStatus !== null && (
            <button
              onClick={() => onAdvance(nextStatus)}
              className="text-xs bg-indigo-50 text-indigo-700 hover:bg-indigo-100 font-semibold px-3 py-1.5 rounded-lg"
            >
              {laundryOrderStatusLabels[nextStatus]} →
            </button>
          )}
          {!isFinal && (
            <button
              onClick={onCancel}
              className="text-xs bg-red-50 text-red-700 hover:bg-red-100 font-semibold px-3 py-1.5 rounded-lg"
            >
              Cancelar
            </button>
          )}
        </div>
      </Td>
    </tr>
  );
}

// ─── New Order Modal ────────────────────────────────────────────────────────
function NewOrderModal({
  branchId,
  onClose,
  onCreated,
}: {
  branchId: string;
  onClose: () => void;
  onCreated: () => void;
}) {
  const [serviceType, setServiceType] = useState<LaundryOrderServiceType>(LaundryOrderServiceType.ByWeight);
  const [customerName, setCustomerName] = useState('');
  const [customerPhone, setCustomerPhone] = useState('');
  const [weightKg, setWeightKg] = useState('');
  const [pricePerKg, setPricePerKg] = useState(String(DEFAULT_PRICE_PER_KG));
  const [comforterCount, setComforterCount] = useState('1');
  const [comforterSize, setComforterSize] = useState(COMFORTER_SIZES[0]);
  const [pricePerComforter, setPricePerComforter] = useState(String(DEFAULT_PRICE_PER_COMFORTER));
  const [notes, setNotes] = useState('');
  const [saving, setSaving] = useState(false);

  const total = serviceType === LaundryOrderServiceType.ByWeight
    ? (parseFloat(weightKg) || 0) * (parseFloat(pricePerKg) || 0)
    : (parseInt(comforterCount) || 0) * (parseFloat(pricePerComforter) || 0);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!customerName.trim()) { toast.error('El nombre del cliente es obligatorio.'); return; }
    if (serviceType === LaundryOrderServiceType.ByWeight && (!weightKg || !pricePerKg)) {
      toast.error('Indica el peso y el precio por kilo.');
      return;
    }
    if (serviceType === LaundryOrderServiceType.Comforter && (!comforterCount || !pricePerComforter)) {
      toast.error('Indica la cantidad y el precio por edredón.');
      return;
    }

    setSaving(true);
    try {
      await laundryOrdersApi.create({
        serviceType,
        customerName: customerName.trim(),
        customerPhone: customerPhone.trim() || undefined,
        weightKg: serviceType === LaundryOrderServiceType.ByWeight ? parseFloat(weightKg) : undefined,
        pricePerKg: serviceType === LaundryOrderServiceType.ByWeight ? parseFloat(pricePerKg) : undefined,
        comforterCount: serviceType === LaundryOrderServiceType.Comforter ? parseInt(comforterCount) : undefined,
        comforterSize: serviceType === LaundryOrderServiceType.Comforter ? comforterSize : undefined,
        pricePerComforter: serviceType === LaundryOrderServiceType.Comforter ? parseFloat(pricePerComforter) : undefined,
        notes: notes.trim() || undefined,
        branchId,
      });
      toast.success('Encargo registrado.');
      onCreated();
    } catch {
      toast.error('Error al registrar el encargo.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="p-6 space-y-4">
          <h2 className="text-lg font-bold text-gray-900">Nuevo Encargo — Lavado por Encargo</h2>

          {/* Toggle: por kilo / edredones */}
          <div className="grid grid-cols-2 gap-2">
            <button
              type="button"
              onClick={() => setServiceType(LaundryOrderServiceType.ByWeight)}
              className={`py-3 rounded-lg text-sm font-semibold border transition ${
                serviceType === LaundryOrderServiceType.ByWeight
                  ? 'bg-indigo-600 text-white border-indigo-600'
                  : 'bg-white text-gray-600 border-gray-300 hover:bg-gray-50'
              }`}
            >
              ⚖️ Lavado por Kilo
            </button>
            <button
              type="button"
              onClick={() => setServiceType(LaundryOrderServiceType.Comforter)}
              className={`py-3 rounded-lg text-sm font-semibold border transition ${
                serviceType === LaundryOrderServiceType.Comforter
                  ? 'bg-indigo-600 text-white border-indigo-600'
                  : 'bg-white text-gray-600 border-gray-300 hover:bg-gray-50'
              }`}
            >
              🛏️ Edredones
            </button>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-2 gap-3">
              <label className="block text-sm col-span-2">
                <span className="text-gray-700 font-medium">Cliente *</span>
                <input
                  type="text"
                  value={customerName}
                  onChange={(e) => setCustomerName(e.target.value)}
                  className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-400"
                  required
                />
              </label>
              <label className="block text-sm col-span-2">
                <span className="text-gray-700 font-medium">Teléfono</span>
                <input
                  type="tel"
                  value={customerPhone}
                  onChange={(e) => setCustomerPhone(e.target.value)}
                  className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-400"
                />
              </label>

              {serviceType === LaundryOrderServiceType.ByWeight ? (
                <>
                  <label className="block text-sm">
                    <span className="text-gray-700 font-medium">Peso (kg) *</span>
                    <input
                      type="number" step="0.1" min="0"
                      value={weightKg}
                      onChange={(e) => setWeightKg(e.target.value)}
                      className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-400"
                      required
                    />
                  </label>
                  <label className="block text-sm">
                    <span className="text-gray-700 font-medium">Precio/kg *</span>
                    <input
                      type="number" step="0.5" min="0"
                      value={pricePerKg}
                      onChange={(e) => setPricePerKg(e.target.value)}
                      className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-400"
                      required
                    />
                  </label>
                </>
              ) : (
                <>
                  <label className="block text-sm">
                    <span className="text-gray-700 font-medium">Cantidad *</span>
                    <input
                      type="number" min="1"
                      value={comforterCount}
                      onChange={(e) => setComforterCount(e.target.value)}
                      className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-400"
                      required
                    />
                  </label>
                  <label className="block text-sm">
                    <span className="text-gray-700 font-medium">Tamaño</span>
                    <select
                      value={comforterSize}
                      onChange={(e) => setComforterSize(e.target.value)}
                      className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-400"
                    >
                      {COMFORTER_SIZES.map((s) => <option key={s} value={s}>{s}</option>)}
                    </select>
                  </label>
                  <label className="block text-sm col-span-2">
                    <span className="text-gray-700 font-medium">Precio por edredón *</span>
                    <input
                      type="number" step="1" min="0"
                      value={pricePerComforter}
                      onChange={(e) => setPricePerComforter(e.target.value)}
                      className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-400"
                      required
                    />
                  </label>
                </>
              )}

              <label className="block text-sm col-span-2">
                <span className="text-gray-700 font-medium">Notas</span>
                <textarea
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  rows={2}
                  className="mt-1 w-full border border-gray-300 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-indigo-400"
                />
              </label>
            </div>

            <div className="flex items-center justify-between bg-gray-50 rounded-lg px-4 py-3">
              <span className="text-sm text-gray-600">Total estimado</span>
              <span className="text-xl font-bold text-indigo-600">{formatCurrency(total)}</span>
            </div>

            <div className="flex justify-end gap-2">
              <button type="button" onClick={onClose} className="px-4 py-2 text-sm font-semibold text-gray-600 hover:bg-gray-100 rounded-lg">
                Cancelar
              </button>
              <button
                type="submit"
                disabled={saving}
                className="px-4 py-2 text-sm font-semibold bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg disabled:opacity-50"
              >
                {saving ? 'Guardando…' : 'Registrar Encargo'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

// ─── Small helpers ──────────────────────────────────────────────────────────
function Th({ children }: { children?: React.ReactNode }) {
  return <th className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">{children}</th>;
}

function Td({ children, className = '' }: { children?: React.ReactNode; className?: string }) {
  return <td className={`px-4 py-3 text-sm ${className}`}>{children}</td>;
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
    <div className="text-center py-12 text-gray-500">Selecciona una sucursal para ver los encargos.</div>
  );
}
