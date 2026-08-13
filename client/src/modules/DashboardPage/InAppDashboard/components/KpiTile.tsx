import { AlertTriangle } from 'lucide-react';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/helpers/utils';

import { formatCurrency } from '../helpers';
import { CurrencyBreakdownRow, DisplayAmount } from '../types';

export const MissingRateBadge = () => (
  <span
    title="No exchange rate available for this period"
    className="inline-flex items-center gap-1 rounded bg-amber-500/15 px-1.5 py-0.5 text-[11px] font-medium text-amber-600 dark:text-amber-400"
  >
    <AlertTriangle className="h-3 w-3" />
    no rate
  </span>
);

/** Inline native → display-currency breakdown, one row per source currency. */
export const FxMiniBreakdown = ({
  breakdown,
  displayCurrency,
}: {
  breakdown: CurrencyBreakdownRow[];
  displayCurrency: string;
}) => {
  if (!breakdown.length) {
    return null;
  }

  return (
    <div className="mt-2 space-y-0.5">
      {breakdown.map((row) => (
        <div
          key={row.currency}
          className="flex flex-wrap items-center justify-between gap-x-2 text-xs text-muted-foreground"
        >
          <span>{formatCurrency(row.native, row.currency)}</span>
          {row.converted != null ? (
            <span>→ {formatCurrency(row.converted, displayCurrency)}</span>
          ) : row.currency.toUpperCase() ===
            displayCurrency.toUpperCase() ? null : (
            <MissingRateBadge />
          )}
        </div>
      ))}
    </div>
  );
};

export const formatDisplayAmount = (display: DisplayAmount): string => {
  if (display.amount == null) {
    return '—';
  }
  return formatCurrency(display.amount, display.currency);
};

export const KpiTile = ({
  title,
  value,
  secondary,
  isLoading,
  onClick,
  children,
}: {
  title: string;
  value: React.ReactNode;
  secondary?: React.ReactNode;
  isLoading?: boolean;
  onClick?: () => void;
  children?: React.ReactNode;
}) => {
  return (
    <Card
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      onKeyDown={
        onClick
          ? (e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                onClick();
              }
            }
          : undefined
      }
      className={cn(
        onClick &&
          'cursor-pointer outline-none transition-colors hover:border-primary/50 focus-visible:border-primary/50'
      )}
    >
      <CardHeader className="px-4 pb-2 pt-4 sm:px-6 sm:pt-6">
        <CardTitle className="text-sm font-medium text-muted-foreground">
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent className="px-4 pb-4 sm:px-6 sm:pb-6">
        {isLoading ? (
          <div className="space-y-2">
            <Skeleton className="h-8 w-24" />
            <Skeleton className="h-3 w-32" />
          </div>
        ) : (
          <>
            <div className="text-2xl font-bold leading-tight tabular-nums">
              {value}
            </div>
            {secondary && (
              <p className="text-xs text-muted-foreground">{secondary}</p>
            )}
            {children}
          </>
        )}
      </CardContent>
    </Card>
  );
};
