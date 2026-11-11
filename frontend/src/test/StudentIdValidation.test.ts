import { describe, it, expect } from 'vitest';

describe('Student ID Validation & Sanitization', () => {
  const sanitizeStudentId = (value: string): string => {
    // Strips non-digit characters and truncates to 17 digits
    return value.replace(/\D/g, '').slice(0, 17);
  };

  const isValidStudentId = (id: string): boolean => {
    return /^\d{17}$/.test(id.trim());
  };

  it('validates 17 numeric digits successfully', () => {
    expect(isValidStudentId('20261110000000001')).toBe(true);
    expect(isValidStudentId('00000000000000000')).toBe(true);
    expect(isValidStudentId('99999999999999999')).toBe(true);
  });

  it('rejects student IDs with length not equal to 17 digits', () => {
    expect(isValidStudentId('1234567890123456')).toBe(false); // 16 digits
    expect(isValidStudentId('123456789012345678')).toBe(false); // 18 digits
    expect(isValidStudentId('')).toBe(false);
    expect(isValidStudentId('   ')).toBe(false);
  });

  it('rejects student IDs with non-numeric characters', () => {
    expect(isValidStudentId('2026111000000000A')).toBe(false);
    expect(isValidStudentId('2026-11-100000000')).toBe(false);
    expect(isValidStudentId('2026 1110 0000 001')).toBe(false);
  });

  it('sanitizes input by stripping non-digit characters and limiting to 17 digits', () => {
    expect(sanitizeStudentId('2026-1110-0000-00001')).toBe('20261110000000001');
    expect(sanitizeStudentId('abc2026xyz1110000000001 extra')).toBe('20261110000000001');
    expect(sanitizeStudentId('12345678901234567899999')).toBe('12345678901234567'); // Truncated to 17
  });
});
