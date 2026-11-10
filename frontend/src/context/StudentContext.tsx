import React, { createContext, useContext, useState, useEffect } from 'react';
import { apiClient } from '../services/apiClient';

interface StudentContextType {
  studentId: string;
  setStudentId: (id: string) => void;
  clearStudentId: () => void;
  isValidStudentId: boolean;
  generateRandomStudentId: () => void;
}

const STORAGE_KEY = 'studentId';
const STUDENT_ID_REGEX = /^\d{17}$/;

const StudentContext = createContext<StudentContextType | undefined>(undefined);

export const StudentProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [studentId, setStudentIdState] = useState<string>(() => {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      return saved ? saved.trim() : '';
    } catch {
      return '';
    }
  });

  // Synchronize with API client and localStorage
  useEffect(() => {
    if (studentId) {
      try {
        localStorage.setItem(STORAGE_KEY, studentId);
      } catch (e) {
        console.warn('Failed to save studentId to localStorage:', e);
      }
      apiClient.setStudentId(studentId);
    } else {
      try {
        localStorage.removeItem(STORAGE_KEY);
      } catch (e) {
        console.warn('Failed to remove studentId from localStorage:', e);
      }
      apiClient.setStudentId(null);
    }
  }, [studentId]);

  const setStudentId = (id: string) => {
    // Sanitize: allow only digits and trim at 17 characters
    const sanitized = id.replace(/\D/g, '').slice(0, 17);
    setStudentIdState(sanitized);
  };

  const clearStudentId = () => {
    setStudentIdState('');
  };

  const generateRandomStudentId = () => {
    // Generate valid 17-digit birth certificate number starting with realistic year (e.g., 2015)
    const birthYear = '2015';
    const districtCode = '26'; // Dhaka
    const randomDigits = Math.floor(Math.random() * 100000000000)
      .toString()
      .padStart(11, '0');
    const generated = `${birthYear}${districtCode}${randomDigits}`.slice(0, 17);
    setStudentIdState(generated);
  };

  const isValidStudentId = STUDENT_ID_REGEX.test(studentId);

  return (
    <StudentContext.Provider
      value={{
        studentId,
        setStudentId,
        clearStudentId,
        isValidStudentId,
        generateRandomStudentId
      }}
    >
      {children}
    </StudentContext.Provider>
  );
};

export const useStudent = (): StudentContextType => {
  const context = useContext(StudentContext);
  if (!context) {
    throw new Error('useStudent must be used within a StudentProvider');
  }
  return context;
};
