import { useEffect, useState, useCallback, useRef } from 'react';
import toast from 'react-hot-toast';
import { productsApi } from '@/api/endpoints';
import { useBranchStore } from '@/stores';
import { productCategoryLabels, productCategoryIcons, stockMovementTypeLabels, formatCurrency, formatDate } from '@/utils/constants';
import type { Product, StockMovement } from '@/types';
import { ProductCategory } from '@/types';

const categoryOptions = Object.values(ProductCategory).filter((v) => typeof v === 'number') as ProductCategory[];

export function ProductsPage() {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [category, setCategory] = useState<'all' | ProductCategory>('all');
  const [search, setSearch] = useState('');
  const [importing, setImporting] = useState(false);

  const [showForm, setShowForm] = useState(false);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [sellingProduct, setSellingProduct] = useState<Product | null>(null);
  const [adjustingProduct, setAdjustingProduct] = useState<Product | null>(null);
  const [movementsProduct, setMovementsProduct] = useState<Product | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);
  const selectedBranchId = useBranchStore((s) => s.selectedBranchId);
  // Cashiers can now fully manage the product catalog (create/edit/delete + import/export).
  const canManage = true;

  const load = useCallback(async () => {
    if (!selectedBranchId) { setLoading(false); return; }
    try {
      const res = await productsApi.getByBranch(selectedBranchId);
      if (res.data.success && res.data.data) setProducts(res.data.data);
    } catch { /* silent */ }
    finally { setLoading(false); }
  }, [selectedBranchId]);

  useEffect(() => { load(); }, [load]);

  const filtered = products.filter((p) => {
    if (category !== 'all' && p.category !== category) return false;
    if (search) {
      const q = search.toLowerCase();
      return p.name.toLowerCase().includes(q) || p.brand.toLowerCase().includes(q) || (p.sku ?? '').toLowerCase().includes(q);
    }
    return true;
  });

  const counts = {
    total: products.length,
    lowStock: products.filter((p) => p.isLowStock).length,
    inventoryValue: products.reduce((sum, p) => sum + p.stockQuantity * p.purchasePrice, 0),
  };

  const handleExport = async () => {
    if (!selectedBranchId) return;
    try {
      const res = await productsApi.exportExcel(selectedBranchId);
      const url = URL.createObjectURL(new Blob([res.data as BlobPart]));
      const link = document.createElement('a');
      link.href = url;
      link.download = `productos-${new Date().toISOString().slice(0, 10)}.xlsx`;
      link.click();
      URL.revokeObjectURL(url);
    } catch {
      toast.error('Error al exportar el catálogo.');
    }
  };

  const handleImportClick = () => fileInputRef.current?.click();

  const handleImportFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file || !selectedBranchId) return;
    setImporting(true);
    try {
      const res = await productsApi.importExcel(selectedBranchId, file);
      if (res.data.success && res.data.data) {
        const { imported, failed } = res.data.data;
        toast.success(`Importación completa: ${imported} productos, ${failed} errores.`);
      }
      load();
    } catch {
      toast.error('Error al importar el archivo.');
    } finally {
      setImporting(false);
    }
  };

  const handleDelete = async (product: Product) => {
    if (!confirm(`¿Eliminar "${product.name}"?`)) return;
    try {
      await productsApi.delete(product.id);
      toast.success('Producto eliminado.');
      load();
    } catch {
      toast.error('Error al eliminar el producto.');
    }
  };

  if (loading) return <Spinner />;
  if (!selectedBranchId) return <NoSelection />;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl font-bold text-gray-900">Productos</h1>
        {canManage && (
          <div className="flex gap-2">
            <input ref={fileInputRef} type="file" accept=".xlsx,.xls" className="hidden" onChange={handleImportFile} />
            <button
              onClick={handleImportClick}
              disabled={importing}
              className="bg-white border border-gray-300 hover:bg-gray-50 text-gray-700 text-sm font-semibold px-4 py-2 rounded-lg transition disabled:opacity-50"
            >
              {importing ? 'Importando…' : '📥 Importar Excel'}
            </button>
            <button
              onClick={handleExport}
              className="bg-white border border-gray-300 hover:bg-gray-50 text-gray-700 text-sm font-semibold px-4 py-2 rounded-lg transition"
            >
              📤 Exportar Excel
            </button>
            <button
              onClick={() => { setEditingProduct(null); setShowForm(true); }}
              className="bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold px-4 py-2 rounded-lg transition"
            >
              + Nuevo Producto
            </button>
          </div>
        )}
      </div>

      {/* Summary pills */}
      <div className="flex flex-wrap gap-3">
        <Pill label="Productos" value={counts.total} color="bg-indigo-100 text-indigo-800" />
        <Pill label="Stock bajo" value={counts.lowStock} color="bg-red-100 text-red-800" />
        <span className="inline-flex items-center gap-1 px-3 py-1 rounded-full text-sm font-semibold bg-green-100 text-green-800">
          {formatCurrency(counts.inventoryValue)} en inventario
        </span>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="flex flex-wrap gap-2">
          <button
            onClick={() => setCategory('all')}
            className={`px-4 py-1.5 rounded-full text-sm font-medium transition ${
              category === 'all' ? 'bg-indigo-600 text-white' : 'bg-white text-gray-600 border border-gray-300 hover:bg-gray-50'
            }`}
          >
            Todas
          </button>
          {categoryOptions.map((c) => (
            <button
              key={c}
              onClick={() => setCategory(c)}
              className={`px-4 py-1.5 rounded-full text-sm font-medium transition ${
                category === c ? 'bg-indigo-600 text-white' : 'bg-white text-gray-600 border border-gray-300 hover:bg-gray-50'
              }`}
            >
              {productCategoryIcons[c]} {productCategoryLabels[c]}
            </button>
          ))}
        </div>
        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Buscar por nombre, marca o SKU…"
          className="ml-auto border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400 w-64"
        />
      </div>

      {/* Product grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
        {filtered.map((p) => (
          <ProductCard
            key={p.id}
            product={p}
            canManage={canManage}
            onEdit={() => { setEditingProduct(p); setShowForm(true); }}
            onDelete={() => handleDelete(p)}
            onSell={() => setSellingProduct(p)}
            onAdjustStock={() => setAdjustingProduct(p)}
            onViewMovements={() => setMovementsProduct(p)}
          />
        ))}
        {filtered.length === 0 && (
          <div className="col-span-full text-center py-12 text-gray-500">No hay productos que coincidan.</div>
        )}
      </div>

      {showForm && (
        <ProductFormModal
          product={editingProduct}
          branchId={selectedBranchId}
          onClose={() => setShowForm(false)}
          onSaved={() => { setShowForm(false); load(); }}
        />
      )}

      {sellingProduct && (
        <SellModal
          product={sellingProduct}
          onClose={() => setSellingProduct(null)}
          onSold={() => { setSellingProduct(null); load(); }}
        />
      )}

      {adjustingProduct && (
        <AdjustStockModal
          product={adjustingProduct}
          onClose={() => setAdjustingProduct(null)}
          onAdjusted={() => { setAdjustingProduct(null); load(); }}
        />
      )}

      {movementsProduct && (
        <MovementsModal product={movementsProduct} onClose={() => setMovementsProduct(null)} />
      )}
    </div>
  );
}

// ─── Product Card ───────────────────────────────────────────────────────────
function ProductCard({
  product: p,
  canManage,
  onEdit,
  onDelete,
  onSell,
  onAdjustStock,
  onViewMovements,
}: {
  product: Product;
  canManage: boolean;
  onEdit: () => void;
  onDelete: () => void;
  onSell: () => void;
  onAdjustStock: () => void;
  onViewMovements: () => void;
}) {
  return (
    <div className={`bg-white rounded-xl shadow p-5 border-l-4 ${p.isLowStock ? 'border-red-500' : 'border-green-500'}`}>
      <div className="flex items-start justify-between mb-3">
        <div>
          <p className="text-xs text-gray-400 font-medium">{p.sku ?? 'Sin SKU'}</p>
          <p className="font-bold text-gray-900">{p.name}</p>
          <p className="text-xs text-gray-500">{p.brand} · {p.presentation}</p>
        </div>
        <span className="text-2xl">{productCategoryIcons[p.category]}</span>
      </div>

      <div className="space-y-1 mb-4">
        <div className="flex items-center justify-between text-xs">
          <span className="text-gray-500">Categoría</span>
          <span className="font-medium text-gray-700">{productCategoryLabels[p.category]}</span>
        </div>
        <div className="flex items-center justify-between text-xs">
          <span className="text-gray-500">Precio venta</span>
          <span className="font-medium text-gray-700">{formatCurrency(p.salePrice)}</span>
        </div>
        <div className="flex items-center justify-between text-xs">
          <span className="text-gray-500">Stock</span>
          <span className={`px-2 py-0.5 rounded-full font-medium ${p.isLowStock ? 'bg-red-100 text-red-800' : 'bg-green-100 text-green-800'}`}>
            {p.stockQuantity} {p.isLowStock ? '⚠ Bajo' : ''}
          </span>
        </div>
      </div>

      <div className={`grid ${canManage ? 'grid-cols-2' : 'grid-cols-1'} gap-2 mb-2`}>
        <button
          onClick={onSell}
          disabled={p.stockQuantity === 0}
          className="bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-semibold py-2 rounded-lg transition disabled:opacity-40 disabled:cursor-not-allowed"
        >
          Vender
        </button>
        {canManage && (
          <button
            onClick={onAdjustStock}
            className="bg-gray-100 hover:bg-gray-200 text-gray-700 text-xs font-semibold py-2 rounded-lg transition"
          >
            Ajustar stock
          </button>
        )}
      </div>
      <div className={`grid ${canManage ? 'grid-cols-3' : 'grid-cols-1'} gap-2 text-xs`}>
        <button onClick={onViewMovements} className="text-indigo-600 hover:underline">Movimientos</button>
        {canManage && <button onClick={onEdit} className="text-gray-600 hover:underline">Editar</button>}
        {canManage && <button onClick={onDelete} className="text-red-600 hover:underline">Eliminar</button>}
      </div>
    </div>
  );
}

// ─── Create / Edit Modal ────────────────────────────────────────────────────
function ProductFormModal({
  product,
  branchId,
  onClose,
  onSaved,
}: {
  product: Product | null;
  branchId: string;
  onClose: () => void;
  onSaved: () => void;
}) {
  const isEdit = !!product;
  const [form, setForm] = useState({
    name: product?.name ?? '',
    brand: product?.brand ?? '',
    category: product?.category ?? ProductCategory.Detergent,
    presentation: product?.presentation ?? '',
    sku: product?.sku ?? '',
    barcode: product?.barcode ?? '',
    purchasePrice: product?.purchasePrice ?? 0,
    salePrice: product?.salePrice ?? 0,
    stockQuantity: product?.stockQuantity ?? 0,
    minStockThreshold: product?.minStockThreshold ?? 5,
    notes: product?.notes ?? '',
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async () => {
    if (!form.name || !form.brand || !form.presentation) {
      setError('Completa los campos requeridos.');
      return;
    }
    setSaving(true);
    setError('');
    try {
      if (isEdit && product) {
        await productsApi.update(product.id, form);
      } else {
        await productsApi.create({ ...form, branchId });
      }
      toast.success(isEdit ? 'Producto actualizado.' : 'Producto creado.');
      onSaved();
    } catch {
      setError('Error al guardar. Intenta de nuevo.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg p-6 space-y-4 max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-bold text-gray-900">{isEdit ? 'Editar producto' : 'Nuevo producto'}</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl">✕</button>
        </div>

        <div className="grid grid-cols-2 gap-3">
          <Field label="Nombre *" span2>
            <input type="text" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} className={inputCls} />
          </Field>
          <Field label="Marca *">
            <input type="text" value={form.brand} onChange={(e) => setForm({ ...form, brand: e.target.value })} className={inputCls} />
          </Field>
          <Field label="Presentación *">
            <input type="text" value={form.presentation} onChange={(e) => setForm({ ...form, presentation: e.target.value })} placeholder="Ej: 1L" className={inputCls} />
          </Field>
          <Field label="Categoría">
            <select value={form.category} onChange={(e) => setForm({ ...form, category: Number(e.target.value) })} className={inputCls}>
              {categoryOptions.map((c) => (
                <option key={c} value={c}>{productCategoryLabels[c]}</option>
              ))}
            </select>
          </Field>
          <Field label="SKU">
            <input type="text" value={form.sku} onChange={(e) => setForm({ ...form, sku: e.target.value })} className={inputCls} />
          </Field>
          <Field label="Código de barras">
            <input type="text" value={form.barcode} onChange={(e) => setForm({ ...form, barcode: e.target.value })} className={inputCls} />
          </Field>
          <Field label="Precio compra">
            <input type="number" step="0.01" min="0" value={form.purchasePrice} onChange={(e) => setForm({ ...form, purchasePrice: Number(e.target.value) })} className={inputCls} />
          </Field>
          <Field label="Precio venta *">
            <input type="number" step="0.01" min="0" value={form.salePrice} onChange={(e) => setForm({ ...form, salePrice: Number(e.target.value) })} className={inputCls} />
          </Field>
          {!isEdit && (
            <Field label="Stock inicial">
              <input type="number" min="0" value={form.stockQuantity} onChange={(e) => setForm({ ...form, stockQuantity: Number(e.target.value) })} className={inputCls} />
            </Field>
          )}
          <Field label="Stock mínimo">
            <input type="number" min="0" value={form.minStockThreshold} onChange={(e) => setForm({ ...form, minStockThreshold: Number(e.target.value) })} className={inputCls} />
          </Field>
          <Field label="Notas" span2>
            <textarea value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} rows={2} className={inputCls} />
          </Field>
        </div>

        {error && <p className="text-sm text-red-600">{error}</p>}

        <div className="flex justify-end gap-2 pt-2">
          <button onClick={onClose} className="px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-100 rounded-lg">Cancelar</button>
          <button
            onClick={handleSubmit}
            disabled={saving}
            className="px-4 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg disabled:opacity-50"
          >
            {saving ? 'Guardando…' : 'Guardar'}
          </button>
        </div>
      </div>
    </div>
  );
}

// ─── Sell Modal ─────────────────────────────────────────────────────────────
function SellModal({ product, onClose, onSold }: { product: Product; onClose: () => void; onSold: () => void }) {
  const [quantity, setQuantity] = useState(1);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async () => {
    if (quantity <= 0 || quantity > product.stockQuantity) {
      setError('Cantidad inválida.');
      return;
    }
    setSaving(true);
    try {
      await productsApi.sell(product.id, quantity);
      toast.success('Venta registrada.');
      onSold();
    } catch {
      setError('Error al registrar la venta.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-sm p-6 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-bold text-gray-900">Vender: {product.name}</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl">✕</button>
        </div>
        <p className="text-sm text-gray-500">Stock disponible: {product.stockQuantity}</p>
        <Field label="Cantidad">
          <input type="number" min={1} max={product.stockQuantity} value={quantity} onChange={(e) => setQuantity(Number(e.target.value))} className={inputCls} />
        </Field>
        <p className="text-sm font-semibold text-gray-700">Total: {formatCurrency(quantity * product.salePrice)}</p>
        {error && <p className="text-sm text-red-600">{error}</p>}
        <div className="flex justify-end gap-2 pt-2">
          <button onClick={onClose} className="px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-100 rounded-lg">Cancelar</button>
          <button onClick={handleSubmit} disabled={saving} className="px-4 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg disabled:opacity-50">
            {saving ? 'Procesando…' : 'Confirmar venta'}
          </button>
        </div>
      </div>
    </div>
  );
}

// ─── Adjust Stock Modal ─────────────────────────────────────────────────────
function AdjustStockModal({ product, onClose, onAdjusted }: { product: Product; onClose: () => void; onAdjusted: () => void }) {
  const [quantity, setQuantity] = useState(0);
  const [reason, setReason] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async () => {
    if (quantity === 0 || !reason.trim()) {
      setError('Indica una cantidad distinta de cero y una razón.');
      return;
    }
    if (product.stockQuantity + quantity < 0) {
      setError('El stock resultante no puede ser negativo.');
      return;
    }
    setSaving(true);
    try {
      await productsApi.adjustStock(product.id, quantity, reason);
      toast.success('Stock ajustado.');
      onAdjusted();
    } catch {
      setError('Error al ajustar el stock.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-sm p-6 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-bold text-gray-900">Ajustar stock: {product.name}</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl">✕</button>
        </div>
        <p className="text-sm text-gray-500">Stock actual: {product.stockQuantity}</p>
        <Field label="Cantidad (+ agregar / − quitar)">
          <input type="number" value={quantity} onChange={(e) => setQuantity(Number(e.target.value))} className={inputCls} />
        </Field>
        <Field label="Razón *">
          <input type="text" value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Ej: Merma, conteo físico, compra…" className={inputCls} />
        </Field>
        {error && <p className="text-sm text-red-600">{error}</p>}
        <div className="flex justify-end gap-2 pt-2">
          <button onClick={onClose} className="px-4 py-2 text-sm font-medium text-gray-600 hover:bg-gray-100 rounded-lg">Cancelar</button>
          <button onClick={handleSubmit} disabled={saving} className="px-4 py-2 text-sm font-semibold text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg disabled:opacity-50">
            {saving ? 'Guardando…' : 'Aplicar ajuste'}
          </button>
        </div>
      </div>
    </div>
  );
}

// ─── Movements Modal ────────────────────────────────────────────────────────
function MovementsModal({ product, onClose }: { product: Product; onClose: () => void }) {
  const [movements, setMovements] = useState<StockMovement[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    productsApi.getMovements(product.id)
      .then((res) => { if (res.data.success && res.data.data) setMovements(res.data.data); })
      .finally(() => setLoading(false));
  }, [product.id]);

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-2xl p-6 space-y-4 max-h-[85vh] overflow-y-auto">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-bold text-gray-900">Movimientos: {product.name}</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl">✕</button>
        </div>

        {loading ? (
          <Spinner />
        ) : movements.length === 0 ? (
          <p className="text-sm text-gray-500 text-center py-8">Sin movimientos registrados.</p>
        ) : (
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs text-gray-500 border-b">
                <th className="py-2">Fecha</th>
                <th className="py-2">Tipo</th>
                <th className="py-2 text-right">Cantidad</th>
                <th className="py-2 text-right">Stock resultante</th>
                <th className="py-2">Razón</th>
                <th className="py-2">Usuario</th>
              </tr>
            </thead>
            <tbody>
              {movements.map((m) => (
                <tr key={m.id} className="border-b last:border-0">
                  <td className="py-2 whitespace-nowrap">{formatDate(m.createdAt)}</td>
                  <td className="py-2">{stockMovementTypeLabels[m.type]}</td>
                  <td className={`py-2 text-right font-medium ${m.quantity < 0 ? 'text-red-600' : 'text-green-600'}`}>
                    {m.quantity > 0 ? '+' : ''}{m.quantity}
                  </td>
                  <td className="py-2 text-right">{m.newStock}</td>
                  <td className="py-2 text-gray-500">{m.reason ?? '—'}</td>
                  <td className="py-2 text-gray-500">{m.userName ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

// ─── Shared bits ────────────────────────────────────────────────────────────
const inputCls = 'w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-400';

function Field({ label, span2, children }: { label: string; span2?: boolean; children: React.ReactNode }) {
  return (
    <div className={span2 ? 'col-span-2' : ''}>
      <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
      {children}
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
    <div className="text-center py-12 text-gray-500">Selecciona una sucursal para ver los productos.</div>
  );
}
