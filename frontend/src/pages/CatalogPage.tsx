import React, { useState, useEffect, useCallback, useRef } from 'react';
import { ClassItem, HoldResponse } from '../types/api';
import { apiClient, ApiClientError } from '../services/apiClient';
import { useStudent } from '../context/StudentContext';
import { useNotification } from '../context/NotificationContext';
import { ClassCard } from '../components/ClassCard';
import { AdminCreateClassModal } from '../components/AdminCreateClassModal';
import { ErrorBanner } from '../components/ErrorBanner';
import { RefreshCw, Plus, Layers, Loader2 } from 'lucide-react';

interface CatalogPageProps {
  onHoldCreated?: (hold: HoldResponse) => void;
}

export const CatalogPage: React.FC<CatalogPageProps> = ({ onHoldCreated }) => {
  const { studentId, isValidStudentId } = useStudent();
  const { showError, showSuccess } = useNotification();
  const [classes, setClasses] = useState<ClassItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [applyingClassId, setApplyingClassId] = useState<string | null>(null);
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

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
      showError(
        new Error('Please enter a valid 17-digit student birth registration number above before applying.'),
        'Student ID Required'
      );
      return;
    }

    try {
      setApplyingClassId(classId);

      const hold = await apiClient.createHold(classId, studentId);

      showSuccess(
        `Seat hold placed successfully (Hold ID: ${hold.holdId.substring(0, 8)}...). Complete fee payment before the timer expires.`,
        'Seat Reserved / সীট সংরক্ষিত'
      );

      // Refresh catalog immediately to reflect updated held/available counts
      await fetchClasses(true);

      if (onHoldCreated) {
        onHoldCreated(hold);
      }
    } catch (err) {
      showError(err, 'Could not reserve seat. It may already be full or reserved by another applicant.');
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

      {/* Global Error State */}
      {error && (
        <ErrorBanner
          title="Could not load class catalog"
          message={error}
          onRetry={() => fetchClasses(false)}
          onDismiss={() => setError(null)}
        />
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
