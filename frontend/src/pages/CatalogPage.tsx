import React, { useState, useEffect, useCallback, useRef, useMemo } from 'react';
import { ClassItem, HoldResponse } from '../types/api';
import { apiClient, ApiClientError } from '../services/apiClient';
import { useStudent } from '../context/StudentContext';
import { useNotification } from '../context/NotificationContext';
import { ClassCard } from '../components/ClassCard';
import { AdminCreateClassModal } from '../components/AdminCreateClassModal';
import { ErrorBanner } from '../components/ErrorBanner';
import { 
  RefreshCw, 
  Plus, 
  Layers, 
  Loader2, 
  Search, 
  GraduationCap,
  Building2,
  CalendarDays
} from 'lucide-react';

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
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedShiftFilter, setSelectedShiftFilter] = useState<'ALL' | 'MORNING' | 'DAY' | 'AVAILABLE'>('ALL');
  const [lastRefreshedAt, setLastRefreshedAt] = useState<Date>(new Date());

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
        setLastRefreshedAt(new Date());
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
        `Seat hold placed successfully (Hold ID: ${hold.holdId.substring(0, 8)}...). Complete fee payment within 10 minutes to secure admission.`,
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

  // Filtered classes based on search query and shift filter
  const filteredClasses = useMemo(() => {
    return classes.filter((item) => {
      const matchesSearch = item.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
                            item.id.toLowerCase().includes(searchQuery.toLowerCase());

      if (!matchesSearch) return false;

      if (selectedShiftFilter === 'MORNING') {
        return item.name.toLowerCase().includes('morning') || item.name.toLowerCase().includes('prabhati');
      }
      if (selectedShiftFilter === 'DAY') {
        return item.name.toLowerCase().includes('day') || item.name.toLowerCase().includes('diba');
      }
      if (selectedShiftFilter === 'AVAILABLE') {
        return item.availableSeats > 0;
      }

      return true;
    });
  }, [classes, searchQuery, selectedShiftFilter]);

  // Aggregate stats
  const totalCapacity = useMemo(() => classes.reduce((sum, c) => sum + c.totalSeats, 0), [classes]);
  const totalAvailable = useMemo(() => classes.reduce((sum, c) => sum + c.availableSeats, 0), [classes]);
  const totalConfirmed = useMemo(() => classes.reduce((sum, c) => sum + c.confirmedSeats, 0), [classes]);

  return (
    <div className="space-y-6">
      
      {/* Institutional Hero Banner */}
      <div className="bg-gradient-to-r from-navy-900 via-navy-800 to-navy-900 text-white rounded-2xl p-6 sm:p-8 shadow-sm border border-navy-700/60 relative overflow-hidden">
        {/* Subtle decorative background watermark */}
        <div className="absolute right-0 top-0 bottom-0 opacity-5 pointer-events-none flex items-center pr-6">
          <Building2 className="w-80 h-80 text-white" />
        </div>

        <div className="relative z-10 max-w-3xl space-y-3">
          <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-brand-500/20 border border-brand-400/30 text-brand-300 text-xs font-semibold uppercase tracking-wider">
            <CalendarDays className="w-3.5 h-3.5 text-brand-400" />
            <span>Academic Session 2026–2027 • Centralized Admission</span>
          </div>

          <h2 className="text-2xl sm:text-3xl font-extrabold tracking-tight text-white leading-tight">
            Secondary School Seat Allocation Portal
          </h2>

          <p className="text-sm text-navy-200 leading-relaxed max-w-2xl">
            Official seat reservation system for Secondary Class Offerings. Built on OnnoRokom Projukti Ltd&apos;s 
            high-concurrency FCFS engine guaranteeing zero seat overselling and atomic 10-minute hold reservation windows.
          </p>

          {/* Key Stat Cards */}
          <div className="pt-3 grid grid-cols-2 sm:grid-cols-4 gap-3">
            <div className="bg-white/10 backdrop-blur-xs rounded-xl p-3 border border-white/10">
              <span className="text-xs text-navy-300 block">Class Offerings</span>
              <span className="text-xl font-bold font-mono text-white">{classes.length} Shifts</span>
            </div>

            <div className="bg-white/10 backdrop-blur-xs rounded-xl p-3 border border-white/10">
              <span className="text-xs text-navy-300 block">Total Seat Capacity</span>
              <span className="text-xl font-bold font-mono text-white">{totalCapacity}</span>
            </div>

            <div className="bg-white/10 backdrop-blur-xs rounded-xl p-3 border border-white/10">
              <span className="text-xs text-emerald-300 block">Available Seats</span>
              <span className="text-xl font-bold font-mono text-emerald-400">{totalAvailable}</span>
            </div>

            <div className="bg-white/10 backdrop-blur-xs rounded-xl p-3 border border-white/10">
              <span className="text-xs text-brand-300 block">Confirmed Enrolled</span>
              <span className="text-xl font-bold font-mono text-brand-300">{totalConfirmed}</span>
            </div>
          </div>
        </div>
      </div>

      {/* 4-Step Human Admission Process Guide */}
      <div className="bg-white rounded-xl border border-navy-200/80 p-4 sm:p-5 shadow-xs">
        <h4 className="text-xs font-bold uppercase tracking-wider text-navy-500 mb-3 flex items-center gap-2">
          <GraduationCap className="w-4 h-4 text-brand-600" />
          <span>How It Works: Step-by-Step Enrollment Procedure</span>
        </h4>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 text-xs">
          <div className="p-3 bg-navy-50 rounded-lg border border-navy-100 flex items-start gap-2.5">
            <span className="w-5 h-5 rounded-full bg-brand-600 text-white font-bold flex items-center justify-center shrink-0 text-[11px]">
              1
            </span>
            <div>
              <p className="font-bold text-navy-900">Verify Student BRN</p>
              <p className="text-navy-500 text-[11px] mt-0.5">Enter 17-digit digital birth certificate number in top bar.</p>
            </div>
          </div>

          <div className="p-3 bg-navy-50 rounded-lg border border-navy-100 flex items-start gap-2.5">
            <span className="w-5 h-5 rounded-full bg-brand-600 text-white font-bold flex items-center justify-center shrink-0 text-[11px]">
              2
            </span>
            <div>
              <p className="font-bold text-navy-900">Choose Class & Shift</p>
              <p className="text-navy-500 text-[11px] mt-0.5">Select preferred Morning or Day shift based on availability.</p>
            </div>
          </div>

          <div className="p-3 bg-navy-50 rounded-lg border border-navy-100 flex items-start gap-2.5">
            <span className="w-5 h-5 rounded-full bg-brand-600 text-white font-bold flex items-center justify-center shrink-0 text-[11px]">
              3
            </span>
            <div>
              <p className="font-bold text-navy-900">10-Minute Seat Hold</p>
              <p className="text-navy-500 text-[11px] mt-0.5">Your seat is exclusively held while you complete fee payment.</p>
            </div>
          </div>

          <div className="p-3 bg-navy-50 rounded-lg border border-navy-100 flex items-start gap-2.5">
            <span className="w-5 h-5 rounded-full bg-emerald-600 text-white font-bold flex items-center justify-center shrink-0 text-[11px]">
              4
            </span>
            <div>
              <p className="font-bold text-navy-900">Confirm & Get Slip</p>
              <p className="text-navy-500 text-[11px] mt-0.5">Pay ৳ 1,500 admission fee to instantly secure enrollment.</p>
            </div>
          </div>
        </div>
      </div>

      {/* Catalog Search & Controls Bar */}
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3 bg-white p-4 rounded-xl border border-navy-200/80 shadow-xs">
        
        {/* Left: Search input */}
        <div className="relative flex-1 max-w-md">
          <Search className="w-4 h-4 text-navy-400 absolute left-3 top-1/2 -translate-y-1/2" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search by class name or ID (e.g., Morning, c_c6m)..."
            className="w-full pl-9 pr-3.5 py-2 text-xs sm:text-sm bg-navy-50/60 border border-navy-200 rounded-lg focus:outline-none focus:border-brand-600 focus:bg-white transition-all text-navy-900 placeholder-navy-400"
          />
        </div>

        {/* Middle: Shift Filter Pills */}
        <div className="flex flex-wrap items-center gap-1.5">
          {(['ALL', 'MORNING', 'DAY', 'AVAILABLE'] as const).map((filterKey) => (
            <button
              key={filterKey}
              type="button"
              onClick={() => setSelectedShiftFilter(filterKey)}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                selectedShiftFilter === filterKey
                  ? 'bg-navy-900 text-white shadow-2xs'
                  : 'bg-navy-50 text-navy-600 hover:bg-navy-100 hover:text-navy-900 border border-navy-200/80'
              }`}
            >
              {filterKey === 'ALL' && 'All Offerings'}
              {filterKey === 'MORNING' && 'Morning Shift'}
              {filterKey === 'DAY' && 'Day Shift'}
              {filterKey === 'AVAILABLE' && 'Available Only'}
            </button>
          ))}
        </div>

        {/* Right: Actions */}
        <div className="flex items-center gap-2 self-end md:self-auto">
          <button
            type="button"
            onClick={() => fetchClasses(false)}
            disabled={isRefreshing}
            className="px-3 py-2 border border-navy-200 rounded-lg text-navy-700 hover:text-navy-900 hover:bg-navy-50 transition-colors text-xs font-semibold flex items-center gap-1.5 shadow-2xs disabled:opacity-50"
            title={`Last refreshed at ${lastRefreshedAt.toLocaleTimeString()}`}
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isRefreshing ? 'animate-spin text-brand-600' : ''}`} />
            <span>Refresh</span>
          </button>

          <button
            type="button"
            onClick={() => setIsCreateModalOpen(true)}
            className="px-3.5 py-2 bg-brand-600 hover:bg-brand-700 active:bg-brand-800 text-white rounded-lg text-xs font-bold shadow-2xs transition-all flex items-center gap-1.5"
          >
            <Plus className="w-4 h-4" />
            <span>Add Offering</span>
          </button>
        </div>

      </div>

      {/* Global Error Banner */}
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
        <div className="py-20 flex flex-col items-center justify-center text-navy-400 space-y-3">
          <Loader2 className="w-8 h-8 animate-spin text-brand-600" />
          <p className="text-sm font-semibold text-navy-600">Retrieving official class offerings from database...</p>
        </div>
      ) : filteredClasses.length === 0 ? (
        <div className="py-16 bg-white border border-dashed border-navy-300 rounded-xl text-center space-y-3">
          <Layers className="w-10 h-10 text-navy-300 mx-auto" />
          <h3 className="text-base font-bold text-navy-800">No matching class offerings found</h3>
          <p className="text-xs text-navy-500 max-w-sm mx-auto">
            {searchQuery
              ? `No offerings matched "${searchQuery}". Try clearing search filters.`
              : 'Click "Add Offering" to seed or create the first admission offering for this academic year.'}
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
          {filteredClasses.map((c) => (
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
