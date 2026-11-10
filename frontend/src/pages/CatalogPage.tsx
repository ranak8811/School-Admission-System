import React, { useState, useEffect, useCallback, useRef } from 'react';
import { ClassItem, HoldResponse } from '../types/api';
import { apiClient, ApiClientError } from '../services/apiClient';
import { useStudent } from '../context/StudentContext';
import { ClassCard } from '../components/ClassCard';
import { AdminCreateClassModal } from '../components/AdminCreateClassModal';
import { RefreshCw, Plus, AlertCircle, CheckCircle2, Layers, Loader2 } from 'lucide-react';

interface CatalogPageProps {
  onHoldCreated?: (hold: HoldResponse) => void;
}

export const CatalogPage: React.FC<CatalogPageProps> = ({ onHoldCreated }) => {
  const { studentId, isValidStudentId } = useStudent();
  const [classes, setClasses] = useState<ClassItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [applyingClassId, setApplyingClassId] = useState<string | null>(null);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [notification, setNotification] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const isMountedRef = useRef(true);

  useEffect(() => {
    isMountedRef.current = true;
    return () => {
      isMountedRef.current = false;
    };
  }, []);

  // Fetch classes from backend API
  const fetchClasses = useCallback(async (isSilent = false) => {
    if (!isSilent) setIsRefreshing(true);
    try {
      const data = await apiClient.getClasses();
      if (isMountedRef.current) {
        setClasses(data);
        setError(null);
      }
    } catch (err) {
      if (isMountedRef.current) {
        if (!isSilent) {
          if (err instanceof ApiClientError) {
            setError(err.message);
          } else {
            setError('Failed to fetch class catalog from backend.');
          }
        }
      }
    } finally {
      if (isMountedRef.current) {
        setIsLoading(false);
        setIsRefreshing(false);
      }
    }
  }, []);

  // Initial fetch and 4-second background polling
  useEffect(() => {
    fetchClasses(false);

    const pollingInterval = setInterval(() => {
      fetchClasses(true);
    }, 4000);

    return () => {
      clearInterval(pollingInterval);
    };
  }, [fetchClasses]);

  // Handle seat hold application
  const handleApply = async (classId: string) => {
    if (!isValidStudentId) {
      setNotification({
        type: 'error',
        message: 'Please enter a valid 17-digit student birth registration number above before applying.'
      });
      return;
    }

    try {
      setApplyingClassId(classId);
      setNotification(null);

      const hold = await apiClient.createHold(classId, studentId);

      setNotification({
        type: 'success',
        message: `Seat hold placed successfully (Hold ID: ${hold.holdId}). Please proceed with fee payment.`
      });

      // Refresh catalog immediately to reflect updated held/available counts
      await fetchClasses(true);

      if (onHoldCreated) {
        onHoldCreated(hold);
      }
    } catch (err) {
      if (err instanceof ApiClientError) {
        setNotification({
          type: 'error',
          message: err.message
        });
      } else {
        setNotification({
          type: 'error',
          message: 'An unexpected error occurred while placing seat hold.'
        });
      }
    } finally {
      setApplyingClassId(null);
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Controls Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-5 rounded-2xl border border-slate-200 shadow-2xs">
        <div>
          <div className="flex items-center space-x-2">
            <h2 className="text-xl font-bold text-slate-900 tracking-tight">
              Class Catalog & Seat Inventory
            </h2>
            <span className="flex h-2 w-2 relative">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75" />
              <span className="relative inline-flex rounded-full h-2 w-2 bg-emerald-500" />
            </span>
          </div>
          <p className="text-xs sm:text-sm text-slate-500 mt-1">
            Real-time seat allocation polling every 4 seconds. First-come, first-served.
          </p>
        </div>

        <div className="flex items-center space-x-2.5">
          <button
            type="button"
            onClick={() => fetchClasses(false)}
            disabled={isRefreshing}
            className="p-2 sm:px-3 sm:py-2 border border-slate-200 rounded-xl text-slate-600 hover:text-slate-900 hover:bg-slate-50 transition-colors text-xs font-medium flex items-center space-x-1.5 shadow-2xs"
            title="Refresh class capacity"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isRefreshing ? 'animate-spin text-indigo-600' : ''}`} />
            <span className="hidden sm:inline">Refresh</span>
          </button>

          <button
            type="button"
            onClick={() => setIsCreateModalOpen(true)}
            className="px-3.5 py-2 bg-indigo-600 hover:bg-indigo-700 active:bg-indigo-800 text-white rounded-xl text-xs sm:text-sm font-semibold shadow-sm transition-all flex items-center space-x-1.5"
          >
            <Plus className="w-4 h-4" />
            <span>Add Class</span>
          </button>
        </div>
      </div>

      {/* Action Notification Banner */}
      {notification && (
        <div className={`p-4 rounded-xl border flex items-start justify-between space-x-3 transition-all ${
          notification.type === 'success'
            ? 'bg-emerald-50 border-emerald-200 text-emerald-900'
            : 'bg-rose-50 border-rose-200 text-rose-900'
        }`}>
          <div className="flex items-start space-x-2.5">
            {notification.type === 'success' ? (
              <CheckCircle2 className="w-5 h-5 text-emerald-600 shrink-0 mt-0.5" />
            ) : (
              <AlertCircle className="w-5 h-5 text-rose-600 shrink-0 mt-0.5" />
            )}
            <p className="text-sm font-medium">{notification.message}</p>
          </div>
          <button
            type="button"
            onClick={() => setNotification(null)}
            className="text-slate-400 hover:text-slate-600 text-xs font-bold"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Global Error State */}
      {error && (
        <div className="p-4 bg-rose-50 border border-rose-200 rounded-xl flex items-center justify-between text-rose-800 text-sm">
          <div className="flex items-center space-x-2">
            <AlertCircle className="w-5 h-5 text-rose-600" />
            <span>{error}</span>
          </div>
          <button
            type="button"
            onClick={() => fetchClasses(false)}
            className="px-3 py-1 bg-white border border-rose-300 text-rose-700 text-xs rounded-lg font-medium hover:bg-rose-100/50"
          >
            Retry
          </button>
        </div>
      )}

      {/* Classes Grid */}
      {isLoading ? (
        <div className="py-20 flex flex-col items-center justify-center text-slate-400 space-y-3">
          <Loader2 className="w-8 h-8 animate-spin text-indigo-500" />
          <p className="text-sm font-medium">Loading class offerings & capacity...</p>
        </div>
      ) : classes.length === 0 ? (
        <div className="py-16 bg-white border border-dashed border-slate-200 rounded-2xl text-center space-y-3">
          <Layers className="w-10 h-10 text-slate-300 mx-auto" />
          <h3 className="text-base font-semibold text-slate-700">No classes found</h3>
          <p className="text-xs text-slate-500 max-w-sm mx-auto">
            Click &quot;Add Class&quot; to seed or create the first admission offering for this academic year.
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {classes.map((c) => (
            <ClassCard
              key={c.id}
              classItem={c}
              isValidStudentId={isValidStudentId}
              isApplying={applyingClassId === c.id}
              onApply={handleApply}
            />
          ))}
        </div>
      )}

      {/* Admin Create Class Modal */}
      <AdminCreateClassModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onCreated={() => fetchClasses(false)}
      />
    </div>
  );
};
