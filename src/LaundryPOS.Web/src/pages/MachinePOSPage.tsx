import { useState, useEffect, useCallback } from 'react';
import { machinesApi, paymentsApi } from '@/api/endpoints';
import { useBranchStore } from '@/stores';
import { MachineType, PaymentMethod } from '@/types';
import type { Machine } from '@/types';
import { formatCurrency, machineTypeLabels } from '@/utils/constants';
import { clsx } from 'clsx';
import toast from 'react-hot-toast';
import { useLocation } from 'react-router-dom';

type Step = 'select' | 'confirm' | 'payment' | 'processing' | 'done' | 'error';

const paymentMethods = [
  { value: PaymentMethod.Cash,       label: 'Efectivo',        icon: '💵' },
  { value: PaymentMethod.CreditCard, label: 'Tarjeta crédito', icon: '💳' },
  { value: PaymentMethod.DebitCard,  label: 'Tarjeta débito',  icon: '🏦' },
];

export function MachinePOSPage() {
  const [machines, setMachines]               = useState<Machine[]>([]);
  const [selected, setSelected]               = useState<Machine | null>(null);
  const [step, setStep]                       = useState<Step>('select');
  const [paymentMethod, setPaymentMethod]     = useState<PaymentMethod>(PaymentMethod.Cash);
  const [txNumber, setTxNumber]               = useState('');
  const [errorMsg, setErrorMsg]               = useState('');
  const [loading, setLoading]                 = useState(false);
  const selectedBranchId = useBranchStore((s) => s.selectedBranchId);
  const location = useLocation();
  const preselectedMachineId = (location.state as { machineId?: string } | null)?.machineId;

  const loadMachines = useCallback(async () => {
    if (!selectedBranchId) return;
    try {
      const res = await machinesApi.getAvailable(selectedBranchId);
      if (res.data.success && res.data.data) {
        setMachines(res.data.data);
        // Auto-select if coming from MachinesPage
        if (preselectedMachineId) {
          const m = res.data.data.find((x) => x.id === preselectedMachineId);
          if (m) { setSelected(m); setStep('confirm'); }
        }
      }
    } catch {
      toast.error('Error al cargar máquinas');
    }
  }, [selectedBranchId, preselectedMachineId]);

  useEffect(() => { loadMachines(); }, [loadMachines]);

  const handleSelect = (m: Machine) => { setSelected(m); setStep('confirm'); };

  const handlePay = async () => {
    if (!selected || !selectedBranchId) return;
    setStep('processing');
    setLoading(true);
    try {
      const res = await paymentsApi.process({
        machineId:     selected.id,
        branchId:      selectedBranchId,
        paymentMethod: paymentMethod,
        paymentGateway: paymentMethod === PaymentMethod.Cash ? 'Cash' : 'Stripe',
      });

      if (res.data.success && res.data.data) {
        setTxNumber((res.data.data as { transactionNumber?: string }).transactionNumber ?? '');
        setStep('done');
        toast.success('¡Pago exitoso! Máquina iniciando…');
        loadMachines(); // refresh list
      } else {
        setErrorMsg(res.data.error ?? 'Error procesando el pago');
        setStep('error');
      }
    } catch (e: unknown) {
      const err = e as { response?: { data?: { message?: string } } };
      setErrorMsg(err?.response?.data?.message ?? 'Error de conexión');
      setStep('error');
    } finally {
      setLoading(false);
    }
  };

  const reset = () => { setSelected(null); setStep('select'); setErrorMsg(''); loadMachines(); };

  if (!selectedBranchId) {
    return (
      <div className="text-center py-16 text-gray-500">
        Selecciona una sucursal desde el menú superior para continuar.
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">🧺 Cobro y Activación de Máquinas</h1>
        {step !== 'select' && (
          <button onClick={reset} className="text-sm text-indigo-600 hover:underline">← Volver</button>
        )}
      </div>

      {/* ── STEP 1: Select machine ── */}
      {step === 'select' && (
        <div>
          <p className="text-sm text-gray-500 mb-4">Selecciona la máquina disponible que el cliente va a usar:</p>
          {machines.length === 0 ? (
            <div className="text-center py-16 text-gray-400">
              No hay máquinas disponibles en este momento.
            </div>
          ) : (
            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-4">
              {machines.map((m) => (
                <button
                  key={m.id}
                  onClick={() => handleSelect(m)}
                  className="bg-white rounded-xl shadow hover:shadow-md border-2 border-transparent hover:border-indigo-400 transition-all p-5 text-center"
                >
                  <div className="text-3xl font-bold text-indigo-700">#{m.number}</div>
                  <div className="text-sm font-medium text-gray-700 mt-1">
                    {machineTypeLabels[m.type as MachineType]}
                  </div>
                  <div className="text-xs text-gray-500">{m.brand} {m.model}</div>
                  <div className="mt-3 bg-green-50 rounded-lg py-1.5">
                    <span className="text-green-700 font-bold text-lg">{formatCurrency(m.price)}</span>
                  </div>
                  <div className="text-xs text-gray-400 mt-1">{m.durationMinutes} min</div>
                </button>
              ))}
            </div>
          )}
        </div>
      )}

      {/* ── STEP 2: Confirm + select payment ── */}
      {step === 'confirm' && selected && (
        <div className="bg-white rounded-2xl shadow p-8 max-w-md mx-auto">
          <h2 className="text-lg font-semibold text-gray-800 mb-6">Confirmar cobro</h2>

          <div className="bg-indigo-50 rounded-xl p-4 mb-6">
            <div className="flex justify-between items-center">
              <div>
                <p className="font-bold text-indigo-900 text-xl">Máquina #{selected.number}</p>
                <p className="text-sm text-indigo-700">{machineTypeLabels[selected.type as MachineType]} · {selected.brand} {selected.model}</p>
                <p className="text-sm text-gray-600 mt-1">Duración: {selected.durationMinutes} minutos</p>
              </div>
              <div className="text-right">
                <p className="text-3xl font-bold text-indigo-700">{formatCurrency(selected.price)}</p>
              </div>
            </div>
          </div>

          <p className="text-sm font-medium text-gray-700 mb-3">Método de pago:</p>
          <div className="space-y-2 mb-6">
            {paymentMethods.map((pm) => (
              <button
                key={pm.value}
                onClick={() => setPaymentMethod(pm.value)}
                className={clsx(
                  'w-full flex items-center gap-3 px-4 py-3 rounded-xl border-2 transition-colors',
                  paymentMethod === pm.value
                    ? 'border-indigo-500 bg-indigo-50 text-indigo-700'
                    : 'border-gray-200 hover:border-gray-300 text-gray-700'
                )}
              >
                <span className="text-xl">{pm.icon}</span>
                <span className="font-medium">{pm.label}</span>
                {paymentMethod === pm.value && <span className="ml-auto text-indigo-500">✓</span>}
              </button>
            ))}
          </div>

          <button
            onClick={() => setStep('payment')}
            className="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-3 rounded-xl transition-colors"
          >
            Continuar →
          </button>
        </div>
      )}

      {/* ── STEP 3: Final confirmation ── */}
      {step === 'payment' && selected && (
        <div className="bg-white rounded-2xl shadow p-8 max-w-md mx-auto text-center">
          <div className="text-5xl mb-4">
            {paymentMethods.find((p) => p.value === paymentMethod)?.icon}
          </div>
          <h2 className="text-xl font-bold text-gray-800 mb-2">
            Cobrar {formatCurrency(selected.price)}
          </h2>
          <p className="text-gray-500 mb-1">Método: {paymentMethods.find((p) => p.value === paymentMethod)?.label}</p>
          <p className="text-gray-500 mb-8">Máquina #{selected.number} · {selected.durationMinutes} min</p>

          <button
            onClick={handlePay}
            disabled={loading}
            className="w-full bg-green-600 hover:bg-green-700 disabled:bg-green-400 text-white font-bold py-4 rounded-xl text-lg transition-colors"
          >
            {loading ? 'Procesando…' : '✅ Confirmar y activar máquina'}
          </button>
          <button onClick={() => setStep('confirm')} className="w-full mt-3 text-sm text-gray-500 hover:text-gray-700">
            ← Cambiar método de pago
          </button>
        </div>
      )}

      {/* ── STEP 4: Processing ── */}
      {step === 'processing' && (
        <div className="text-center py-16">
          <div className="animate-spin rounded-full h-16 w-16 border-b-4 border-indigo-600 mx-auto mb-4" />
          <p className="text-lg font-medium text-gray-700">Procesando pago y activando máquina…</p>
          <p className="text-sm text-gray-400 mt-2">Enviando señal a la máquina Wascomat</p>
        </div>
      )}

      {/* ── STEP 5: Success ── */}
      {step === 'done' && (
        <div className="bg-white rounded-2xl shadow p-10 max-w-md mx-auto text-center">
          <div className="text-6xl mb-4">✅</div>
          <h2 className="text-2xl font-bold text-green-700 mb-2">¡Listo!</h2>
          <p className="text-gray-600 mb-1">La máquina fue activada correctamente.</p>
          {txNumber && <p className="text-sm text-gray-400 mb-6">Transacción: <span className="font-mono">{txNumber}</span></p>}
          <button onClick={reset} className="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-3 rounded-xl">
            Cobrar otra máquina
          </button>
        </div>
      )}

      {/* ── STEP 6: Error ── */}
      {step === 'error' && (
        <div className="bg-white rounded-2xl shadow p-10 max-w-md mx-auto text-center">
          <div className="text-6xl mb-4">❌</div>
          <h2 className="text-xl font-bold text-red-700 mb-2">Error al procesar</h2>
          <p className="text-gray-600 mb-6">{errorMsg}</p>
          <button onClick={reset} className="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-3 rounded-xl">
            Intentar de nuevo
          </button>
        </div>
      )}
    </div>
  );
}
