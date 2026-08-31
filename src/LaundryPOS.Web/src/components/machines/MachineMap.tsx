import { clsx } from 'clsx';
import type { MachineStatusSummary } from '@/types';
import { MachineStatus, MachineType } from '@/types';
import {
  machineStatusLabels,
  machineStatusColors,
  machineTypeLabels,
} from '@/utils/constants';

interface MachineMapProps {
  machines: MachineStatusSummary[];
  onMachineClick?: (machineId: string) => void;
}

export function MachineMap({ machines, onMachineClick }: MachineMapProps) {
  return (
    <div className="bg-white rounded-lg shadow p-6">
      <h2 className="text-lg font-semibold mb-4">Mapa de Máquinas</h2>
      <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-4">
        {machines.map((machine) => (
          <button
            key={machine.machineId}
            onClick={() => onMachineClick?.(machine.machineId)}
            className={clsx(
              'relative rounded-lg p-4 text-white transition-transform hover:scale-105 cursor-pointer',
              machineStatusColors[machine.status as MachineStatus]
            )}
          >
            <div className="text-center">
              <div className="text-2xl font-bold">#{machine.number}</div>
              <div className="text-xs opacity-90">
                {machineTypeLabels[machine.type as MachineType]}
              </div>
              <div className="text-xs mt-1 font-medium">
                {machineStatusLabels[machine.status as MachineStatus]}
              </div>
              {machine.remainingMinutes != null && machine.remainingMinutes > 0 && (
                <div className="text-xs mt-1 bg-black/20 rounded px-2 py-0.5">
                  {machine.remainingMinutes} min restantes
                </div>
              )}
            </div>
            {/* Communication indicator */}
            <div
              className={clsx(
                'absolute top-1 right-1 w-2 h-2 rounded-full',
                machine.communicationStatus === 0
                  ? 'bg-green-300'
                  : 'bg-red-300 animate-pulse'
              )}
            />
          </button>
        ))}
      </div>

      {/* Legend */}
      <div className="flex flex-wrap gap-3 mt-6 text-xs">
        {Object.entries(machineStatusLabels).map(([status, label]) => (
          <div key={status} className="flex items-center gap-1">
            <div
              className={clsx(
                'w-3 h-3 rounded',
                machineStatusColors[Number(status) as MachineStatus]
              )}
            />
            <span className="text-gray-600">{label}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
