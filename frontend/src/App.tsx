import React from 'react';
import { School } from 'lucide-react';
import { apiClient } from './services/apiClient';

export const App: React.FC = () => {
  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      <header className="bg-white border-b border-slate-200 sticky top-0 z-30 shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-indigo-600 rounded-lg text-white">
              <School className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-slate-900 leading-tight">
                School Admission System
              </h1>
              <p className="text-xs text-slate-500">
                Online Seat Allocation & Reservation Portal
              </p>
            </div>
          </div>
          <div className="text-xs text-slate-400 font-mono">
            API: {apiClient.getBaseUrl()}
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 flex-1 w-full">
        <div className="bg-white rounded-xl shadow-sm border border-slate-200 p-6">
          <h2 className="text-lg font-semibold text-slate-900 mb-2">
            Welcome to the Admission Portal
          </h2>
          <p className="text-slate-600 text-sm">
            Frontend client initialized with React 18, TypeScript, and Tailwind CSS.
          </p>
        </div>
      </main>

      <footer className="border-t border-slate-200 bg-white py-4 text-center text-xs text-slate-500">
        &copy; 2026 Onnorokom Projukti Limited - School Admission System
      </footer>
    </div>
  );
};

export default App;
