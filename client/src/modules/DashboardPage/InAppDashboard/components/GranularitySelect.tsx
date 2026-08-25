import { TrendGranularity } from '../types';
import { SegmentedControl } from './SegmentedControl';

const OPTIONS: { value: TrendGranularity; label: string }[] = [
  { value: 'daily', label: 'Day' },
  { value: 'weekly', label: 'Week' },
  { value: 'monthly', label: 'Month' },
  { value: 'quarterly', label: 'Quarter' },
  { value: 'annual', label: 'Year' },
];

/**
 * Trend-bucket size for the two overview trend charts. Placed near the charts
 * (not in the top FilterBar) so it only re-runs the trend endpoints, not the
 * summary / partners / modality queries.
 */
export const GranularitySelect = ({
  value,
  onChange,
}: {
  value: TrendGranularity;
  onChange: (next: TrendGranularity) => void;
}) => (
  <div className="flex items-center gap-2 text-sm text-muted-foreground">
    <span>Trend by</span>
    <SegmentedControl
      ariaLabel="Trend granularity"
      options={OPTIONS}
      value={value}
      onChange={onChange}
    />
  </div>
);
