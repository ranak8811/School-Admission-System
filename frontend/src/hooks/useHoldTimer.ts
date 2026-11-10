import { useState, useEffect } from 'react';

interface UseHoldTimerResult {
  secondsRemaining: number;
  formattedTime: string;
  isExpired: boolean;
  totalDurationSeconds: number;
  percentRemaining: number;
}

export function useHoldTimer(expiresAt: string, defaultDurationSeconds = 600): UseHoldTimerResult {
  const calculateRemaining = () => {
    try {
      const expiryMs = new Date(expiresAt).getTime();
      const nowMs = Date.now();
      const diffSec = Math.max(0, Math.floor((expiryMs - nowMs) / 1000));
      return isNaN(diffSec) ? 0 : diffSec;
    } catch {
      return 0;
    }
  };

  const [secondsRemaining, setSecondsRemaining] = useState<number>(calculateRemaining);

  useEffect(() => {
    // Recalculate immediately whenever expiresAt changes
    setSecondsRemaining(calculateRemaining());

    const interval = setInterval(() => {
      const remaining = calculateRemaining();
      setSecondsRemaining(remaining);

      if (remaining <= 0) {
        clearInterval(interval);
      }
    }, 1000);

    return () => {
      clearInterval(interval);
    };
  }, [expiresAt]);

  const minutes = Math.floor(secondsRemaining / 60);
  const seconds = secondsRemaining % 60;
  const formattedTime = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  const isExpired = secondsRemaining <= 0;
  const percentRemaining = Math.min(100, Math.max(0, Math.round((secondsRemaining / defaultDurationSeconds) * 100)));

  return {
    secondsRemaining,
    formattedTime,
    isExpired,
    totalDurationSeconds: defaultDurationSeconds,
    percentRemaining
  };
}
