import React, { useState, useEffect, useCallback, useRef, useMemo } from 'react';
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
  const [statusFilter, setStatusFilter] = useState<'ALL' | 'CONFIRMED' | 'HELD' | 'EXPIRED'>('ALL');

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

  const filteredApplications = useMemo(() => {
    if (statusFilter === 'ALL') return applications;
    return applications.filter((app) => app.status === statusFilter);
  }, [applications, statusFilter]);

  // If student ID is invalid or missing
  if (!isValidStudentId) {
    return (
      <div className="space-y-6">
        <div className="bg-white border border-amber-200 rounded-2xl p-8 text-center max-w-xl mx-auto shadow-xs">
          <div className="w-16 h-16 bg-amber-50 rounded-2xl border border-amber-200 flex items-center justify-center mx-auto mb-4 text-amber-600">
            <UserCheck className="w-8 h-8" />
          </div>
          <h2 className="text-xl font-bold text-navy-900 mb-2">
            Student Identity Required / শিক্ষার্থী আইডি আবশ্যক
          </h2>
          <p className="text-sm text-navy-600 leading-relaxed mb-6">
            Please enter your valid <strong>17-digit Student Birth Registration Number (BRN)</strong> in the top verification bar to view your submitted applications, reservation status, and admission confirmations.
          </p>
          <div className="inline-flex items-center gap-2 text-xs font-mono bg-navy-50 text-navy-700 px-4 py-2 rounded-lg border border-navy-200">
            <span>Format: 17 Digits (e.g., 20261110000000001)</span>
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
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-5 border-b border-navy-200">
        <div>
          <div className="inline-flex items-center gap-2 px-2.5 py-0.5 rounded-full bg-brand-50 border border-brand-200 text-brand-700 text-xs font-semibold mb-1">
            <span>Student Application Portal</span>
          </div>
          <h1 className="text-2xl font-black text-navy-900 tracking-tight flex items-center gap-2.5">
            <GraduationCap className="w-7 h-7 text-brand-600" />
            My Applications & Admission Records
          </h1>
          <p className="text-xs sm:text-sm text-navy-500 mt-1">
            Official record ledger for student BRN:{' '}
            <span className="font-mono font-bold text-navy-900 bg-navy-100 px-2 py-0.5 rounded">{studentId}</span>
          </p>
        </div>

        <div className="flex items-center gap-2.5 self-start sm:self-auto">
          <button
            type="button"
            onClick={() => loadApplications(false)}
            disabled={isRefreshing}
            className="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold rounded-lg border border-navy-200 bg-white hover:bg-navy-50 text-navy-700 shadow-2xs transition disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isRefreshing ? 'animate-spin text-brand-600' : ''}`} />
            <span>Refresh Records</span>
          </button>

          <button
            type="button"
            onClick={onNavigateToCatalog}
            className="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-bold rounded-lg bg-brand-600 hover:bg-brand-700 text-white shadow-2xs transition"
          >
            <span>Browse Classes</span>
            <ArrowRight className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>

      {/* Summary KPI Badges */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white border border-navy-200 rounded-xl p-4 shadow-2xs flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-brand-50 border border-brand-100 flex items-center justify-center text-brand-600 shrink-0">
            <FolderOpen className="w-6 h-6" />
          </div>
          <div>
            <div className="text-xs font-bold text-navy-500 uppercase tracking-wider">Total Submissions</div>
            <div className="text-2xl font-black text-navy-900 font-mono">{applications.length}</div>
          </div>
        </div>

        <div className="bg-white border border-navy-200 rounded-xl p-4 shadow-2xs flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-amber-50 border border-amber-100 flex items-center justify-center text-amber-600 shrink-0">
            <Clock className="w-6 h-6" />
          </div>
          <div>
            <div className="text-xs font-bold text-amber-800 uppercase tracking-wider">Active Seat Holds</div>
            <div className="text-2xl font-black text-amber-900 font-mono">{activeHoldsCount}</div>
          </div>
        </div>

        <div className="bg-white border border-navy-200 rounded-xl p-4 shadow-2xs flex items-center gap-4">
          <div className="w-12 h-12 rounded-xl bg-emerald-50 border border-emerald-100 flex items-center justify-center text-emerald-600 shrink-0">
            <ShieldCheck className="w-6 h-6" />
          </div>
          <div>
            <div className="text-xs font-bold text-emerald-800 uppercase tracking-wider">Confirmed Admissions</div>
            <div className="text-2xl font-black text-emerald-900 font-mono">{confirmedCount}</div>
          </div>
        </div>
      </div>

      {/* Filter Tabs */}
      {applications.length > 0 && (
        <div className="flex items-center gap-2 border-b border-navy-200 pb-2">
          {(['ALL', 'CONFIRMED', 'HELD', 'EXPIRED'] as const).map((filter) => (
            <button
              key={filter}
              type="button"
              onClick={() => setStatusFilter(filter)}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                statusFilter === filter
                  ? 'bg-navy-900 text-white shadow-2xs'
                  : 'text-navy-600 hover:text-navy-900 hover:bg-navy-100'
              }`}
            >
              {filter === 'ALL' && `All Applications (${applications.length})`}
              {filter === 'CONFIRMED' && `Confirmed (${confirmedCount})`}
              {filter === 'HELD' && `Active Holds (${activeHoldsCount})`}
              {filter === 'EXPIRED' && `Expired / Cancelled`}
            </button>
          ))}
        </div>
      )}

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
        <div className="py-20 text-center space-y-3">
          <RefreshCw className="w-8 h-8 text-brand-600 animate-spin mx-auto" />
          <p className="text-sm font-semibold text-navy-600">Retrieving student records from database...</p>
        </div>
      ) : applications.length === 0 ? (
        <div className="bg-white border border-navy-200 rounded-xl p-10 text-center max-w-lg mx-auto shadow-xs">
          <div className="w-16 h-16 bg-navy-50 rounded-2xl flex items-center justify-center mx-auto mb-4 text-navy-400 border border-navy-200">
            <FolderOpen className="w-8 h-8" />
          </div>
          <h3 className="text-base font-bold text-navy-900 mb-1">
            No Applications Found / কোনো আবেদন পাওয়া যায়নি
          </h3>
          <p className="text-xs sm:text-sm text-navy-500 mb-6 leading-relaxed">
            You have not reserved or enrolled in any class seats yet. Explore available classes to reserve a seat.
          </p>
          <button
            type="button"
            onClick={onNavigateToCatalog}
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-lg bg-brand-600 hover:bg-brand-700 text-white font-bold text-xs sm:text-sm shadow-xs transition"
          >
            <span>Explore Available Classes</span>
            <ArrowRight className="w-4 h-4" />
          </button>
        </div>
      ) : filteredApplications.length === 0 ? (
        <div className="bg-white border border-navy-200 rounded-xl p-8 text-center max-w-md mx-auto">
          <p className="text-sm font-semibold text-navy-700">No applications match the selected status filter.</p>
          <button
            type="button"
            onClick={() => setStatusFilter('ALL')}
            className="mt-3 text-xs font-bold text-brand-600 hover:underline"
          >
            Show All Applications
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {filteredApplications.map((application) => (
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
