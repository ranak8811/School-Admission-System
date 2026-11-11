import React, { createContext, useContext, useState, useCallback, ReactNode } from 'react';
import { ApiClientError } from '../services/apiClient';
import { AlertCircle, CheckCircle2, AlertTriangle, Info, X } from 'lucide-react';

export type NotificationType = 'error' | 'success' | 'warning' | 'info';

export interface NotificationItem {
  id: string;
  type: NotificationType;
  title: string;
  message: string;
  duration?: number;
}

interface NotificationContextType {
  notifications: NotificationItem[];
  showNotification: (type: NotificationType, title: string, message: string, duration?: number) => void;
  showError: (err: unknown, fallbackMessage?: string) => void;
  showSuccess: (message: string, title?: string) => void;
  dismissNotification: (id: string) => void;
  clearAll: () => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export const NotificationProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);

  const dismissNotification = useCallback((id: string) => {
    setNotifications((prev) => prev.filter((n) => n.id !== id));
  }, []);

  const showNotification = useCallback(
    (type: NotificationType, title: string, message: string, duration = 6000) => {
      const id = `${Date.now()}-${Math.random().toString(36).substring(2, 9)}`;
      const newItem: NotificationItem = { id, type, title, message, duration };

      setNotifications((prev) => [...prev, newItem]);

      if (duration > 0) {
        setTimeout(() => {
          dismissNotification(id);
        }, duration);
      }
    },
    [dismissNotification]
  );

  const showError = useCallback(
    (err: unknown, fallbackMessage = 'An unexpected error occurred.') => {
      let title = 'Error';
      let message = fallbackMessage;

      if (err instanceof ApiClientError) {
        title = `Error (${err.status})`;
        if (err.status === 409) {
          title = 'Seat Unavailable / কনফ্লিক্ট';
          message = err.message || 'Seat is already reserved, expired, or capacity is full.';
        } else if (err.status === 400) {
          title = 'Invalid Request / ভুল অনুরোধ';
          message = err.message || 'Please check your inputs and 17-digit Student ID.';
        } else if (err.status === 404) {
          title = 'Not Found / পাওয়া যায়নি';
          message = err.message || 'Requested resource could not be found.';
        } else if (err.status === 403) {
          title = 'Forbidden / অনুমতি নেই';
          message = err.message || 'You do not have permission to modify this reservation.';
        } else if (err.status >= 500) {
          title = 'Server Error / সার্ভার ত্রুটি';
          message = 'The server encountered an error. Please try again in a moment.';
        } else {
          message = err.message;
        }
      } else if (err instanceof TypeError && err.message.toLowerCase().includes('fetch')) {
        title = 'Network Error / সংযোগ ব্যর্থ';
        message = 'Unable to connect to the backend server. Please verify your connection or backend status.';
      } else if (err instanceof Error) {
        message = err.message;
      }

      showNotification('error', title, message, 8000);
    },
    [showNotification]
  );

  const showSuccess = useCallback(
    (message: string, title = 'Success / সফল') => {
      showNotification('success', title, message, 5000);
    },
    [showNotification]
  );

  const clearAll = useCallback(() => {
    setNotifications([]);
  }, []);

  return (
    <NotificationContext.Provider
      value={{
        notifications,
        showNotification,
        showError,
        showSuccess,
        dismissNotification,
        clearAll
      }}
    >
      {children}

      {/* Floating Global Notification Container */}
      <div
        className="fixed top-4 right-4 z-50 flex flex-col gap-2 max-w-md w-full pointer-events-none px-4 sm:px-0"
        aria-live="assertive"
      >
        {notifications.map((item) => (
          <div
            key={item.id}
            role="alert"
            className={`pointer-events-auto rounded-lg shadow-lg border p-4 transition-all duration-300 transform translate-y-0 flex items-start gap-3 ${
              item.type === 'error'
                ? 'bg-red-50 border-red-200 text-red-900'
                : item.type === 'success'
                ? 'bg-emerald-50 border-emerald-200 text-emerald-900'
                : item.type === 'warning'
                ? 'bg-amber-50 border-amber-200 text-amber-900'
                : 'bg-blue-50 border-blue-200 text-blue-900'
            }`}
          >
            <div className="shrink-0 mt-0.5">
              {item.type === 'error' && <AlertCircle className="w-5 h-5 text-red-600" />}
              {item.type === 'success' && <CheckCircle2 className="w-5 h-5 text-emerald-600" />}
              {item.type === 'warning' && <AlertTriangle className="w-5 h-5 text-amber-600" />}
              {item.type === 'info' && <Info className="w-5 h-5 text-blue-600" />}
            </div>

            <div className="flex-1 text-sm">
              <p className="font-semibold">{item.title}</p>
              <p className="mt-0.5 text-xs sm:text-sm opacity-90 break-words">{item.message}</p>
            </div>

            <button
              type="button"
              onClick={() => dismissNotification(item.id)}
              className="shrink-0 rounded p-1 hover:bg-black/5 transition text-slate-400 hover:text-slate-600"
              aria-label="Close notification"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        ))}
      </div>
    </NotificationContext.Provider>
  );
};

export function useNotification(): NotificationContextType {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotification must be used within a NotificationProvider');
  }
  return context;
}
