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
  Hash
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
      }, 800);
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
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl border border-slate-200 w-full max-w-lg overflow-hidden transform transition-all animate-in fade-in zoom-in-95 duration-200">
        {/* Header */}
        <div className="px-6 py-4 border-b border-slate-100 bg-slate-50/60 flex items-center justify-between">
          <div className="flex items-center space-x-2.5">
            <div className={`p-2 rounded-lg ${
              currentStatus === 'CONFIRMED'
                ? 'bg-emerald-100 text-emerald-700'
                : currentStatus === 'EXPIRED'
                ? 'bg-rose-100 text-rose-700'
                : 'bg-amber-100 text-amber-700'
            }`}>
              {currentStatus === 'CONFIRMED' ? (
                <ShieldCheck className="w-5 h-5" />
              ) : currentStatus === 'EXPIRED' ? (
                <AlertTriangle className="w-5 h-5" />
              ) : (
                <Clock className="w-5 h-5" />
              )}
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">
                {currentStatus === 'CONFIRMED'
                  ? 'Admission Confirmed'
                  : currentStatus === 'EXPIRED'
                  ? 'Seat Reservation Expired'
                  : 'Seat Held (Pending Payment)'}
              </h3>
              <p className="text-xs text-slate-500 font-mono">
                Hold ID: {hold.holdId}
              </p>
            </div>
          </div>

          <button
            type="button"
            onClick={onClose}
            className="text-slate-400 hover:text-slate-600 p-1.5 rounded-lg hover:bg-slate-100 transition-colors"
          >
            <XCircle className="w-5 h-5" />
          </button>
        </div>

        {/* Content Body */}
        <div className="p-6 space-y-6">
          {/* Error Message Banner */}
          {errorMessage && (
            <div className="p-3.5 bg-rose-50 border border-rose-200 rounded-xl text-rose-800 text-xs flex items-start space-x-2">
              <AlertTriangle className="w-4 h-4 shrink-0 mt-0.5 text-rose-600" />
              <div className="flex-1 font-medium">{errorMessage}</div>
            </div>
          )}

          {/* Confirmed State Celebration Card */}
          {currentStatus === 'CONFIRMED' ? (
            <div className="p-6 bg-emerald-50/70 border border-emerald-200 rounded-2xl text-center space-y-3">
              <div className="w-12 h-12 bg-emerald-100 text-emerald-600 rounded-full flex items-center justify-center mx-auto">
                <CheckCircle2 className="w-7 h-7" />
              </div>
              <h4 className="text-lg font-bold text-emerald-950">
                Seat Successfully Reserved & Confirmed!
              </h4>
              <p className="text-xs text-emerald-800 max-w-sm mx-auto leading-relaxed">
                The admission fee has been recorded. This seat is now permanently allocated to this student ID in class{' '}
                <span className="font-semibold">{hold.classId}</span>.
              </p>
            </div>
          ) : currentStatus === 'EXPIRED' ? (
            /* Expired State Alert */
            <div className="p-6 bg-rose-50/80 border border-rose-200 rounded-2xl text-center space-y-3">
              <div className="w-12 h-12 bg-rose-100 text-rose-600 rounded-full flex items-center justify-center mx-auto">
                <AlertTriangle className="w-7 h-7" />
              </div>
              <h4 className="text-lg font-bold text-rose-950">
                Reservation Window Expired
              </h4>
              <p className="text-xs text-rose-800 max-w-sm mx-auto leading-relaxed">
                The 10-minute hold window elapsed before fee confirmation. This seat has returned to the available pool. You may apply again if seats remain available.
              </p>
            </div>
          ) : (
            /* Active Hold Live Countdown Timer */
            <div className="p-6 bg-amber-50/50 border border-amber-200 rounded-2xl text-center space-y-4">
              <span className="inline-flex items-center text-xs font-semibold uppercase tracking-wider text-amber-800 bg-amber-100 px-2.5 py-0.5 rounded-full">
                Active Hold Window
              </span>

              <div className="space-y-1">
                <div className="text-5xl font-extrabold text-amber-950 font-mono tracking-tight animate-pulse">
                  {formattedTime}
                </div>
                <p className="text-xs text-amber-800">
                  Minutes remaining to complete admission payment
                </p>
              </div>

              {/* Countdown Progress Bar */}
              <div className="w-full h-2 bg-amber-100 rounded-full overflow-hidden">
                <div
                  style={{ width: `${percentRemaining}%` }}
                  className="h-full bg-amber-500 transition-all duration-1000 ease-linear"
                />
              </div>
            </div>
          )}

          {/* Hold Details Grid */}
          <div className="grid grid-cols-2 gap-3 text-xs">
            <div className="p-3 bg-slate-50 rounded-xl border border-slate-100">
              <div className="flex items-center space-x-1.5 text-slate-500 mb-1">
                <Hash className="w-3.5 h-3.5" />
                <span>Class Identifier</span>
              </div>
              <p className="font-semibold text-slate-800 font-mono">{hold.classId}</p>
            </div>

            <div className="p-3 bg-slate-50 rounded-xl border border-slate-100">
              <div className="flex items-center space-x-1.5 text-slate-500 mb-1">
                <User className="w-3.5 h-3.5" />
                <span>Student ID</span>
              </div>
              <p className="font-semibold text-slate-800 font-mono truncate" title={hold.studentId}>
                {hold.studentId}
              </p>
            </div>
          </div>
        </div>

        {/* Footer Actions */}
        <div className="px-6 py-4 bg-slate-50/80 border-t border-slate-100 flex items-center justify-between">
          {currentStatus === 'CONFIRMED' || currentStatus === 'EXPIRED' ? (
            <button
              type="button"
              onClick={onClose}
              className="w-full py-2.5 px-4 bg-slate-900 hover:bg-slate-800 text-white font-semibold text-sm rounded-xl shadow-sm transition-colors text-center"
            >
              Close
            </button>
          ) : (
            <>
              <button
                type="button"
                onClick={handleCancelHold}
                disabled={isCancelling || isProcessingPayment}
                className="py-2.5 px-4 text-rose-600 hover:bg-rose-50 text-xs font-semibold rounded-xl border border-rose-200 transition-colors flex items-center space-x-1.5 disabled:opacity-50"
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
                onClick={handleConfirmPayment}
                disabled={isProcessingPayment || isCancelling || isExpired}
                className="py-2.5 px-5 bg-indigo-600 hover:bg-indigo-700 active:bg-indigo-800 text-white font-semibold text-sm rounded-xl shadow-sm hover:shadow transition-all flex items-center space-x-2 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isProcessingPayment ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    <span>Processing Payment...</span>
                  </>
                ) : (
                  <>
                    <CreditCard className="w-4 h-4" />
                    <span>Pay Admission Fee</span>
                  </>
                )}
              </button>
            </>
          )}
        </div>
      </div>
    </div>
  );
};
