import React, { useState } from 'react';
import { StudentHoldItem } from '../types/api';
import { useHoldTimer } from '../hooks/useHoldTimer';
import {
  CheckCircle2,
  Clock,
  XCircle,
  CreditCard,
  Ban,
  Loader2,
  Calendar,
  Layers
} from 'lucide-react';

interface ApplicationCardProps {
  application: StudentHoldItem;
  onConfirm: (holdId: string) => Promise<void>;
  onCancel: (holdId: string) => Promise<void>;
}

export const ApplicationCard: React.FC<ApplicationCardProps> = ({
  application,
  onConfirm,
  onCancel
}) => {
  const [isProcessing, setIsProcessing] = useState(false);
  const [actionType, setActionType] = useState<'confirm' | 'cancel' | null>(null);

  const { formattedTime, isExpired } = useHoldTimer(
    application.status === 'HELD' ? application.expiresAt : null
  );

  // Dynamic status evaluation: if HELD but timer hit 00:00, show as EXPIRED
  const effectiveStatus =
    application.status === 'HELD' && isExpired ? 'EXPIRED' : application.status;

  const handleConfirm = async () => {
    setIsProcessing(true);
    setActionType('confirm');
    try {
      await onConfirm(application.holdId);
    } finally {
      setIsProcessing(false);
      setActionType(null);
    }
  };

  const handleCancel = async () => {
    if (!window.confirm('Are you sure you want to cancel this reservation? The seat will be released immediately.')) {
      return;
    }
    setIsProcessing(true);
    setActionType('cancel');
    try {
      await onCancel(application.holdId);
    } finally {
      setIsProcessing(false);
      setActionType(null);
    }
  };

  return (
    <div
      className={`rounded-2xl border transition-all duration-200 bg-white shadow-sm hover:shadow-md p-5 sm:p-6 ${
        effectiveStatus === 'CONFIRMED'
          ? 'border-emerald-200 bg-gradient-to-br from-white to-emerald-50/30'
          : effectiveStatus === 'HELD'
          ? 'border-amber-300 ring-2 ring-amber-100 bg-gradient-to-br from-white to-amber-50/20'
          : effectiveStatus === 'EXPIRED'
          ? 'border-red-200 opacity-85'
          : 'border-slate-200 opacity-75'
      }`}
    >
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        {/* Class and Hold Meta */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1.5 flex-wrap">
            <span
              className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold uppercase tracking-wider ${
                effectiveStatus === 'CONFIRMED'
                  ? 'bg-emerald-100 text-emerald-800'
                  : effectiveStatus === 'HELD'
                  ? 'bg-amber-100 text-amber-800 animate-pulse'
                  : effectiveStatus === 'EXPIRED'
                  ? 'bg-red-100 text-red-800'
                  : 'bg-slate-100 text-slate-700'
              }`}
            >
              {effectiveStatus === 'CONFIRMED' && <CheckCircle2 className="w-3.5 h-3.5" />}
              {effectiveStatus === 'HELD' && <Clock className="w-3.5 h-3.5" />}
              {effectiveStatus === 'EXPIRED' && <XCircle className="w-3.5 h-3.5" />}
              {effectiveStatus === 'CANCELLED' && <Ban className="w-3.5 h-3.5" />}
              {effectiveStatus === 'CONFIRMED'
                ? 'CONFIRMED / ভর্তি নিশ্চিত'
                : effectiveStatus === 'HELD'
                ? 'ACTIVE HOLD / রিজার্ভড'
                : effectiveStatus === 'EXPIRED'
                ? 'EXPIRED / মেয়াদোত্তীর্ণ'
                : 'CANCELLED / বাতিল'}
            </span>

            <span className="text-xs text-slate-400 font-mono">
              Hold ID: {application.holdId.substring(0, 8)}...
            </span>
          </div>

          <h3 className="text-lg font-bold text-slate-900 truncate">
            {application.className || 'Admission Class'}
          </h3>

          <div className="mt-2 flex items-center gap-4 text-xs text-slate-500">
            <span className="flex items-center gap-1">
              <Layers className="w-3.5 h-3.5 text-slate-400" />
              Class ID: {application.classId.substring(0, 8)}...
            </span>
            <span className="flex items-center gap-1">
              <Calendar className="w-3.5 h-3.5 text-slate-400" />
              Expires:{' '}
              {new Date(application.expiresAt).toLocaleTimeString([], {
                hour: '2-digit',
                minute: '2-digit',
                second: '2-digit'
              })}
            </span>
          </div>
        </div>

        {/* Live Timer or State Indicator */}
        {effectiveStatus === 'HELD' && (
          <div className="shrink-0 flex items-center gap-3 bg-amber-50 border border-amber-200 rounded-xl px-4 py-2.5">
            <Clock className="w-5 h-5 text-amber-600 animate-spin" style={{ animationDuration: '4s' }} />
            <div>
              <div className="text-[10px] uppercase font-semibold text-amber-700 tracking-wider">
                Time Remaining
              </div>
              <div className="text-xl font-black font-mono text-amber-900 tracking-tight">
                {formattedTime}
              </div>
            </div>
          </div>
        )}

        {effectiveStatus === 'CONFIRMED' && (
          <div className="shrink-0 flex items-center gap-2 bg-emerald-50 border border-emerald-200 text-emerald-800 rounded-xl px-4 py-2 text-sm font-semibold">
            <CheckCircle2 className="w-5 h-5 text-emerald-600" />
            <span>Admission Guaranteed</span>
          </div>
        )}
      </div>

      {/* Action Row for Active Holds */}
      {effectiveStatus === 'HELD' && (
        <div className="mt-5 pt-4 border-t border-amber-100 flex flex-wrap items-center justify-end gap-3">
          <button
            type="button"
            onClick={handleCancel}
            disabled={isProcessing}
            className="px-4 py-2 rounded-xl text-xs sm:text-sm font-medium border border-slate-300 text-slate-600 hover:bg-slate-100 active:bg-slate-200 disabled:opacity-50 transition inline-flex items-center gap-1.5"
          >
            {isProcessing && actionType === 'cancel' ? (
              <Loader2 className="w-4 h-4 animate-spin text-slate-500" />
            ) : (
              <Ban className="w-4 h-4 text-slate-400" />
            )}
            Cancel Reservation / বাতিল
          </button>

          <button
            type="button"
            onClick={handleConfirm}
            disabled={isProcessing}
            className="px-5 py-2 rounded-xl text-xs sm:text-sm font-semibold bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white shadow-sm hover:shadow transition disabled:opacity-50 inline-flex items-center gap-2"
          >
            {isProcessing && actionType === 'confirm' ? (
              <Loader2 className="w-4 h-4 animate-spin text-white" />
            ) : (
              <CreditCard className="w-4 h-4 text-white" />
            )}
            Pay Admission Fee / ভর্তি ফি দিন
          </button>
        </div>
      )}

      {effectiveStatus === 'EXPIRED' && (
        <div className="mt-4 pt-3 border-t border-slate-100 text-xs text-red-600 flex items-center gap-1.5">
          <XCircle className="w-4 h-4 shrink-0" />
          This seat reservation has expired. Please return to Available Classes to reserve a new seat.
        </div>
      )}
    </div>
  );
};
