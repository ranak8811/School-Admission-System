import React, { useState } from 'react';
import { 
  BookOpen, 
  GraduationCap, 
  PhoneCall, 
  Mail, 
  ShieldCheck, 
  ExternalLink,
  Server
} from 'lucide-react';
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
    <div className="min-h-screen bg-navy-50 flex flex-col font-sans text-navy-900 selection:bg-brand-100 selection:text-brand-900">
      
      {/* 1. Topmost Government & Institutional Bar */}
      <div className="bg-navy-950 text-navy-300 text-[11px] border-b border-navy-800/80 py-1.5 px-4 sm:px-6 lg:px-8">
        <div className="max-w-7xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-1">
          <div className="flex items-center gap-2">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 inline-block" />
            <span className="font-medium text-navy-200">
              Session 2026–2027 Central Admission Portal • OnnoRokom Projukti Limited
            </span>
          </div>

          <div className="flex items-center gap-4 text-navy-400">
            <span className="flex items-center gap-1 hover:text-navy-200 transition-colors">
              <PhoneCall className="w-3 h-3 text-brand-400" />
              <span>Helpline: 16297</span>
            </span>
            <span className="hidden md:flex items-center gap-1 hover:text-navy-200 transition-colors">
              <Mail className="w-3 h-3 text-brand-400" />
              <span>support@onnorokomprojukti.com</span>
            </span>
            <span className="flex items-center gap-1 text-emerald-400 font-mono">
              <Server className="w-3 h-3" />
              <span>FCFS Server Active</span>
            </span>
          </div>
        </div>
      </div>

      {/* 2. Main Enterprise Header */}
      <header className="bg-white border-b border-navy-200 sticky top-0 z-30 shadow-xs">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-18 flex items-center justify-between">
          
          {/* Brand Logo & Title */}
          <div className="flex items-center space-x-3.5">
            {/* OnnoRokom signature geometric logo mark */}
            <div className="w-10 h-10 rounded-xl bg-gradient-to-br from-brand-600 via-brand-700 to-navy-900 flex items-center justify-center text-white shadow-xs">
              <div className="font-black text-lg tracking-tighter transform -rotate-6">
                অ
              </div>
            </div>

            <div>
              <div className="flex items-center gap-2">
                <span className="text-xs font-black tracking-widest text-brand-700 uppercase">
                  ONNOROKOM PROJUKTI
                </span>
                <span className="text-[10px] bg-navy-100 text-navy-700 px-1.5 py-0.2 rounded font-semibold uppercase">
                  Enterprise
                </span>
              </div>
              <h1 className="text-lg sm:text-xl font-black text-navy-900 leading-none tracking-tight">
                Smart Admission Pro
              </h1>
            </div>
          </div>

          {/* Navigation Tabs */}
          <nav className="flex items-center space-x-1 sm:space-x-1.5 bg-navy-50 p-1.5 rounded-xl border border-navy-200/80">
            <button
              type="button"
              onClick={() => setActiveTab('catalog')}
              className={`flex items-center gap-2 px-3.5 sm:px-4 py-2 rounded-lg text-xs sm:text-sm font-bold transition-all ${
                activeTab === 'catalog'
                  ? 'bg-white text-brand-700 shadow-2xs border border-navy-200/60'
                  : 'text-navy-600 hover:text-navy-900 hover:bg-navy-100/60'
              }`}
            >
              <BookOpen className="w-4 h-4 text-brand-600" />
              <span>Class Catalog</span>
            </button>

            <button
              type="button"
              onClick={() => setActiveTab('applications')}
              className={`flex items-center gap-2 px-3.5 sm:px-4 py-2 rounded-lg text-xs sm:text-sm font-bold transition-all ${
                activeTab === 'applications'
                  ? 'bg-white text-brand-700 shadow-2xs border border-navy-200/60'
                  : 'text-navy-600 hover:text-navy-900 hover:bg-navy-100/60'
              }`}
            >
              <GraduationCap className="w-4 h-4 text-brand-600" />
              <span>My Applications</span>
            </button>
          </nav>
        </div>
      </header>

      {/* 3. Persistent Top Student Identity Bar */}
      <StudentIdBar />

      {/* 4. Main Page Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 flex-1 w-full">
        {activeTab === 'catalog' ? (
          <CatalogPage onHoldCreated={handleHoldCreated} />
        ) : (
          <MyApplicationsPage onNavigateToCatalog={() => setActiveTab('catalog')} />
        )}
      </main>

      {/* 5. Active Hold Countdown & Payment Modal */}
      <ActiveHoldModal
        hold={activeHold}
        isOpen={isHoldModalOpen}
        onClose={() => setIsHoldModalOpen(false)}
        onConfirmed={() => {
          // Modal handles confirmation celebratory state
        }}
        onCancelled={() => {
          setActiveHold(null);
          setIsHoldModalOpen(false);
        }}
      />

      {/* 6. Human Enterprise Footer */}
      <footer className="border-t border-navy-200 bg-white text-navy-600 mt-auto">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-8 mb-8 text-xs">
            
            {/* Col 1: About OnnoRokom */}
            <div className="space-y-3 md:col-span-2">
              <div className="flex items-center gap-2">
                <div className="w-6 h-6 rounded-lg bg-brand-600 text-white flex items-center justify-center font-bold text-xs">
                  অ
                </div>
                <span className="font-black text-sm text-navy-900 tracking-wider">
                  অন্যরকম প্রযুক্তি লিমিটেড (OnnoRokom Projukti Limited)
                </span>
              </div>
              <p className="text-navy-500 leading-relaxed max-w-md">
                A pioneer in technological innovation in Bangladesh. Designing and deploying reliable 
                high-concurrency platforms, smart campus education management systems, and e-governance solutions.
              </p>
              <p className="text-navy-400 text-[11px]">
                Registered Office: Concord Royal Court (3rd Floor), 40/7-A Dhanmondi / Kawran Bazar, Dhaka-1215, Bangladesh.
              </p>
            </div>

            {/* Col 2: Group Sister Concerns */}
            <div className="space-y-2">
              <span className="font-bold text-navy-900 uppercase tracking-wider text-[11px] block">
                OnnoRokom Group
              </span>
              <ul className="space-y-1.5 text-navy-500">
                <li><a href="https://onnorokomprojukti.com" target="_blank" rel="noreferrer" className="hover:text-brand-600 transition-colors flex items-center gap-1">OnnoRokom Projukti Ltd <ExternalLink className="w-2.5 h-2.5" /></a></li>
                <li><a href="https://rokomari.com" target="_blank" rel="noreferrer" className="hover:text-brand-600 transition-colors flex items-center gap-1">Rokomari.com <ExternalLink className="w-2.5 h-2.5" /></a></li>
                <li><a href="https://techshopbd.com" target="_blank" rel="noreferrer" className="hover:text-brand-600 transition-colors flex items-center gap-1">TechShopBD <ExternalLink className="w-2.5 h-2.5" /></a></li>
                <li><span className="text-navy-400">OnnoRokom Web Services</span></li>
              </ul>
            </div>

            {/* Col 3: Architectural Guarantee */}
            <div className="space-y-2">
              <span className="font-bold text-navy-900 uppercase tracking-wider text-[11px] block">
                System Guarantees
              </span>
              <ul className="space-y-1.5 text-navy-500">
                <li className="flex items-center gap-1.5 text-emerald-700 font-semibold">
                  <ShieldCheck className="w-3.5 h-3.5" />
                  <span>Strict Zero Overselling Guarantee</span>
                </li>
                <li><span>Pessimistic Row-Level Lock</span></li>
                <li><span>10-Minute Atomic Hold Queue</span></li>
                <li><span>Idempotent Confirmation</span></li>
              </ul>
            </div>

          </div>

          <div className="pt-6 border-t border-navy-100 flex flex-col sm:flex-row items-center justify-between gap-2 text-[11px] text-navy-400">
            <div>
              &copy; {new Date().getFullYear()} OnnoRokom Projukti Limited. All Rights Reserved.
            </div>
            <div className="font-mono text-navy-400">
              Engine: .NET 8 Web API + PostgreSQL 16 + React 18 / Vite
            </div>
          </div>
        </div>
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
