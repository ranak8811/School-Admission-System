import React from 'react';
import { AlertCircle, RefreshCw, XCircle } from 'lucide-react';

interface ErrorBannerProps {
  title?: string;
  message: string;
  onRetry?: () => void;
  onDismiss?: () => void;
}

export const ErrorBanner: React.FC<ErrorBannerProps> = ({
  title = 'Something went wrong / একটি সমস্যা হয়েছে',
  message,
  onRetry,
  onDismiss
}) => {
  return (
    <div
      role="alert"
      className="rounded-xl border border-red-200 bg-red-50/90 p-4 sm:p-5 shadow-sm text-red-900 transition"
    >
      <div className="flex items-start gap-3">
        <div className="shrink-0 mt-0.5">
          <AlertCircle className="w-5 h-5 text-red-600" />
        </div>

        <div className="flex-1 min-w-0">
          <h4 className="font-semibold text-sm sm:text-base text-red-900">{title}</h4>
          <p className="mt-1 text-xs sm:text-sm text-red-700 leading-relaxed break-words">{message}</p>

          {onRetry && (
            <div className="mt-3">
              <button
                type="button"
                onClick={onRetry}
                className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-red-600 hover:bg-red-700 active:bg-red-800 text-white text-xs font-medium rounded-lg shadow-sm transition"
              >
                <RefreshCw className="w-3.5 h-3.5" />
                Retry / পুনরায় চেষ্টা করুন
              </button>
            </div>
          )}
        </div>

        {onDismiss && (
          <button
            type="button"
            onClick={onDismiss}
            className="shrink-0 p-1 text-red-400 hover:text-red-700 rounded transition"
            aria-label="Dismiss error"
          >
            <XCircle className="w-5 h-5" />
          </button>
        )}
      </div>
    </div>
  );
};
