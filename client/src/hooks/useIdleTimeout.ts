import { useEffect, useRef } from 'react';

const ACTIVITY_EVENTS: (keyof DocumentEventMap)[] = [
  'mousemove',
  'mousedown',
  'keydown',
  'scroll',
  'touchstart',
];

export const useIdleTimeout = (timeout: number, onIdle: () => void) => {
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    const resetTimer = () => {
      if (timerRef.current) clearTimeout(timerRef.current);
      timerRef.current = setTimeout(onIdle, timeout);
    };

    ACTIVITY_EVENTS.forEach((event) =>
      document.addEventListener(event, resetTimer)
    );

    resetTimer();

    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
      ACTIVITY_EVENTS.forEach((event) =>
        document.removeEventListener(event, resetTimer)
      );
    };
  }, [timeout, onIdle]);
};
