import React, { useState } from 'react';
import { School, BookOpen, GraduationCap } from 'lucide-react';
import { StudentProvider } from './context/StudentContext';
import { NotificationProvider } from './context/NotificationContext';
import { StudentIdBar } from './components/StudentIdBar';
import { CatalogPage } from './pages/CatalogPage';
import { MyApplicationsPage } from './pages/MyApplicationsPage';
import { ActiveHoldModal } from './components/ActiveHoldModal';
import { HoldResponse } from './types/api';

type NavTab = 'catalog' | 'applications';

const AppContent: React.FC = () => {
  const [activeTab, setActiveTab] = useState<NavTab>('catalog');
  const [activeHold, setActiveHold] = useState<HoldResponse | null>(null);
  const [isHoldModalOpen, setIsHoldModalOpen] = useState(false);

  const handleHoldCreated = (hold: HoldResponse) => {
    setActiveHold(hold);
    setIsHoldModalOpen(true);
  };

  return (
    <div className="min-h-screen bg-slate-50 flex flex-col">
      {/* Top Application Header */}
      <header className="bg-white border-b border-slate-200 sticky top-0 z-30 shadow-2xs">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-indigo-600 rounded-xl text-white shadow-sm">
              <School className="w-6 h-6" />
            </div>
            <div>
              <h1 className="text-lg sm:text-xl font-black text-slate-900 leading-tight tracking-tight">
                School Admission System
              </h1>
              <p className="text-xs text-slate-500 hidden sm:block">
                Online Seat Allocation & Reservation Portal · Onnorokom Projukti
              </p>
            </div>
          </div>

          {/* Navigation Tabs */}
          <nav className="flex items-center space-x-1 sm:space-x-2 bg-slate-100 p-1 rounded-xl border border-slate-200">
            <button
              type="button"
              onClick={() => setActiveTab('catalog')}
              className={`flex items-center gap-2 px-3 sm:px-4 py-1.5 rounded-lg text-xs sm:text-sm font-semibold transition-all ${
                activeTab === 'catalog'
                  ? 'bg-white text-indigo-700 shadow-xs'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-200/60'
              }`}
            >
              <BookOpen className="w-4 h-4" />
              <span>Classes</span>
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('applications')}
              className={`flex items-center gap-2 px-3 sm:px-4 py-1.5 rounded-lg text-xs sm:text-sm font-semibold transition-all ${
                activeTab === 'applications'
                  ? 'bg-white text-indigo-700 shadow-xs'
                  : 'text-slate-600 hover:text-slate-900 hover:bg-slate-200/60'
              }`}
            >
              <GraduationCap className="w-4 h-4" />
              <span>My Applications</span>
            </button>
          </nav>
        </div>
      </header>

      {/* Persistent Top Student Identity Bar */}
      <StudentIdBar />

      {/* Main Page Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 flex-1 w-full">
        {activeTab === 'catalog' ? (
          <CatalogPage onHoldCreated={handleHoldCreated} />
        ) : (
          <MyApplicationsPage onNavigateToCatalog={() => setActiveTab('catalog')} />
        )}
      </main>

      {/* Active Hold Countdown & Payment Modal */}
      <ActiveHoldModal
        hold={activeHold}
        isOpen={isHoldModalOpen}
        onClose={() => setIsHoldModalOpen(false)}
        onConfirmed={() => {
          // Keep modal open to show celebratory confirmed state
        }}
        onCancelled={() => {
          setActiveHold(null);
          setIsHoldModalOpen(false);
        }}
      />

      {/* Footer */}
      <footer className="border-t border-slate-200 bg-white py-4 text-center text-xs text-slate-500">
        &copy; 2026 Onnorokom Projukti Limited · School Admission System · FCFS Guaranteed
      </footer>
    </div>
  );
};

export const App: React.FC = () => {
  return (
    <StudentProvider>
      <NotificationProvider>
        <AppContent />
      </NotificationProvider>
    </StudentProvider>
  );
};

export default App;
