import React from 'react';
import { useStudent } from '../context/StudentContext';
import { ShieldCheck, AlertCircle, CheckCircle2, XCircle, Wand2, Info, Copy } from 'lucide-react';

const DEMO_STUDENT_IDS = [
  { label: 'Demo Student 1', id: '20261110000000001' },
  { label: 'Demo Student 2', id: '20261110000000002' },
];

export const StudentIdBar: React.FC = () => {
  const {
    studentId,
    setStudentId,
    clearStudentId,
    isValidStudentId,
    generateRandomStudentId
  } = useStudent();

  return (
    <div className="bg-white border-b border-navy-200/80 shadow-xs">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-3.5">
        <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-4">
          
          {/* Left: Input Box & Identity Status */}
          <div className="flex items-start sm:items-center space-x-3.5 flex-1 max-w-2xl">
            <div className={`p-2.5 rounded-xl border transition-all shrink-0 mt-0.5 sm:mt-0 ${
              isValidStudentId 
                ? 'bg-emerald-50 border-emerald-200 text-emerald-700 shadow-xs' 
                : studentId.length > 0 
                ? 'bg-amber-50 border-amber-200 text-amber-700' 
                : 'bg-navy-50 border-navy-200 text-navy-500'
            }`}>
              <ShieldCheck className="w-5 h-5" />
            </div>

            <div className="flex-1 w-full">
              <div className="flex items-center justify-between mb-1.5">
                <label 
                  htmlFor="student-id-input" 
                  className="block text-xs font-bold uppercase tracking-wider text-navy-800"
                >
                  Student Birth Registration Number (17-Digit Digital BRN)
                </label>
                <span className={`text-[11px] font-mono font-semibold px-2 py-0.5 rounded-full ${
                  isValidStudentId 
                    ? 'bg-emerald-100 text-emerald-800' 
                    : studentId.length > 0 
                    ? 'bg-amber-100 text-amber-800' 
                    : 'bg-navy-100 text-navy-600'
                }`}>
                  {studentId.length} / 17 digits
                </span>
              </div>

              <div className="relative rounded-lg shadow-2xs">
                <input
                  id="student-id-input"
                  type="text"
                  inputMode="numeric"
                  maxLength={17}
                  value={studentId}
                  onChange={(e) => setStudentId(e.target.value)}
                  placeholder="Enter 17-digit BRN (e.g., 20261110000000001)"
                  className={`block w-full font-mono text-sm sm:text-base rounded-lg px-3.5 py-2 border transition-all focus:outline-none focus:ring-2 ${
                    isValidStudentId
                      ? 'border-emerald-500 bg-emerald-50/20 text-emerald-950 font-bold focus:ring-emerald-500/20'
                      : studentId.length > 0
                      ? 'border-amber-400 bg-amber-50/20 text-navy-900 font-medium focus:ring-amber-500/20'
                      : 'border-navy-300 bg-white text-navy-900 placeholder-navy-400 focus:border-brand-600 focus:ring-brand-500/20'
                  }`}
                />
                {studentId.length > 0 && (
                  <button
                    type="button"
                    onClick={clearStudentId}
                    title="Clear ID"
                    className="absolute inset-y-0 right-0 pr-3 flex items-center text-navy-400 hover:text-navy-600 transition-colors"
                  >
                    <XCircle className="w-4 h-4" />
                  </button>
                )}
              </div>
            </div>
          </div>

          {/* Right: Quick Demo Fillers for Evaluators & Status Badges */}
          <div className="flex flex-wrap items-center gap-2 self-start lg:self-center">
            <span className="text-[11px] font-semibold text-navy-600 uppercase tracking-wider hidden sm:inline-block">
              Quick Test:
            </span>

            {DEMO_STUDENT_IDS.map((demo) => (
              <button
                key={demo.id}
                type="button"
                onClick={() => setStudentId(demo.id)}
                className={`text-xs font-medium px-2.5 py-1.5 rounded-lg border transition-all flex items-center gap-1.5 ${
                  studentId === demo.id
                    ? 'bg-brand-600 text-white border-brand-700 shadow-xs'
                    : 'bg-navy-50 text-navy-700 border-navy-200 hover:bg-navy-100 hover:border-navy-300'
                }`}
              >
                <Copy className="w-3 h-3 text-navy-400" />
                <span>{demo.label}</span>
              </button>
            ))}

            <button
              type="button"
              onClick={generateRandomStudentId}
              className="text-xs font-semibold px-2.5 py-1.5 rounded-lg bg-navy-800 text-white hover:bg-navy-900 border border-navy-900 transition-all flex items-center gap-1.5 shadow-2xs"
              title="Generate a realistic 17-digit birth certificate number"
            >
              <Wand2 className="w-3.5 h-3.5 text-brand-300" />
              <span>Random ID</span>
            </button>
          </div>

        </div>

        {/* Verification Context Alert */}
        <div className="mt-2.5 flex items-center justify-between text-xs text-navy-500 pt-2 border-t border-navy-100">
          <div className="flex items-center gap-1.5">
            <Info className="w-3.5 h-3.5 text-brand-600 shrink-0" />
            <span>
              Ministry of Education Rule: Exactly <strong>one seat hold</strong> allowed per student across all shift offerings.
            </span>
          </div>

          <div>
            {isValidStudentId ? (
              <span className="inline-flex items-center gap-1 font-semibold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-md border border-emerald-200">
                <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600" />
                Verified Student
              </span>
            ) : (
              <span className="inline-flex items-center gap-1 font-medium text-amber-700 bg-amber-50 px-2 py-0.5 rounded-md border border-amber-200">
                <AlertCircle className="w-3.5 h-3.5 text-amber-600" />
                Verification Pending
              </span>
            )}
          </div>
        </div>

      </div>
    </div>
  );
};
