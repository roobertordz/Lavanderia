import { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { transactionsApi } from '@/api/endpoints';
import { useBranchStore } from '@/stores';
import { formatCurrency, formatDate, transactionStatusLabels } from '@/utils/constants';
import type { Transaction } from '@/types';
import { TransactionStatus, PaymentMethod } from '@/types';

const paymentMethodLabels: Record<PaymentMethod, string> = {
  [PaymentMethod.Cash]:           '💵 Efectivo',
  [PaymentMethod.CreditCard]:     '💳 T. Crédito',
  [PaymentMethod.DebitCard]:      '💳 T. Débito',
  [PaymentMethod.DigitalWallet]:  '📱 Billetera',
  [PaymentMethod.BankTransfer]:   '🏦 Transferencia',
};

const statusBadge: Record<TransactionStatus, string> = {
  [TransactionStatus.Created]:           'bg-gray-100 text-gray-700',
  [TransactionStatus.PaymentPending]:    'bg-yellow-100 text-yellow-700',
  [TransactionStatus.PaymentAuthorized]: 'bg-blue-100 text-blue-700',
  [TransactionStatus.MachineStarting]:   'bg-indigo-100 text-indigo-700',
  [TransactionStatus.InProgress]:        'bg-blue-100 text-blue-800',
  [TransactionStatus.Completed]:         'bg-green-100 text-green-800',
  [TransactionStatus.Failed]:            'bg-red-100 text-red-700',
  [TransactionStatus.Cancelled]:         'bg-gray-200 text-gray-600',
  [TransactionStatus.Refunded]:          'bg-purple-100 text-purple-700',
};

function toISODate(d: Date) {
  return d.toISOString().split('T')[0];
}

export function TransactionsPage() {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState(true);
  const [fromDate, setFromDate] = useState(() => toISODate(new Date(Date.now() - 7 * 86400000)));
  const [toDate, setToDate]   = useState(() => toISODate(new Date()));
  const selectedBranchId = useBranchStore((s) => s.selectedBranchId);
  const navigate = useNavigate();

  const load = useCallback(async () => {
    if (!selectedBranchId) { setLoading(false); return; }
    setLoading(true);
    try {
      const res = await transactionsApi.getByBranch(
        selectedBranchId,
        new Date(fromDate).toISOString(),
        new Date(toDate + 'T23:59:59').toISOString(),
      );
      if (res.data.success && res.data.data) setTransactions(res.data.data);
    } catch { /* silent */ }
    finally { setLoading(false); }
  }, [selectedBranchId, fromDate, toDate]);

  useEffect(() => { load(); }, [load]);

  const totalRevenue = transactions
    .filter((t) => t.status === TransactionStatus.Completed)
    .reduce((s, t) => s + t.totalAmount, 0);

  if (!selectedBranchId) return <NoSelection />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Transacciones</h1>
        <button
          onClick={() => navigate('/cobro')}
          className="bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold px-4 py-2 rounded-lg transition"
        >
          + Nueva transacción
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
        <div className="ml-auto text-right">
          <p className="text-xs text-gray-400">Total periodo</p>
          <p className="text-xl font-bold text-green-600">{formatCurrency(totalRevenue)}</p>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl shadow overflow-hidden">
        {loading ? (
          <div className="flex items-center justify-center py-16">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600" />
          </div>
        ) : transactions.length === 0 ? (
          <div className="text-center py-16 text-gray-400">Sin transacciones en el periodo seleccionado.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-100">
              <thead className="bg-gray-50">
                <tr>
                  {['#', 'Máquina', 'Fecha', 'Método pago', 'Monto', 'Estado'].map((h) => (
                    <th key={h} className="px-4 py-3 text-left text-xs font-semibold text-gray-500 uppercase tracking-wide">
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {transactions.map((t) => (
                  <tr key={t.id} className="hover:bg-gray-50 transition">
                    <td className="px-4 py-3 text-sm font-mono text-gray-600">{t.transactionNumber}</td>
                    <td className="px-4 py-3 text-sm font-medium text-gray-800">
                      <span>#{t.machineNumber} {t.machineName}</span>
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-500">{formatDate(t.transactionDate)}</td>
                    <td className="px-4 py-3 text-sm text-gray-600">{paymentMethodLabels[t.paymentMethod]}</td>
                    <td className="px-4 py-3 text-sm font-bold text-gray-900">{formatCurrency(t.totalAmount)}</td>
                    <td className="px-4 py-3">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${statusBadge[t.status]}`}>
                        {transactionStatusLabels[t.status]}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

function NoSelection() {
  return (
    <div className="text-center py-12 text-gray-500">Selecciona una sucursal para ver las transacciones.</div>
  );
}
