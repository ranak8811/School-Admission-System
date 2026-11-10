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
    <div className="fixed inset-0 z-50 overflow-y-auto bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-xl border border-slate-200 w-full max-w-md overflow-hidden transform transition-all">
        {/* Modal Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100 bg-slate-50/50">
          <div className="flex items-center space-x-2.5">
            <div className="p-2 bg-indigo-50 text-indigo-600 rounded-lg">
              <PlusCircle className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-base font-bold text-slate-900">Create New Class</h3>
              <p className="text-xs text-slate-500">Add an academic class and set initial seat capacity</p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Modal Form */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {error && (
            <div className="p-3 bg-rose-50 border border-rose-200 rounded-lg flex items-start space-x-2 text-rose-700 text-xs">
              <AlertCircle className="w-4 h-4 mt-0.5 shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div>
            <label htmlFor="class-name" className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Class & Shift Name <span className="text-rose-500">*</span>
            </label>
            <input
              id="class-name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Class 8 - Morning Shift"
              className="w-full text-sm rounded-lg border border-slate-300 px-3 py-2 text-slate-900 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
              disabled={isSubmitting}
              autoFocus
            />
          </div>

          <div>
            <label htmlFor="total-seats" className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1">
              Total Seat Capacity <span className="text-rose-500">*</span>
            </label>
            <input
              id="total-seats"
              type="number"
              min={1}
              max={1000}
              value={totalSeats}
              onChange={(e) => setTotalSeats(Math.max(1, parseInt(e.target.value) || 1))}
              className="w-full text-sm rounded-lg border border-slate-300 px-3 py-2 text-slate-900 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 font-mono"
              disabled={isSubmitting}
            />
            <p className="text-xs text-slate-400 mt-1">Maximum allowed applicants for this shift.</p>
          </div>

          {/* Modal Footer */}
          <div className="pt-2 flex items-center justify-end space-x-3">
            <button
              type="button"
              onClick={onClose}
              disabled={isSubmitting}
              className="px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-100 rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting || !name.trim()}
              className="px-4 py-2 text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg shadow-sm transition-colors flex items-center space-x-2 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  <span>Saving...</span>
                </>
              ) : (
                <span>Create Offering</span>
              )}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
