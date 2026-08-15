import { useIsFetching } from '@tanstack/react-query';
import { Loader2 } from 'lucide-react';

import { cn } from '@/helpers/utils';

import { SegmentedControl } from './SegmentedControl';
import { DashboardPeriod, DisplayCurrency } from '../types';

const PERIOD_OPTIONS: { value: DashboardPeriod; label: string }[] = [
  { value: '7d', label: '7d' },
  { value: '30d', label: '30d' },
  { value: '90d', label: '90d' },
  { value: 'current-month', label: 'Current month' },
  { value: 'all-time', label: 'All time' },
];

const CURRENCY_OPTIONS: { value: DisplayCurrency; label: string }[] = [
  { value: 'USD', label: 'USD' },
  { value: 'EUR', label: 'EUR' },
  { value: 'ILS', label: 'ILS' },
];

type OrgScope = 'all' | 'own';

const ORG_OPTIONS: { value: OrgScope; label: string }[] = [
  { value: 'all', label: 'All organizations' },
  { value: 'own', label: 'My organization' },
];

export const FilterBar = ({
  period,
  onPeriodChange,
  myOrganizationOnly,
  onMyOrganizationOnlyChange,
  currency,
  onCurrencyChange,
}: {
  period: DashboardPeriod;
  onPeriodChange: (period: DashboardPeriod) => void;
  myOrganizationOnly: boolean;
  onMyOrganizationOnlyChange: (own: boolean) => void;
  currency: DisplayCurrency;
  onCurrencyChange: (currency: DisplayCurrency) => void;
}) => {
  const isFetching =
    useIsFetching({
      predicate: (query) =>
        String(query.queryKey[0] ?? '').startsWith('dashboard_'),
    }) > 0;

  return (
    <div className="sticky top-0 z-10 -mx-4 flex flex-wrap items-center gap-2 bg-background/95 px-4 py-2 backdrop-blur supports-[backdrop-filter]:bg-background/80">
      <SegmentedControl
        ariaLabel="Period"
        options={PERIOD_OPTIONS}
        value={period}
        onChange={onPeriodChange}
      />
      <SegmentedControl
        ariaLabel="Organization"
        options={ORG_OPTIONS}
        value={myOrganizationOnly ? 'own' : 'all'}
        onChange={(scope) => onMyOrganizationOnlyChange(scope === 'own')}
      />
      <SegmentedControl
        ariaLabel="Currency"
        options={CURRENCY_OPTIONS}
        value={currency}
        onChange={onCurrencyChange}
      />
      {/* Always rendered so the bar doesn't reflow when fetching starts. */}
      <Loader2
        aria-hidden
        className={cn(
          'ml-auto h-4 w-4 animate-spin text-muted-foreground transition-opacity',
          isFetching ? 'opacity-100' : 'opacity-0'
        )}
      />
    </div>
  );
};
