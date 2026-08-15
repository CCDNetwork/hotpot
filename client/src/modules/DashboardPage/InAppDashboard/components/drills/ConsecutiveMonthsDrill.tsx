import { useState } from 'react';

import { useConsecutiveMonthsDetails } from '../../api';
import { formatCount, formatMonth } from '../../helpers';
import { DrillPager } from './DrillPager';
import { DrillEmpty, DrillLoading } from './DrillStatus';

const parseIsoMonth = (iso: string) => {
  // Server sends "2026-03-01T00:00:00Z" — take year+month, ignore day (always 1).
  const d = new Date(iso);
  return { year: d.getUTCFullYear(), month: d.getUTCMonth() + 1 };
};

export const ConsecutiveMonthsDrill = ({
  bucket,
  open,
}: {
  bucket: string;
  open: boolean;
}) => {
  const [page, setPage] = useState(1);
  const { data, isLoading } = useConsecutiveMonthsDetails(bucket, page, open);

  if (isLoading) return <DrillLoading />;
  if (!data || data.data.length === 0)
    return <DrillEmpty label={`No runs in the "${bucket} months" bucket.`} />;

  const totalPages = Math.max(Math.ceil(data.totalCount / data.pageSize), 1);

  return (
    <div className="flex-1 space-y-3 overflow-x-auto">
      <p className="text-xs text-muted-foreground">
        {formatCount(data.totalCount)} run{data.totalCount === 1 ? '' : 's'} of{' '}
        {bucket} month{bucket === '1' ? '' : 's'} (trailing 12-month window).
        Each row is one uninterrupted stretch of booked assistance for a single
        household; the household may appear more than once if it has separate
        runs.
      </p>
      <table className="w-full min-w-[32rem] text-sm">
        <thead>
          <tr className="border-b text-left text-xs text-muted-foreground">
            <th className="py-2 font-medium">Household</th>
            <th className="py-2 font-medium">Run window</th>
            <th className="py-2 text-right font-medium">Months</th>
            <th className="py-2 font-medium">Organizations</th>
          </tr>
        </thead>
        <tbody>
          {data.data.map((row, idx) => {
            const s = parseIsoMonth(row.runStartMonth);
            const e = parseIsoMonth(row.runEndMonth);
            return (
              <tr
                key={`${row.householdIdMasked}-${row.runStartMonth}-${idx}`}
                className="border-b last:border-0"
              >
                <td className="py-1.5 font-mono text-xs">
                  {row.householdIdMasked}
                </td>
                <td className="py-1.5 whitespace-nowrap text-muted-foreground">
                  {formatMonth(s.year, s.month)} – {formatMonth(e.year, e.month)}
                </td>
                <td className="py-1.5 text-right tabular-nums">
                  {row.monthsCount}
                </td>
                <td className="py-1.5 max-w-[14rem] truncate">
                  {row.organizationNames.join(', ') || '—'}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
      {totalPages > 1 && (
        <DrillPager page={page} totalPages={totalPages} onChange={setPage} />
      )}
    </div>
  );
};
