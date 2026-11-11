import React, { useState, useEffect, useCallback, useRef } from 'react';
import { StudentHoldItem } from '../types/api';
import { apiClient, ApiClientError } from '../services/apiClient';
import { useStudent } from '../context/StudentContext';
import { useNotification } from '../context/NotificationContext';
import { ApplicationCard } from '../components/ApplicationCard';
import { ErrorBanner } from '../components/ErrorBanner';
import {
  GraduationCap,
  RefreshCw,
  Clock,
  FolderOpen,
  ArrowRight,
  ShieldCheck,
  UserCheck
} from 'lucide-react';

interface MyApplicationsPageProps {
  onNavigateToCatalog: () => void;
}

export const MyApplicationsPage: React.FC<MyApplicationsPageProps> = ({
  onNavigateToCatalog
}) => {
  const { studentId, isValidStudentId } = useStudent();
  const { showError, showSuccess, showNotification } = useNotification();

  const [applications, setApplications] = useState<StudentHoldItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [fetchError, setFetchError] = useState<string | null>(null);

  const isMountedRef = useRef(true);

  useEffect(() => {
    isMountedRef.current = true;
    return () => {
      isMountedRef.current = false;
    };
  }, []);

  const loadApplications = useCallback(
    async (isSilent = false) => {
      if (!isValidStudentId) {
        setApplications([]);
        setIsLoading(false);
        return;
      }

      if (!isSilent) setIsRefreshing(true);
      try {
        const data = await apiClient.getMyHolds();
        if (isMountedRef.current) {
          setApplications(data);
          setFetchError(null);
        }
      } catch (err) {
        if (isMountedRef.current) {
          let errorMsg = 'Failed to load applications from the server.';
          if (err instanceof ApiClientError) {
            errorMsg = err.message;
          }
          setFetchError(errorMsg);
          if (!isSilent) {
            showError(err, 'Failed to fetch application records.');
          }
        }
      } finally {
        if (isMountedRef.current) {
          setIsLoading(false);
          setIsRefreshing(false);
        }
      }
    },
    [isValidStudentId, showError]
  );

  useEffect(() => {
    loadApplications();
  }, [loadApplications]);

  const handleConfirm = async (holdId: string) => {
    try {
      await apiClient.confirmHold(holdId);
      showSuccess(
        'Admission fee paid successfully! Your seat is now guaranteed.',
        'Enrollment Confirmed / ভর্তি নিশ্চিত'
      );
      await loadApplications(true);
    } catch (err) {
      showError(err, 'Could not confirm admission fee payment.');
      await loadApplications(true);
    }
  };

  const handleCancel = async (holdId: string) => {
    try {
      await apiClient.cancelHold(holdId);
      showNotification(
        'info',
        'Reservation Cancelled',
        'Your seat hold has been released and returned to the available pool.'
      );
      await loadApplications(true);
    } catch (err) {
      showError(err, 'Could not cancel reservation.');
      await loadApplications(true);
    }
  };

  // If student ID is invalid or missing
  if (!isValidStudentId) {
    return (
      <div className="space-y-6">
        <div className="bg-amber-50 border border-amber-200 rounded-2xl p-6 sm:p-8 text-center max-w-2xl mx-auto shadow-sm">
          <div className="w-16 h-16 bg-amber-100 rounded-full flex items-center justify-center mx-auto mb-4 text-amber-700">
            <UserCheck className="w-8 h-8" />
          </div>
          <h2 className="text-xl font-bold text-amber-900 mb-2">
            Student Identity Required / শিক্ষার্থী আইডি আবশ্যক
          </h2>
          <p className="text-sm text-amber-800 leading-relaxed mb-6">
            Please enter your valid <strong>17-digit Student ID</strong> in the identification bar
            above to view your submitted applications, reservation status, and admission confirmations.
          </p>
          <div className="inline-flex items-center gap-2 text-xs font-mono bg-amber-100/70 text-amber-900 px-4 py-2 rounded-lg">
            <span>Format: Exactly 17 digits (e.g. 20261110000000001)</span>
          </div>
        </div>
      </div>
    );
  }

  const activeHoldsCount = applications.filter((a) => a.status === 'HELD').length;
  const confirmedCount = applications.filter((a) => a.status === 'CONFIRMED').length;

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-slate-200">
        <div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight flex items-center gap-2.5">
            <GraduationCap className="w-7 h-7 text-indigo-600" />
            My Applications / আমার আবেদন
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Tracking admission reservations, countdown timers, and payment history for student{' '}
            <span className="font-mono font-semibold text-slate-700">{studentId}</span>
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => loadApplications(false)}
            disabled={isRefreshing}
            className="inline-flex items-center gap-2 px-3.5 py-2 text-xs font-semibold rounded-xl border border-slate-200 bg-white hover:bg-slate-50 active:bg-slate-100 text-slate-700 shadow-sm transition disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isRefreshing ? 'animate-spin text-indigo-600' : ''}`} />
            Refresh / রিফ্রেশ
          </button>

          <button
            type="button"
            onClick={onNavigateToCatalog}
            className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-xl bg-indigo-600 hover:bg-indigo-700 active:bg-indigo-800 text-white shadow-sm transition"
          >
            Browse Classes / ক্লাসের তালিকা
            <ArrowRight className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>

      {/* Summary KPI Badges */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white border border-slate-200 rounded-2xl p-4 shadow-sm flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-indigo-50 border border-indigo-100 flex items-center justify-center text-indigo-600">
            <FolderOpen className="w-6 h-6" />
          </div>
          <div>
            <div className="text-xs font-medium text-slate-500">Total Applications</div>
            <div className="text-2xl font-black text-slate-900">{applications.length}</div>
          </div>
        </div>

        <div className="bg-white border border-slate-200 rounded-2xl p-4 shadow-sm flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-amber-50 border border-amber-100 flex items-center justify-center text-amber-600">
            <Clock className="w-6 h-6" />
          </div>
          <div>
            <div className="text-xs font-medium text-slate-500">Active Seat Holds</div>
            <div className="text-2xl font-black text-amber-900">{activeHoldsCount}</div>
          </div>
        </div>

        <div className="bg-white border border-slate-200 rounded-2xl p-4 shadow-sm flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-emerald-50 border border-emerald-100 flex items-center justify-center text-emerald-600">
            <ShieldCheck className="w-6 h-6" />
          </div>
          <div>
            <div className="text-xs font-medium text-slate-500">Confirmed Admissions</div>
            <div className="text-2xl font-black text-emerald-900">{confirmedCount}</div>
          </div>
        </div>
      </div>

      {/* Inline Error Banner if Fetch Failed */}
      {fetchError && (
        <ErrorBanner
          title="Could not retrieve application history"
          message={fetchError}
          onRetry={() => loadApplications(false)}
          onDismiss={() => setFetchError(null)}
        />
      )}

      {/* Main Content Area */}
      {isLoading ? (
        <div className="py-20 text-center">
          <RefreshCw className="w-8 h-8 text-indigo-600 animate-spin mx-auto mb-3" />
          <p className="text-sm font-medium text-slate-600">Loading your applications...</p>
        </div>
      ) : applications.length === 0 ? (
        <div className="bg-white border border-slate-200 rounded-2xl p-10 text-center max-w-lg mx-auto shadow-sm">
          <div className="w-16 h-16 bg-slate-100 rounded-full flex items-center justify-center mx-auto mb-4 text-slate-400">
            <FolderOpen className="w-8 h-8" />
          </div>
          <h3 className="text-base font-bold text-slate-800 mb-1">
            No Applications Found / কোনো আবেদন পাওয়া যায়নি
          </h3>
          <p className="text-xs sm:text-sm text-slate-500 mb-6 leading-relaxed">
            You haven't reserved or applied for any class seats yet. Explore available classes now to reserve a seat.
          </p>
          <button
            type="button"
            onClick={onNavigateToCatalog}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-xs sm:text-sm shadow-sm transition"
          >
            Explore Available Classes / ক্লাস দেখুন
            <ArrowRight className="w-4 h-4" />
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {applications.map((application) => (
            <ApplicationCard
              key={application.holdId}
              application={application}
              onConfirm={handleConfirm}
              onCancel={handleCancel}
            />
          ))}
        </div>
      )}
    </div>
  );
};
