import React from 'react';
import { useStudent } from '../context/StudentContext';
import { UserCheck, AlertCircle, CheckCircle2, XCircle, Sparkles } from 'lucide-react';

export const StudentIdBar: React.FC = () => {
  const {
    studentId,
    setStudentId,
    clearStudentId,
    isValidStudentId,
    generateRandomStudentId
  } = useStudent();

  return (
    <div className="bg-white border-b border-slate-200 shadow-sm">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-3">
        <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3">
          {/* Left: Input with icon & label */}
          <div className="flex items-center space-x-3 flex-1 max-w-xl">
            <div className={`p-2 rounded-lg transition-colors ${
              isValidStudentId ? 'bg-emerald-50 text-emerald-600' : 'bg-slate-100 text-slate-500'
            }`}>
              <UserCheck className="w-5 h-5" />
            </div>

            <div className="flex-1">
              <label htmlFor="student-id-input" className="block text-xs font-semibold uppercase tracking-wider text-slate-500 mb-1">
                Student Birth Certificate No (17 Digits)
              </label>
              <div className="relative rounded-md shadow-sm">
                <input
                  id="student-id-input"
                  type="text"
                  inputMode="numeric"
                  maxLength={17}
                  value={studentId}
                  onChange={(e) => setStudentId(e.target.value)}
                  placeholder="e.g. 20152691234567890"
                  className={`block w-full font-mono text-sm sm:text-base rounded-md px-3 py-1.5 border transition-colors focus:outline-none focus:ring-2 ${
                    isValidStudentId
                      ? 'border-emerald-400 bg-emerald-50/20 focus:ring-emerald-500 focus:border-emerald-500 text-emerald-950 font-semibold'
                      : studentId.length > 0
                      ? 'border-amber-300 bg-amber-50/20 focus:ring-amber-500 focus:border-amber-500 text-slate-900'
                      : 'border-slate-300 focus:ring-indigo-500 focus:border-indigo-500 text-slate-900'
                  }`}
                />
                {studentId.length > 0 && (
                  <button
                    type="button"
                    onClick={clearStudentId}
                    title="Clear ID"
                    className="absolute inset-y-0 right-0 pr-2.5 flex items-center text-slate-400 hover:text-slate-600"
                  >
                    <XCircle className="w-4 h-4" />
                  </button>
                )}
              </div>
            </div>
          </div>

          {/* Right: Status badge & Helper buttons */}
          <div className="flex items-center space-x-2 sm:space-x-3 self-end md:self-center">
            {/* Real-time digit counter & status */}
            <div className="flex items-center space-x-1.5">
              <span className={`text-xs font-mono font-medium px-2 py-1 rounded-md ${
                isValidStudentId
                  ? 'bg-emerald-100 text-emerald-800'
                  : studentId.length > 0
                  ? 'bg-amber-100 text-amber-800'
                  : 'bg-slate-100 text-slate-600'
              }`}>
                {studentId.length} / 17 digits
              </span>

              {isValidStudentId ? (
                <span className="hidden sm:inline-flex items-center text-xs text-emerald-700 font-medium">
                  <CheckCircle2 className="w-4 h-4 mr-1 text-emerald-600" />
                  Active
                </span>
              ) : studentId.length > 0 ? (
                <span className="hidden sm:inline-flex items-center text-xs text-amber-700 font-medium">
                  <AlertCircle className="w-4 h-4 mr-1 text-amber-500" />
                  Incomplete
                </span>
              ) : null}
            </div>

            {/* Quick Generator Button for easy evaluator testing */}
            <button
              type="button"
              onClick={generateRandomStudentId}
              className="inline-flex items-center px-2.5 py-1.5 border border-slate-200 text-xs font-medium rounded-md text-slate-700 bg-white hover:bg-slate-50 hover:border-slate-300 shadow-sm transition-colors focus:outline-none focus:ring-2 focus:ring-indigo-500"
            >
              <Sparkles className="w-3.5 h-3.5 mr-1 text-indigo-500" />
              Sample ID
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
