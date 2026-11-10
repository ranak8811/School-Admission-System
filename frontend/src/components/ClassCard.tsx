import React from 'react';
import { ClassItem } from '../types/api';
import { Users, CheckCircle, Clock, AlertTriangle, ArrowRight, Loader2, Sparkles } from 'lucide-react';

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

  return (
    <div className={`bg-white rounded-2xl border transition-all duration-200 flex flex-col justify-between overflow-hidden ${
      isSoldOut
        ? 'border-slate-200 bg-slate-50/50 opacity-90'
        : 'border-slate-200 hover:border-indigo-200 hover:shadow-md'
    }`}>
      {/* Top Header */}
      <div className="p-5 border-b border-slate-100">
        <div className="flex items-start justify-between gap-3 mb-2">
          <h3 className="text-base sm:text-lg font-bold text-slate-900 leading-snug">
            {classItem.name}
          </h3>
          <span className="shrink-0 font-mono text-[11px] text-slate-500 bg-slate-100 px-2 py-0.5 rounded-md">
            {classItem.id}
          </span>
        </div>

        {/* Capacity Stacked Progress Bar */}
        <div className="space-y-1.5 mt-4">
          <div className="flex justify-between text-xs text-slate-500">
            <span>Occupancy Ratio</span>
            <span className="font-semibold text-slate-700">
              {classItem.confirmedSeats + classItem.heldSeats} / {classItem.totalSeats} seats
            </span>
          </div>

          <div className="w-full h-2.5 bg-slate-100 rounded-full overflow-hidden flex">
            {confirmedPercent > 0 && (
              <div
                style={{ width: `${confirmedPercent}%` }}
                className="bg-emerald-500 transition-all duration-500"
                title={`Confirmed: ${classItem.confirmedSeats}`}
              />
            )}
            {heldPercent > 0 && (
              <div
                style={{ width: `${heldPercent}%` }}
                className="bg-amber-400 transition-all duration-500"
                title={`Held: ${classItem.heldSeats}`}
              />
            )}
          </div>
        </div>
      </div>

      {/* Middle: Badges Grid */}
      <div className="p-5 grid grid-cols-2 gap-2.5 bg-slate-50/40">
        <div className="p-2.5 bg-white rounded-xl border border-slate-100 shadow-2xs">
          <div className="flex items-center space-x-1.5 text-xs text-slate-500 mb-0.5">
            <Users className="w-3.5 h-3.5" />
            <span>Total Seats</span>
          </div>
          <p className="text-lg font-bold text-slate-800 font-mono">{classItem.totalSeats}</p>
        </div>

        <div className="p-2.5 bg-white rounded-xl border border-slate-100 shadow-2xs">
          <div className="flex items-center space-x-1.5 text-xs text-emerald-600 mb-0.5">
            <CheckCircle className="w-3.5 h-3.5" />
            <span>Confirmed</span>
          </div>
          <p className="text-lg font-bold text-emerald-700 font-mono">{classItem.confirmedSeats}</p>
        </div>

        <div className="p-2.5 bg-white rounded-xl border border-slate-100 shadow-2xs">
          <div className="flex items-center space-x-1.5 text-xs text-amber-600 mb-0.5">
            <Clock className="w-3.5 h-3.5" />
            <span>Active Holds</span>
          </div>
          <p className="text-lg font-bold text-amber-700 font-mono">{classItem.heldSeats}</p>
        </div>

        <div className={`p-2.5 rounded-xl border shadow-2xs transition-colors ${
          isSoldOut
            ? 'bg-rose-50 border-rose-200 text-rose-700'
            : 'bg-emerald-50/80 border-emerald-200 text-emerald-800'
        }`}>
          <div className="flex items-center space-x-1.5 text-xs mb-0.5 font-medium">
            {isSoldOut ? <AlertTriangle className="w-3.5 h-3.5 text-rose-500" /> : <Sparkles className="w-3.5 h-3.5 text-emerald-600" />}
            <span>Available</span>
          </div>
          <p className="text-lg font-extrabold font-mono">
            {isSoldOut ? '0 (Full)' : classItem.availableSeats}
          </p>
        </div>
      </div>

      {/* Bottom Action Footer */}
      <div className="p-4 bg-white border-t border-slate-100">
        {isSoldOut ? (
          <button
            type="button"
            disabled
            className="w-full py-2.5 px-4 bg-slate-200 text-slate-500 font-semibold text-sm rounded-xl cursor-not-allowed flex items-center justify-center space-x-2"
          >
            <AlertTriangle className="w-4 h-4" />
            <span>Sold Out</span>
          </button>
        ) : !isValidStudentId ? (
          <button
            type="button"
            disabled
            className="w-full py-2.5 px-4 bg-slate-100 hover:bg-slate-100 text-slate-400 font-medium text-xs sm:text-sm rounded-xl cursor-not-allowed border border-dashed border-slate-300 flex items-center justify-center text-center"
            title="Please enter a 17-digit student birth registration number in the top bar"
          >
            <span>Enter 17-digit ID Above to Apply</span>
          </button>
        ) : (
          <button
            type="button"
            onClick={() => onApply(classItem.id)}
            disabled={isApplying}
            className="w-full py-2.5 px-4 bg-indigo-600 hover:bg-indigo-700 active:bg-indigo-800 text-white font-semibold text-sm rounded-xl shadow-sm hover:shadow transition-all flex items-center justify-center space-x-2 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isApplying ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" />
                <span>Reserving Seat...</span>
              </>
            ) : (
              <>
                <span>Apply / Hold Seat</span>
                <ArrowRight className="w-4 h-4 ml-1" />
              </>
            )}
          </button>
        )}
      </div>
    </div>
  );
};
