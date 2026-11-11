import React, { useState, useEffect } from 'react';
import { HoldResponse, HoldStatus } from '../types/api';
import { useHoldTimer } from '../hooks/useHoldTimer';
import { apiClient, ApiClientError } from '../services/apiClient';
import { useStudent } from '../context/StudentContext';
import {
  Clock,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  CreditCard,
  Trash2,
  Loader2,
  ShieldCheck,
  User,
  Hash,
  Printer
} from 'lucide-react';

interface ActiveHoldModalProps {
  hold: HoldResponse | null;
  isOpen: boolean;
  onClose: () => void;
  onConfirmed?: (holdId: string) => void;
  onCancelled?: (holdId: string) => void;
}

export const ActiveHoldModal: React.FC<ActiveHoldModalProps> = ({
  hold,
  isOpen,
  onClose,
  onConfirmed,
  onCancelled
}) => {
  const { studentId } = useStudent();
  const [currentStatus, setCurrentStatus] = useState<HoldStatus>('HELD');
  const [isProcessingPayment, setIsProcessingPayment] = useState(false);
  const [isCancelling, setIsCancelling] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const { formattedTime, isExpired, percentRemaining } = useHoldTimer(
    hold?.expiresAt || new Date().toISOString()
  );

  // Sync internal status with hold prop
  useEffect(() => {
    if (hold) {
      setCurrentStatus(hold.status);
      setErrorMessage(null);
    }
  }, [hold]);

  // Automatic client-side transition to EXPIRED when timer hits 00:00
  useEffect(() => {
    if (isExpired && currentStatus === 'HELD') {
      setCurrentStatus('EXPIRED');
    }
  }, [isExpired, currentStatus]);

  if (!isOpen || !hold) return null;

  const handleConfirmPayment = async () => {
    try {
      setIsProcessingPayment(true);
      setErrorMessage(null);

      const targetStudentId = studentId || hold.studentId;
      await apiClient.confirmHold(hold.holdId, targetStudentId);

      setCurrentStatus('CONFIRMED');
      if (onConfirmed) {
        onConfirmed(hold.holdId);
      }
    } catch (err) {
      if (err instanceof ApiClientError) {
        setErrorMessage(err.message);
        if (err.status === 409) {
          setCurrentStatus('EXPIRED');
        }
      } else {
        setErrorMessage('Failed to confirm admission fee payment. Please try again.');
      }
    } finally {
      setIsProcessingPayment(false);
    }
  };

  const handleCancelHold = async () => {
    if (!window.confirm('Are you sure you want to cancel this seat reservation? The seat will be released immediately.')) {
      return;
    }

    try {
      setIsCancelling(true);
      setErrorMessage(null);

      const targetStudentId = studentId || hold.studentId;
      await apiClient.cancelHold(hold.holdId, targetStudentId);

      setCurrentStatus('CANCELLED');
      if (onCancelled) {
        onCancelled(hold.holdId);
      }
      setTimeout(() => {
        onClose();
      }, 600);
    } catch (err) {
      if (err instanceof ApiClientError) {
        setErrorMessage(err.message);
      } else {
        setErrorMessage('Failed to cancel hold reservation.');
      }
    } finally {
      setIsCancelling(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-navy-950/70 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl border border-navy-200 w-full max-w-lg overflow-hidden transform transition-all animate-in fade-in zoom-in-95 duration-200">
        
        {/* Header */}
        <div className="px-6 py-4 border-b border-navy-100 bg-navy-50/70 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className={`p-2.5 rounded-xl border ${
              currentStatus === 'CONFIRMED'
                ? 'bg-emerald-100 border-emerald-200 text-emerald-800'
                : currentStatus === 'EXPIRED'
                ? 'bg-rose-100 border-rose-200 text-rose-800'
                : 'bg-amber-100 border-amber-200 text-amber-800'
            }`}>
              {currentStatus === 'CONFIRMED' ? (
                <ShieldCheck className="w-5 h-5 text-emerald-600" />
              ) : currentStatus === 'EXPIRED' ? (
                <AlertTriangle className="w-5 h-5 text-rose-600" />
              ) : (
                <Clock className="w-5 h-5 text-amber-600" />
              )}
            </div>
            <div>
              <h3 className="text-base font-bold text-navy-900 leading-snug">
                {currentStatus === 'CONFIRMED'
                  ? 'Admission Confirmed'
                  : currentStatus === 'EXPIRED'
                  ? 'Seat Reservation Expired'
                  : 'Seat Held (Pending Payment)'}
              </h3>
              <p className="text-xs text-navy-500 font-mono">
                Hold ID: {hold.holdId}
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={onClose}
            className="text-navy-400 hover:text-navy-700 p-1.5 rounded-lg hover:bg-navy-100 transition-colors"
          >
            <XCircle className="w-5 h-5" />
          </button>
        </div>

        {/* Content Body */}
        <div className="p-6 space-y-5">
          {/* Error Message Banner */}
          {errorMessage && (
            <div className="p-3.5 bg-rose-50 border border-rose-200 rounded-xl text-rose-800 text-xs flex items-start space-x-2.5">
              <AlertTriangle className="w-4 h-4 shrink-0 mt-0.5 text-rose-600" />
              <div className="flex-1 font-medium">{errorMessage}</div>
            </div>
          )}

          {/* Confirmed State Celebration Card */}
          {currentStatus === 'CONFIRMED' ? (
            <div className="p-6 bg-emerald-50/70 border border-emerald-200 rounded-xl text-center space-y-3">
              <div className="w-12 h-12 bg-emerald-100 text-emerald-600 rounded-full flex items-center justify-center mx-auto shadow-2xs">
                <CheckCircle2 className="w-7 h-7 text-emerald-600" />
              </div>
              <h4 className="text-lg font-bold text-emerald-950">
                Seat Successfully Reserved & Confirmed!
              </h4>
              <p className="text-xs text-emerald-800 max-w-sm mx-auto leading-relaxed">
                The admission fee has been officially recorded. This seat is permanently allocated to student BRN{' '}
                <span className="font-bold font-mono text-emerald-950">{hold.studentId}</span> in offering{' '}
                <span className="font-semibold text-emerald-950">{hold.classId}</span>.
              </p>
              
              <div className="pt-2 flex justify-center">
                <button
                  type="button"
                  onClick={() => window.print()}
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold bg-white border border-emerald-300 text-emerald-800 hover:bg-emerald-50 shadow-2xs transition-all"
                >
                  <Printer className="w-3.5 h-3.5" />
                  <span>Print Admission Slip</span>
                </button>
              </div>
            </div>
          ) : currentStatus === 'EXPIRED' ? (
            /* Expired State Alert */
            <div className="p-6 bg-rose-50/80 border border-rose-200 rounded-xl text-center space-y-3">
              <div className="w-12 h-12 bg-rose-100 text-rose-600 rounded-full flex items-center justify-center mx-auto shadow-2xs">
                <AlertTriangle className="w-7 h-7 text-rose-600" />
              </div>
              <h4 className="text-lg font-bold text-rose-950">
                Reservation Window Expired
              </h4>
              <p className="text-xs text-rose-800 max-w-sm mx-auto leading-relaxed">
                The 10-minute hold window elapsed before fee confirmation. Under strict FCFS rules, this seat has returned to the available public pool.
              </p>
            </div>
          ) : (
            /* Active Hold Live Countdown Timer */
            <div className="p-6 bg-amber-50/60 border border-amber-200 rounded-xl text-center space-y-3">
              <span className="inline-flex items-center text-[11px] font-bold uppercase tracking-wider text-amber-900 bg-amber-100 px-3 py-0.5 rounded-full border border-amber-300/60">
                Guaranteed Reservation Window
              </span>

              <div className="space-y-1">
                <div className="text-5xl font-black text-amber-950 font-mono tracking-tight">
                  {formattedTime}
                </div>
                <p className="text-xs text-amber-800 font-medium">
                  Remaining time to complete admission fee payment
                </p>
              </div>

              {/* Countdown Progress Bar */}
              <div className="w-full h-2.5 bg-amber-200/80 rounded-full overflow-hidden shadow-inner">
                <div
                  style={{ width: `${percentRemaining}%` }}
                  className="h-full bg-amber-500 transition-all duration-1000 ease-linear"
                />
              </div>
            </div>
          )}

          {/* Institutional Hold Breakdown */}
          <div className="bg-navy-50/60 rounded-xl border border-navy-100 p-4 space-y-2.5 text-xs">
            <div className="flex items-center justify-between py-1 border-b border-navy-100/80">
              <span className="text-navy-500 flex items-center gap-1.5">
                <Hash className="w-3.5 h-3.5 text-navy-400" />
                Target Offering:
              </span>
              <span className="font-bold text-navy-900 font-mono">{hold.classId}</span>
            </div>

            <div className="flex items-center justify-between py-1 border-b border-navy-100/80">
              <span className="text-navy-500 flex items-center gap-1.5">
                <User className="w-3.5 h-3.5 text-navy-400" />
                Student BRN:
              </span>
              <span className="font-bold text-navy-900 font-mono">{hold.studentId}</span>
            </div>

            <div className="flex items-center justify-between py-1">
              <span className="text-navy-500 flex items-center gap-1.5">
                <CreditCard className="w-3.5 h-3.5 text-navy-400" />
                Admission Fee:
              </span>
              <span className="font-extrabold text-navy-900 text-sm">৳ 1,500 BDT</span>
            </div>
          </div>
        </div>

        {/* Modal Actions Footer */}
        <div className="px-6 py-4 bg-navy-50/40 border-t border-navy-100 flex items-center justify-between gap-3">
          {currentStatus === 'HELD' ? (
            <>
              <button
                type="button"
                disabled={isCancelling || isProcessingPayment}
                onClick={handleCancelHold}
                className="px-3.5 py-2 text-xs font-semibold text-rose-700 hover:text-rose-800 hover:bg-rose-50 rounded-lg transition-colors flex items-center gap-1.5 disabled:opacity-50"
              >
                {isCancelling ? (
                  <Loader2 className="w-3.5 h-3.5 animate-spin" />
                ) : (
                  <Trash2 className="w-3.5 h-3.5" />
                )}
                <span>Cancel Reservation</span>
              </button>

              <button
                type="button"
                disabled={isProcessingPayment || isCancelling}
                onClick={handleConfirmPayment}
                className="px-5 py-2.5 bg-brand-600 hover:bg-brand-700 text-white rounded-lg text-sm font-bold shadow-xs hover:shadow-sm transition-all flex items-center gap-2 disabled:opacity-50"
              >
                {isProcessingPayment ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    <span>Processing Payment...</span>
                  </>
                ) : (
                  <>
                    <CreditCard className="w-4 h-4" />
                    <span>Pay Admission Fee (৳ 1,500)</span>
                  </>
                )}
              </button>
            </>
          ) : (
            <button
              type="button"
              onClick={onClose}
              className="w-full py-2.5 px-4 bg-navy-800 hover:bg-navy-900 text-white rounded-lg text-sm font-bold shadow-xs transition-all"
            >
              Close Window
            </button>
          )}
        </div>

      </div>
    </div>
  );
};
