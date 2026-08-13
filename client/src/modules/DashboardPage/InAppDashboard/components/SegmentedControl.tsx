import { cn } from '@/helpers/utils';

type Props<T extends string> = {
  options: { value: T; label: string }[];
  value: T;
  onChange: (value: T) => void;
  ariaLabel: string;
};

export const SegmentedControl = <T extends string>({
  options,
  value,
  onChange,
  ariaLabel,
}: Props<T>) => {
  return (
    <div
      role="group"
      aria-label={ariaLabel}
      className="inline-flex h-9 items-center rounded-lg bg-muted p-1 text-muted-foreground"
    >
      {options.map((option) => (
        <button
          key={option.value}
          type="button"
          onClick={() => onChange(option.value)}
          className={cn(
            'inline-flex items-center justify-center whitespace-nowrap rounded-md px-3 py-1 text-sm font-medium transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
            option.value === value
              ? 'bg-background text-foreground shadow'
              : 'hover:text-foreground'
          )}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
};
