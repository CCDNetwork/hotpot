import { useEffect, useState } from 'react';
import { CalendarIcon } from '@radix-ui/react-icons';
import { format, parseISO } from 'date-fns';
import { DateRange } from 'react-day-picker';

import { Button } from '@/components/ui/button';
import { Calendar } from '@/components/ui/calendar';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { cn } from '@/helpers/utils';

type Props = {
  active: boolean;
  from?: string;
  to?: string;
  onApply: (from: string, to: string) => void;
};

/**
 * "Custom" period chip that opens a two-month range picker. Emits ISO
 * YYYY-MM-DD strings (calendar days, no timezone) so the server can anchor
 * them to UTC — matches the InforEuro-active-rate FX convention.
 */
export const CustomRangeButton = ({ active, from, to, onApply }: Props) => {
  const initial: DateRange | undefined = from
    ? { from: parseISO(from), to: to ? parseISO(to) : undefined }
    : undefined;
  const [range, setRange] = useState<DateRange | undefined>(initial);
  const [open, setOpen] = useState(false);

  // Re-sync from external state (e.g. deep-link, reset) so the picker's
  // opinion doesn't drift from the URL / query state.
  useEffect(() => {
    setRange(initial);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [from, to]);

  const label =
    active && range?.from && range?.to
      ? `${format(range.from, 'MMM d, y')} — ${format(range.to, 'MMM d, y')}`
      : 'Custom';

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="outline"
          className={cn(
            'h-9 px-3 text-sm font-medium',
            active
              ? 'border-transparent bg-background text-foreground shadow'
              : 'border-muted bg-muted text-muted-foreground hover:text-foreground'
          )}
        >
          <CalendarIcon className="mr-2 h-4 w-4" />
          {label}
        </Button>
      </PopoverTrigger>
      <PopoverContent align="start" className="w-auto p-0">
        <Calendar
          initialFocus
          mode="range"
          numberOfMonths={2}
          defaultMonth={range?.from}
          selected={range}
          onSelect={(next) => {
            setRange(next);
            if (next?.from && next?.to) {
              onApply(
                format(next.from, 'yyyy-MM-dd'),
                format(next.to, 'yyyy-MM-dd')
              );
              setOpen(false);
            }
          }}
        />
      </PopoverContent>
    </Popover>
  );
};
