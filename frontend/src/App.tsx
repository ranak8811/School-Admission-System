import React, { useState } from 'react';
import { School } from 'lucide-react';
import { apiClient } from './services/apiClient';
import { StudentProvider } from './context/StudentContext';
import { StudentIdBar } from './components/StudentIdBar';
import { CatalogPage } from './pages/CatalogPage';
import { HoldResponse } from './types/api';

const AppContent: React.FC = () => {
  const [, setActiveHold] = useState<HoldResponse | null>(null);

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

      {/* Persistent Top Student Identity Bar */}
      <StudentIdBar />

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 flex-1 w-full">
        <CatalogPage onHoldCreated={(hold) => setActiveHold(hold)} />
      </main>

      <footer className="border-t border-slate-200 bg-white py-4 text-center text-xs text-slate-500">
        &copy; 2026 Onnorokom Projukti Limited - School Admission System
      </footer>
    </div>
  );
};

export const App: React.FC = () => {
  return (
    <StudentProvider>
      <AppContent />
    </StudentProvider>
  );
};

export default App;
