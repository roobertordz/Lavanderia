import { useState, useCallback } from 'react';
import { paymentsApi, machinesApi } from '@/api/endpoints';
import { useBranchStore } from '@/stores';
import { useEffect } from 'react';
import {
  formatCurrency,
  machineTypeLabels,
} from '@/utils/constants';
import type { Machine } from '@/types';
import { MachineType, PaymentMethod } from '@/types';
import { clsx } from 'clsx';
import toast from 'react-hot-toast';

type Step = 'select-machine' | 'confirm' | 'payment' | 'processing' | 'success' | 'error';

export function KioskPage() {
  const [step, setStep] = useState<Step>('select-machine');
  const [machines, setMachines] = useState<Machine[]>([]);
  const [selectedMachine, setSelectedMachine] = useState<Machine | null>(null);
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>(PaymentMethod.CreditCard);
  const [paymentGateway, setPaymentGateway] = useState('Stripe');
  const [error, setError] = useState<string | null>(null);
  const selectedBranchId = useBranchStore((s) => s.selectedBranchId);

  const loadMachines = useCallback(async () => {
    if (!selectedBranchId) return;
    try {
      const response = await machinesApi.getAvailable(selectedBranchId);
      if (response.data.success && response.data.data) {
        setMachines(response.data.data);
      }
    } catch {
      toast.error('Error al cargar máquinas');
    }
  }, [selectedBranchId]);

  useEffect(() => {
    loadMachines();
  }, [loadMachines]);

  const handleSelectMachine = (machine: Machine) => {
    setSelectedMachine(machine);
    setStep('confirm');
  };

  const handleProcessPayment = async () => {
    if (!selectedMachine || !selectedBranchId) return;
    setStep('processing');

    try {
      const response = await paymentsApi.process({
        machineId: selectedMachine.id,
        branchId: selectedBranchId,
        paymentMethod,
        paymentGateway,
      });

      if (response.data.success) {
        setStep('success');
        toast.success('¡Pago procesado! La máquina está iniciando.');
      } else {
        setError(response.data.error || 'Error al procesar el pago');
        setStep('error');
      }
    } catch {
      setError('Error de conexión. Intente de nuevo.');
      setStep('error');
    }
  };

  const handleReset = () => {
    setStep('select-machine');
    setSelectedMachine(null);
    setError(null);
    loadMachines();
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-900 to-indigo-900 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-4xl overflow-hidden">
        {/* Header */}
        <div className="bg-indigo-600 text-white px-8 py-6">
          <h1 className="text-3xl font-bold text-center">LaundryPOS</h1>
          <p className="text-center text-indigo-200 mt-1">Lavandería de Autoservicio</p>
        </div>

        <div className="p-8">
          {/* Step 1: Select Machine */}
          {step === 'select-machine' && (
            <div>
              <h2 className="text-2xl font-bold text-gray-800 mb-6 text-center">
                Selecciona una máquina disponible
              </h2>
              <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                {machines.map((machine) => (
                  <button
                    key={machine.id}
                    onClick={() => handleSelectMachine(machine)}
                    className="bg-green-50 border-2 border-green-200 rounded-xl p-6 hover:border-green-500 hover:shadow-lg transition-all"
                  >
                    <div className="text-4xl font-bold text-green-600">#{machine.number}</div>
                    <div className="text-sm text-gray-600 mt-2">{machine.name}</div>
                    <div className="text-xs text-gray-500">
                      {machineTypeLabels[machine.type as MachineType]} • {machine.capacity}
                    </div>
                    <div className="text-xl font-bold text-indigo-600 mt-3">
                      {formatCurrency(machine.price)}
                    </div>
                    <div className="text-xs text-gray-400">{machine.durationMinutes} minutos</div>
                  </button>
                ))}
              </div>
              {machines.length === 0 && (
                <p className="text-center text-gray-500 py-12">
                  No hay máquinas disponibles en este momento.
                </p>
              )}
            </div>
          )}

          {/* Step 2: Confirm */}
          {step === 'confirm' && selectedMachine && (
            <div className="text-center max-w-md mx-auto">
              <h2 className="text-2xl font-bold text-gray-800 mb-6">Confirmar servicio</h2>
              <div className="bg-gray-50 rounded-xl p-6 mb-6">
                <div className="text-5xl font-bold text-indigo-600">#{selectedMachine.number}</div>
                <div className="text-lg text-gray-600 mt-2">{selectedMachine.name}</div>
                <div className="text-sm text-gray-500">
                  {machineTypeLabels[selectedMachine.type as MachineType]} • {selectedMachine.capacity}
                </div>
                <hr className="my-4" />
                <div className="text-3xl font-bold text-green-600">
                  {formatCurrency(selectedMachine.price)}
                </div>
                <div className="text-sm text-gray-400">
                  Duración: {selectedMachine.durationMinutes} minutos
                </div>
              </div>
              <div className="flex gap-4">
                <button onClick={handleReset} className="flex-1 bg-gray-200 text-gray-700 rounded-lg py-3 font-semibold hover:bg-gray-300">
                  Cancelar
                </button>
                <button onClick={() => setStep('payment')} className="flex-1 bg-indigo-600 text-white rounded-lg py-3 font-semibold hover:bg-indigo-700">
                  Continuar al pago
                </button>
              </div>
            </div>
          )}

          {/* Step 3: Payment */}
          {step === 'payment' && selectedMachine && (
            <div className="text-center max-w-md mx-auto">
              <h2 className="text-2xl font-bold text-gray-800 mb-6">Forma de pago</h2>
              <div className="space-y-3 mb-6">
                {[
                  { method: PaymentMethod.CreditCard, label: 'Tarjeta de Crédito', gateway: 'Stripe' },
                  { method: PaymentMethod.DebitCard, label: 'Tarjeta de Débito', gateway: 'Stripe' },
                  { method: PaymentMethod.DigitalWallet, label: 'Mercado Pago', gateway: 'MercadoPago' },
                  { method: PaymentMethod.Cash, label: 'Efectivo', gateway: 'Cash' },
                ].map(({ method, label, gateway }) => (
                  <button
                    key={method}
                    onClick={() => { setPaymentMethod(method); setPaymentGateway(gateway); }}
                    className={clsx(
                      'w-full py-4 rounded-xl border-2 font-semibold transition-all',
                      paymentMethod === method
                        ? 'border-indigo-500 bg-indigo-50 text-indigo-700'
                        : 'border-gray-200 text-gray-600 hover:border-gray-300'
                    )}
                  >
                    {label}
                  </button>
                ))}
              </div>
              <div className="text-3xl font-bold text-green-600 mb-6">
                Total: {formatCurrency(selectedMachine.price)}
              </div>
              <div className="flex gap-4">
                <button onClick={() => setStep('confirm')} className="flex-1 bg-gray-200 text-gray-700 rounded-lg py-3 font-semibold hover:bg-gray-300">
                  Atrás
                </button>
                <button onClick={handleProcessPayment} className="flex-1 bg-green-600 text-white rounded-lg py-3 font-semibold hover:bg-green-700 text-lg">
                  Pagar
                </button>
              </div>
            </div>
          )}

          {/* Step 4: Processing */}
          {step === 'processing' && (
            <div className="text-center py-12">
              <div className="animate-spin rounded-full h-20 w-20 border-b-4 border-indigo-600 mx-auto" />
              <p className="text-xl text-gray-600 mt-6">Procesando pago...</p>
              <p className="text-sm text-gray-400 mt-2">No cierre esta pantalla</p>
            </div>
          )}

          {/* Step 5: Success */}
          {step === 'success' && (
            <div className="text-center py-12">
              <div className="text-6xl mb-4">✅</div>
              <h2 className="text-3xl font-bold text-green-600">¡Listo!</h2>
              <p className="text-lg text-gray-600 mt-2">
                La máquina #{selectedMachine?.number} está iniciando
              </p>
              <p className="text-sm text-gray-400 mt-1">
                Duración: {selectedMachine?.durationMinutes} minutos
              </p>
              <button onClick={handleReset} className="mt-8 bg-indigo-600 text-white rounded-lg px-8 py-3 font-semibold hover:bg-indigo-700">
                Nueva transacción
              </button>
            </div>
          )}

          {/* Step 6: Error */}
          {step === 'error' && (
            <div className="text-center py-12">
              <div className="text-6xl mb-4">❌</div>
              <h2 className="text-2xl font-bold text-red-600">Error</h2>
              <p className="text-gray-600 mt-2">{error}</p>
              <button onClick={handleReset} className="mt-8 bg-indigo-600 text-white rounded-lg px-8 py-3 font-semibold hover:bg-indigo-700">
                Intentar de nuevo
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
