import React, { useState } from 'react';
import { X, PlusCircle, AlertCircle, Loader2 } from 'lucide-react';
import { apiClient, ApiClientError } from '../services/apiClient';

interface AdminCreateClassModalProps {
  isOpen: boolean;
  onClose: () => void;
  onCreated: () => void;
}

export const AdminCreateClassModal: React.FC<AdminCreateClassModalProps> = ({
  isOpen,
  onClose,
  onCreated
}) => {
  const [name, setName] = useState('');
  const [totalSeats, setTotalSeats] = useState<number>(60);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!name.trim()) {
      setError('Class name cannot be empty.');
      return;
    }
    if (totalSeats < 1) {
      setError('Total seats must be at least 1.');
      return;
    }

    try {
      setIsSubmitting(true);
      setError(null);
      await apiClient.createClass(name.trim(), totalSeats);
      setName('');
      setTotalSeats(60);
      onCreated();
      onClose();
    } catch (err) {
      if (err instanceof ApiClientError) {
        setError(err.message);
      } else {
        setError('Failed to create class. Please try again.');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-navy-950/70 backdrop-blur-xs flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl border border-navy-200 w-full max-w-md overflow-hidden transform transition-all">
        {/* Modal Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-navy-100 bg-navy-50/70">
          <div className="flex items-center space-x-3">
            <div className="p-2.5 bg-brand-50 text-brand-600 rounded-xl border border-brand-200">
              <PlusCircle className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-navy-900 leading-snug">Create Class Offering</h3>
              <p className="text-xs text-navy-500">Configure academic offering and seat quota</p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="text-navy-400 hover:text-navy-600 p-1.5 rounded-lg hover:bg-navy-100 transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Modal Form */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {error && (
            <div className="p-3.5 bg-rose-50 border border-rose-200 rounded-xl flex items-start space-x-2 text-rose-800 text-xs">
              <AlertCircle className="w-4 h-4 mt-0.5 shrink-0 text-rose-600" />
              <span className="font-medium">{error}</span>
            </div>
          )}

          <div>
            <label htmlFor="class-name" className="block text-xs font-bold text-navy-800 uppercase tracking-wider mb-1.5">
              Class & Shift Name <span className="text-rose-500">*</span>
            </label>
            <input
              id="class-name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Class 8 - Morning Shift (Science)"
              className="w-full text-sm rounded-lg border border-navy-300 px-3.5 py-2.5 text-navy-900 placeholder-navy-400 focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-600 transition-all"
              disabled={isSubmitting}
              autoFocus
            />
          </div>

          <div>
            <label htmlFor="total-seats" className="block text-xs font-bold text-navy-800 uppercase tracking-wider mb-1.5">
              Total Seat Capacity <span className="text-rose-500">*</span>
            </label>
            <input
              id="total-seats"
              type="number"
              min={1}
              max={1000}
              value={totalSeats}
              onChange={(e) => setTotalSeats(parseInt(e.target.value, 10) || 0)}
              className="w-full text-sm rounded-lg border border-navy-300 px-3.5 py-2.5 text-navy-900 focus:outline-none focus:ring-2 focus:ring-brand-500/20 focus:border-brand-600 font-mono transition-all"
              disabled={isSubmitting}
            />
            <p className="text-[11px] text-navy-500 mt-1">
              Minimum 1 seat. Seats are allocated strictly First-Come, First-Served.
            </p>
          </div>

          <div className="pt-3 border-t border-navy-100 flex items-center justify-end space-x-2.5">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="px-4 py-2 text-xs font-semibold text-navy-600 hover:text-navy-900 hover:bg-navy-50 rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting}
              className="px-5 py-2.5 bg-brand-600 hover:bg-brand-700 active:bg-brand-800 text-white text-xs font-bold rounded-lg shadow-xs transition-all flex items-center space-x-1.5 disabled:opacity-50"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="w-3.5 h-3.5 animate-spin" />
                  <span>Creating Offering...</span>
                </>
              ) : (
                <span>Publish Offering</span>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
