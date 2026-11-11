import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useHoldTimer } from '../hooks/useHoldTimer';

describe('useHoldTimer Hook', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('formats initial remaining duration into MM:SS correctly', () => {
    // 5 minutes from now
    const now = Date.now();
    const expiry = new Date(now + 300 * 1000).toISOString();

    const { result } = renderHook(() => useHoldTimer(expiry, 300));

    expect(result.current.isExpired).toBe(false);
    expect(result.current.formattedTime).toBe('05:00');
    expect(result.current.secondsRemaining).toBe(300);
  });

  it('decrements secondsRemaining and formattedTime with every elapsed second', () => {
    const now = Date.now();
    const expiry = new Date(now + 65 * 1000).toISOString(); // 1 minute 5 seconds

    const { result } = renderHook(() => useHoldTimer(expiry, 65));

    expect(result.current.formattedTime).toBe('01:05');

    // Advance 5 seconds
    act(() => {
      vi.advanceTimersByTime(5000);
    });

    expect(result.current.formattedTime).toBe('01:00');
    expect(result.current.secondsRemaining).toBe(60);
    expect(result.current.isExpired).toBe(false);
  });

  it('triggers isExpired = true and sets 00:00 when countdown hits 0', () => {
    const now = Date.now();
    const expiry = new Date(now + 3 * 1000).toISOString(); // 3 seconds remaining

    const { result } = renderHook(() => useHoldTimer(expiry, 3));

    expect(result.current.formattedTime).toBe('00:03');
    expect(result.current.isExpired).toBe(false);

    // Advance 4 seconds to pass expiration point
    act(() => {
      vi.advanceTimersByTime(4000);
    });

    expect(result.current.formattedTime).toBe('00:00');
    expect(result.current.secondsRemaining).toBe(0);
    expect(result.current.isExpired).toBe(true);
  });

  it('handles null, undefined or empty expiresAt safely', () => {
    const { result: resNull } = renderHook(() => useHoldTimer(null));
    expect(resNull.current.isExpired).toBe(true);
    expect(resNull.current.formattedTime).toBe('00:00');

    const { result: resUndefined } = renderHook(() => useHoldTimer(undefined));
    expect(resUndefined.current.isExpired).toBe(true);
    expect(resUndefined.current.formattedTime).toBe('00:00');
  });
});
