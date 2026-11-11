import React from 'react';
import { ClassItem } from '../types/api';
import { 
  Users, 
  CheckCircle2, 
  Clock, 
  AlertCircle, 
  ArrowRight, 
  Loader2, 
  Sun, 
  Sunset, 
  BookOpen, 
  Lock,
  BadgeAlert
} from 'lucide-react';

interface ClassCardProps {
  classItem: ClassItem;
  isValidStudentId: boolean;
  isApplying: boolean;
  onApply: (classId: string) => void;
}

export const ClassCard: React.FC<ClassCardProps> = ({
  classItem,
  isValidStudentId,
  isApplying,
  onApply
}) => {
  const isSoldOut = classItem.availableSeats <= 0;

  // Percentage calculations for capacity progress bar
  const confirmedPercent = Math.min(100, Math.round((classItem.confirmedSeats / classItem.totalSeats) * 100));
  const heldPercent = Math.min(100 - confirmedPercent, Math.round((classItem.heldSeats / classItem.totalSeats) * 100));
  const occupiedCount = classItem.confirmedSeats + classItem.heldSeats;

  // Derive shift details from class name
  const isMorning = classItem.name.toLowerCase().includes('morning') || classItem.name.toLowerCase().includes('prabhati');
  const isDay = classItem.name.toLowerCase().includes('day') || classItem.name.toLowerCase().includes('diba');

  return (
    <div className={`bg-white rounded-xl border transition-all duration-200 flex flex-col justify-between overflow-hidden ${
      isSoldOut
        ? 'border-navy-200 bg-navy-50/40 opacity-90'
        : 'border-navy-200 hover:border-brand-500 hover:shadow-md'
    }`}>
      {/* Top Header Card */}
      <div className="p-5 border-b border-navy-100">
        <div className="flex items-center justify-between gap-2 mb-2.5">
          <div className="flex items-center gap-1.5">
            {isMorning ? (
              <span className="inline-flex items-center gap-1 text-[11px] font-bold text-amber-800 bg-amber-100/90 px-2.5 py-0.5 rounded-full">
                <Sun className="w-3 h-3 text-amber-600" />
                Morning Shift (07:30 AM – 12:00 PM)
              </span>
            ) : isDay ? (
              <span className="inline-flex items-center gap-1 text-[11px] font-bold text-sky-800 bg-sky-100/90 px-2.5 py-0.5 rounded-full">
                <Sunset className="w-3 h-3 text-sky-600" />
                Day Shift (12:30 PM – 05:00 PM)
              </span>
            ) : (
              <span className="inline-flex items-center gap-1 text-[11px] font-bold text-navy-700 bg-navy-100 px-2.5 py-0.5 rounded-full">
                <BookOpen className="w-3 h-3 text-navy-500" />
                Standard Offering
              </span>
            )}
          </div>

          <span className="font-mono text-xs text-navy-500 bg-navy-100/80 px-2 py-0.5 rounded border border-navy-200">
            {classItem.id}
          </span>
        </div>

        <h3 className="text-lg font-bold text-navy-900 leading-snug tracking-tight">
          {classItem.name}
        </h3>

        {/* Visual Occupancy Bar */}
        <div className="space-y-1.5 mt-4">
          <div className="flex justify-between text-xs text-navy-600">
            <span className="font-medium">Total Seat Allocation</span>
            <span className="font-bold text-navy-900 font-mono">
              {occupiedCount} / {classItem.totalSeats} seats
            </span>
          </div>

          <div className="w-full h-2.5 bg-navy-100 rounded-full overflow-hidden flex shadow-inner">
            {confirmedPercent > 0 && (
              <div
                style={{ width: `${confirmedPercent}%` }}
                className="bg-emerald-500 transition-all duration-500"
                title={`Confirmed: ${classItem.confirmedSeats} seats`}
              />
            )}
            {heldPercent > 0 && (
              <div
                style={{ width: `${heldPercent}%` }}
                className="bg-amber-400 transition-all duration-500"
                title={`Active Holds: ${classItem.heldSeats} seats`}
              />
            )}
          </div>
        </div>
      </div>

      {/* Middle: 4-Grid Metrics Breakdown */}
      <div className="p-4 grid grid-cols-2 gap-2 bg-navy-50/50">
        <div className="p-2.5 bg-white rounded-lg border border-navy-100 shadow-2xs">
          <div className="flex items-center space-x-1.5 text-xs text-navy-500 mb-0.5">
            <Users className="w-3.5 h-3.5 text-navy-400" />
            <span>Capacity</span>
          </div>
          <p className="text-lg font-bold text-navy-900 font-mono">{classItem.totalSeats}</p>
        </div>

        <div className="p-2.5 bg-white rounded-lg border border-navy-100 shadow-2xs">
          <div className="flex items-center space-x-1.5 text-xs text-emerald-700 mb-0.5 font-medium">
            <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" />
            <span>Confirmed</span>
          </div>
          <p className="text-lg font-bold text-emerald-700 font-mono">{classItem.confirmedSeats}</p>
        </div>

        <div className="p-2.5 bg-white rounded-lg border border-navy-100 shadow-2xs">
          <div className="flex items-center space-x-1.5 text-xs text-amber-700 mb-0.5 font-medium">
            <Clock className="w-3.5 h-3.5 text-amber-600" />
            <span>Active Holds</span>
          </div>
          <p className="text-lg font-bold text-amber-700 font-mono">{classItem.heldSeats}</p>
        </div>

        <div className={`p-2.5 rounded-lg border shadow-2xs transition-colors ${
          isSoldOut
            ? 'bg-rose-50 border-rose-200 text-rose-800'
            : 'bg-brand-50 border-brand-200 text-brand-900'
        }`}>
          <div className="flex items-center space-x-1.5 text-xs mb-0.5 font-bold">
            {isSoldOut ? <BadgeAlert className="w-3.5 h-3.5 text-rose-600" /> : <BookOpen className="w-3.5 h-3.5 text-brand-600" />}
            <span>Available</span>
          </div>
          <p className="text-lg font-extrabold font-mono">
            {isSoldOut ? '0 (Full)' : classItem.availableSeats}
          </p>
        </div>
      </div>

      {/* Bottom Action Footer */}
      <div className="p-4 border-t border-navy-100 bg-white space-y-3">
        <div className="flex items-center justify-between text-xs text-navy-500">
          <span>Admission Fee:</span>
          <span className="font-bold text-navy-800">৳ 1,500 BDT</span>
        </div>

        <button
          type="button"
          disabled={isSoldOut || isApplying}
          onClick={() => onApply(classItem.id)}
          className={`w-full py-2.5 px-4 rounded-lg font-bold text-sm transition-all flex items-center justify-center gap-2 ${
            isSoldOut
              ? 'bg-navy-100 text-navy-400 cursor-not-allowed border border-navy-200'
              : !isValidStudentId
              ? 'bg-navy-800 text-white hover:bg-navy-900 shadow-xs'
              : 'bg-brand-600 text-white hover:bg-brand-700 shadow-xs hover:shadow-sm'
          }`}
        >
          {isApplying ? (
            <>
              <Loader2 className="w-4 h-4 animate-spin" />
              <span>Reserving Hold...</span>
            </>
          ) : isSoldOut ? (
            <>
              <Lock className="w-4 h-4" />
              <span>Class Full (Sold Out)</span>
            </>
          ) : !isValidStudentId ? (
            <>
              <AlertCircle className="w-4 h-4 text-amber-300" />
              <span>Verify 17-Digit ID to Reserve</span>
            </>
          ) : (
            <>
              <span>Reserve Seat (Hold 10 Mins)</span>
              <ArrowRight className="w-4 h-4" />
            </>
          )}
        </button>
      </div>
    </div>
  );
};
