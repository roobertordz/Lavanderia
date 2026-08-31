import { useEffect, useRef, useState, useCallback } from 'react';
import toast from 'react-hot-toast';
import { systemApi } from '@/api/endpoints';

export function UpdatesPage() {
  const [version, setVersion] = useState<string>('');
  const [environment, setEnvironment] = useState<string>('');
  const [checking, setChecking] = useState(false);
  const [applying, setApplying] = useState(false);
  const [latestVersion, setLatestVersion] = useState<string | null>(null);
  const [updateAvailable, setUpdateAvailable] = useState(false);
  const [running, setRunning] = useState(false);
  const [log, setLog] = useState('');

  const pollRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const loadVersion = useCallback(async () => {
    try {
      const res = await systemApi.getVersion();
      if (res.data.success && res.data.data) {
        setVersion(res.data.data.version);
        setEnvironment(res.data.data.environment);
      }
    } catch {
      /* silent */
    }
  }, []);

  const stopPolling = useCallback(() => {
    if (pollRef.current) {
      clearInterval(pollRef.current);
      pollRef.current = null;
    }
  }, []);

  const pollStatus = useCallback(async () => {
    try {
      const res = await systemApi.getUpdateStatus();
      if (res.data.success && res.data.data) {
        setRunning(res.data.data.running);
        setLog(res.data.data.log);
        if (!res.data.data.running) {
          stopPolling();
          setApplying(false);
          toast.success('Proceso de actualización finalizado. Verificando versión...');
          loadVersion();
        }
      }
    } catch {
      /* silent */
    }
  }, [loadVersion, stopPolling]);

  useEffect(() => {
    loadVersion();
    return () => stopPolling();
  }, [loadVersion, stopPolling]);

  const handleCheck = async () => {
    setChecking(true);
    try {
      const res = await systemApi.checkUpdate();
      if (res.data.success && res.data.data) {
        setLatestVersion(res.data.data.latestVersion);
        setUpdateAvailable(res.data.data.updateAvailable);
        toast[res.data.data.updateAvailable ? 'success' : 'success'](
          res.data.data.updateAvailable
            ? `Nueva versión disponible: ${res.data.data.latestVersion}`
            : 'El sistema ya está actualizado.'
        );
      } else {
        toast.error(res.data.error || 'No se pudo verificar actualizaciones.');
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error verificando actualizaciones.';
      toast.error(message);
    } finally {
      setChecking(false);
    }
  };

  const handleApply = async () => {
    if (!window.confirm('¿Aplicar la actualización ahora? El sistema podría quedar brevemente no disponible.')) {
      return;
    }
    setApplying(true);
    setLog('');
    try {
      const res = await systemApi.applyUpdate(latestVersion ?? undefined);
      if (res.data.success) {
        toast.success('Actualización iniciada. Esto puede tardar varios minutos...');
        setRunning(true);
        pollRef.current = setInterval(pollStatus, 4000);
      } else {
        toast.error(res.data.error || 'No se pudo iniciar la actualización.');
        setApplying(false);
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Error iniciando la actualización.';
      toast.error(message);
      setApplying(false);
    }
  };

  return (
    <div className="p-6 max-w-3xl mx-auto space-y-6">
      <h1 className="text-2xl font-bold text-gray-800">Actualizaciones del sistema</h1>

      <div className="bg-white rounded-xl shadow p-6 space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm text-gray-500">Versión actual</p>
            <p className="text-xl font-semibold text-gray-800">{version || '—'}</p>
            <p className="text-xs text-gray-400">{environment}</p>
          </div>
          <button
            onClick={handleCheck}
            disabled={checking || applying || running}
            className="px-4 py-2 rounded-lg bg-indigo-600 text-white text-sm font-medium hover:bg-indigo-700 disabled:opacity-50"
          >
            {checking ? 'Buscando...' : 'Buscar actualizaciones'}
          </button>
        </div>

        {latestVersion && (
          <div className="border-t pt-4 flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-500">Última versión disponible</p>
              <p className="text-lg font-medium text-gray-800">{latestVersion}</p>
            </div>
            {updateAvailable ? (
              <button
                onClick={handleApply}
                disabled={applying || running}
                className="px-4 py-2 rounded-lg bg-emerald-600 text-white text-sm font-medium hover:bg-emerald-700 disabled:opacity-50"
              >
                {applying || running ? 'Actualizando...' : 'Aplicar actualización'}
              </button>
            ) : (
              <span className="text-sm text-emerald-600 font-medium">✓ Actualizado</span>
            )}
          </div>
        )}
      </div>

      {(running || log) && (
        <div className="bg-gray-900 rounded-xl shadow p-4">
          <p className="text-xs text-gray-400 mb-2">
            {running ? 'Actualización en progreso...' : 'Registro de la última actualización'}
          </p>
          <pre className="text-xs text-emerald-400 whitespace-pre-wrap max-h-80 overflow-y-auto">
            {log || 'Esperando salida...'}
          </pre>
        </div>
      )}

      <p className="text-xs text-gray-400">
        El sistema realiza un respaldo de la base de datos antes de actualizar y revierte
        automáticamente a la versión anterior si la actualización falla.
      </p>
    </div>
  );
}
