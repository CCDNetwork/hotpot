import { SegmentedControl } from './SegmentedControl';
import { DashboardPeriod, DisplayCurrency } from '../types';

const PERIOD_OPTIONS: { value: DashboardPeriod; label: string }[] = [
  { value: '7d', label: '7d' },
  { value: '30d', label: '30d' },
  { value: '90d', label: '90d' },
  { value: 'current-month', label: 'Current month' },
];

const CURRENCY_OPTIONS: { value: DisplayCurrency; label: string }[] = [
  { value: 'USD', label: 'USD' },
  { value: 'EUR', label: 'EUR' },
];

type OrgScope = 'all' | 'own';

const ORG_OPTIONS: { value: OrgScope; label: string }[] = [
  { value: 'all', label: 'All organisations' },
  { value: 'own', label: 'My organisation' },
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
  return (
    <div className="sticky top-0 z-10 -mx-4 flex flex-wrap items-center gap-2 bg-background/95 px-4 py-2 backdrop-blur supports-[backdrop-filter]:bg-background/80">
      <SegmentedControl
        ariaLabel="Period"
        options={PERIOD_OPTIONS}
        value={period}
        onChange={onPeriodChange}
      />
      <SegmentedControl
        ariaLabel="Organisation"
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
    </div>
  );
};
