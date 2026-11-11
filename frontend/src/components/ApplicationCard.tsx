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
  Layers,
  Printer,
  ShieldCheck
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
      className={`rounded-xl border transition-all duration-200 bg-white shadow-xs hover:shadow-sm p-5 sm:p-6 ${
        effectiveStatus === 'CONFIRMED'
          ? 'border-emerald-300 bg-gradient-to-br from-white to-emerald-50/20'
          : effectiveStatus === 'HELD'
          ? 'border-amber-300 ring-2 ring-amber-100 bg-gradient-to-br from-white to-amber-50/20'
          : effectiveStatus === 'EXPIRED'
          ? 'border-navy-200 opacity-80'
          : 'border-navy-200 opacity-70'
      }`}
    >
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        {/* Class and Hold Meta */}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-2 flex-wrap">
            <span
              className={`inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wider ${
                effectiveStatus === 'CONFIRMED'
                  ? 'bg-emerald-100 text-emerald-800'
                  : effectiveStatus === 'HELD'
                  ? 'bg-amber-100 text-amber-900 animate-pulse'
                  : effectiveStatus === 'EXPIRED'
                  ? 'bg-rose-100 text-rose-800'
                  : 'bg-navy-100 text-navy-700'
              }`}
            >
              {effectiveStatus === 'CONFIRMED' && <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" />}
              {effectiveStatus === 'HELD' && <Clock className="w-3.5 h-3.5 text-amber-600" />}
              {effectiveStatus === 'EXPIRED' && <XCircle className="w-3.5 h-3.5 text-rose-600" />}
              {effectiveStatus === 'CANCELLED' && <Ban className="w-3.5 h-3.5 text-navy-500" />}
              {effectiveStatus === 'CONFIRMED'
                ? 'CONFIRMED / ভর্তি নিশ্চিত'
                : effectiveStatus === 'HELD'
                ? 'ACTIVE HOLD / রিজার্ভড'
                : effectiveStatus === 'EXPIRED'
                ? 'EXPIRED / মেয়াদোত্তীর্ণ'
                : 'CANCELLED / বাতিল'}
            </span>

            <span className="text-xs text-navy-400 font-mono">
              Hold ID: {application.holdId.substring(0, 8)}...
            </span>
          </div>

          <h3 className="text-lg font-bold text-navy-900 truncate">
            {application.className || 'Admission Class'}
          </h3>

          <div className="mt-2 flex flex-wrap items-center gap-4 text-xs text-navy-500">
            <span className="flex items-center gap-1">
              <Layers className="w-3.5 h-3.5 text-navy-400" />
              Offering ID: {application.classId}
            </span>
            <span className="flex items-center gap-1">
              <Calendar className="w-3.5 h-3.5 text-navy-400" />
              Reservation Window:{' '}
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
            <Clock className="w-5 h-5 text-amber-600" />
            <div>
              <div className="text-[10px] uppercase font-bold text-amber-800 tracking-wider">
                Hold Remaining
              </div>
              <div className="text-xl font-black font-mono text-amber-950 tracking-tight">
                {formattedTime}
              </div>
            </div>
          </div>
        )}

        {effectiveStatus === 'CONFIRMED' && (
          <div className="shrink-0 flex flex-col sm:items-end gap-1.5">
            <div className="inline-flex items-center gap-1.5 bg-emerald-50 border border-emerald-200 text-emerald-800 rounded-lg px-3 py-1.5 text-xs font-bold">
              <ShieldCheck className="w-4 h-4 text-emerald-600" />
              <span>Admission Officially Guaranteed</span>
            </div>

            <button
              type="button"
              onClick={() => window.print()}
              className="inline-flex items-center gap-1.5 px-3 py-1 text-xs font-medium text-navy-600 hover:text-navy-900 hover:bg-navy-50 rounded border border-navy-200 transition-colors"
            >
              <Printer className="w-3 h-3" />
              <span>Print Admission Slip</span>
            </button>
          </div>
        )}
      </div>

      {/* Action Row for Active Holds */}
      {effectiveStatus === 'HELD' && (
        <div className="mt-4 pt-4 border-t border-amber-100 flex flex-wrap items-center justify-between gap-3">
          <div className="text-xs text-amber-800">
            Fee Amount: <strong className="font-bold text-navy-900">৳ 1,500 BDT</strong>
          </div>

          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={handleCancel}
              disabled={isProcessing}
              className="px-3.5 py-1.5 rounded-lg text-xs font-semibold border border-navy-300 text-navy-600 hover:bg-navy-50 disabled:opacity-50 transition inline-flex items-center gap-1.5"
            >
              {isProcessing && actionType === 'cancel' ? (
                <Loader2 className="w-3.5 h-3.5 animate-spin" />
              ) : (
                <Ban className="w-3.5 h-3.5" />
              )}
              Cancel Reservation
            </button>

            <button
              type="button"
              onClick={handleConfirm}
              disabled={isProcessing}
              className="px-4 py-1.5 rounded-lg text-xs font-bold bg-brand-600 hover:bg-brand-700 active:bg-brand-800 text-white shadow-xs transition disabled:opacity-50 inline-flex items-center gap-1.5"
            >
              {isProcessing && actionType === 'confirm' ? (
                <Loader2 className="w-3.5 h-3.5 animate-spin" />
              ) : (
                <CreditCard className="w-3.5 h-3.5" />
              )}
              Pay Admission Fee (৳ 1,500)
            </button>
          </div>
        </div>
      )}

      {effectiveStatus === 'EXPIRED' && (
        <div className="mt-3.5 pt-3 border-t border-navy-100 text-xs text-rose-700 flex items-center gap-1.5">
          <XCircle className="w-3.5 h-3.5 shrink-0" />
          <span>This seat reservation elapsed before fee payment. Please return to Class Catalog to apply for an available seat.</span>
        </div>
      )}
    </div>
  );
};
