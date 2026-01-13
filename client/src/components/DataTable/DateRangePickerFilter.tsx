import { useState } from 'react';
import { XIcon } from 'lucide-react';
import { CalendarIcon } from '@radix-ui/react-icons';
import { format } from 'date-fns';
import { DateRange } from 'react-day-picker';

import { Button } from '@/components/ui/button';
import { Calendar } from '@/components/ui/calendar';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import { cn } from '@/helpers/utils';

import { Separator } from '../ui/separator';

export const DateRangePickerFilter = ({
  className,
  placeholder,
  setCurrentFilters,
  filterNameFrom = 'createdAt[gt]',
  filterNameTo = 'createdAt[lt]',
}: React.HTMLAttributes<HTMLDivElement> & {
  placeholder?: string;
  filterNameFrom?: string;
  filterNameTo?: string;
  setCurrentFilters: React.Dispatch<
    React.SetStateAction<Record<string, string>>
  >;
}) => {
  const [date, setDate] = useState<DateRange | undefined>(undefined);

  const onSelect = (date: DateRange | undefined) => {
    if (!date) return;

    setDate(date);

    if (date.from) {
      setCurrentFilters((old) => ({
        ...old,
        [filterNameFrom]: date.from!.toISOString(),
      }));
    }

    if (date.to) {
      setCurrentFilters((old) => ({
        ...old,
        [filterNameTo]: date.to!.toISOString(),
      }));
    }
  };

  return (
    <div className={cn('grid gap-2', className)}>
      <Popover>
        <PopoverTrigger asChild>
          <Button
            id="date"
            variant="outline"
            className="border-dashed w-fit justify-start text-left font-normal"
          >
            <CalendarIcon className="mr-2 h-4 w-4" />
            {date?.from ? (
              date.to ? (
                <>
                  {format(date.from, 'LLL dd, y')} -{' '}
                  {format(date.to, 'LLL dd, y')}
                </>
              ) : (
                format(date.from, 'LLL dd, y')
              )
            ) : (
              <span className="font-medium">
                {placeholder || 'Pick a date'}
              </span>
            )}
            {date && (
              <>
                <Separator orientation="vertical" className="mx-2 h-4" />
                <XIcon
                  onClick={(e: any) => {
                    e.preventDefault();
                    setDate(undefined);
                    setCurrentFilters((old) => ({
                      ...old,
                      [filterNameFrom]: '',
                      [filterNameTo]: '',
                    }));
                  }}
                  className="w-4 h-4"
                />
              </>
            )}
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-auto p-0" align="start">
          <Calendar
            initialFocus
            mode="range"
            defaultMonth={date?.from}
            selected={date}
            onSelect={onSelect}
            numberOfMonths={2}
          />
        </PopoverContent>
      </Popover>
    </div>
  );
};
