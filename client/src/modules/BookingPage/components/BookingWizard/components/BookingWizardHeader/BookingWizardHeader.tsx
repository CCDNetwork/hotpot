import { cn } from '@/helpers/utils';
import { CheckCircleIcon } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';

const BOOKING_STEPS: { step: string }[] = [
  { step: 'Upload data' },
  { step: 'File check' },
  { step: 'Booking check' },
];

export const BookingWizardHeader = ({
  currentStep,
}: {
  currentStep: number;
}) => {
  const [stepUnderlineWidth, setStepUnderlineWidth] = useState(0);
  const [stepUnderlineOffset, stepTabUnderlineOffset] = useState(0);

  const tabsRef = useRef<HTMLDivElement[]>([]);

  useEffect(() => {
    function setTabPosition() {
      const currentTab = tabsRef.current[currentStep];
      stepTabUnderlineOffset(currentTab?.offsetLeft ?? 0);
      setStepUnderlineWidth(currentTab?.clientWidth ?? 0);
    }

    setTabPosition();
    window.addEventListener('resize', setTabPosition);

    return () => window.removeEventListener('resize', setTabPosition);
  }, [currentStep]);

  return (
    <div className="relative w-full p-2 border-b flex justify-evenly items-start gap-2 bg-muted/50 z-10">
      {BOOKING_STEPS.map(({ step }, stepIndex) => (
        <div
          ref={(el) => (tabsRef.current[stepIndex + 1] = el as HTMLDivElement)}
          key={step}
          className={cn(
            'relative px-2 text-sm pb-2 pt-1 rounded-md w-full transition-opacity duration-300',
            {
              'opacity-60': stepIndex + 1 < currentStep,
            }
          )}
        >
          <span className="font-medium flex items-center">
            {`Step ${stepIndex + 1}`}
            {stepIndex + 1 < currentStep && (
              <CheckCircleIcon className="transition-all duration-150 animate-appear ml-1.5 h-4 w-4 text-green-600" />
            )}
          </span>
          <p className="font-medium text-muted-foreground text-xs">{step}</p>
        </div>
      ))}
      <span
        className="absolute bottom-0 block h-1 rounded-lg bg-primary transition-all duration-300"
        style={{ left: stepUnderlineOffset, width: stepUnderlineWidth }}
      />
    </div>
  );
};
