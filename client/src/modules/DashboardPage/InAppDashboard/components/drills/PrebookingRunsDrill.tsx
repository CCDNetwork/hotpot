import { useState } from 'react';
import { Badge } from '@/components/ui/badge';

import { usePrebookingRunsDrill } from '../../api';
import { formatCount, formatDate } from '../../helpers';
import { DashboardApiParams } from '../../types';
import { DrillPager } from './DrillPager';
import { DrillEmpty, DrillLoading } from './DrillStatus';

export const PrebookingRunsDrill = ({
  params,
  open,
}: {
  params: DashboardApiParams;
  open: boolean;
}) => {
  const [page, setPage] = useState(1);
  const { data, isLoading } = usePrebookingRunsDrill(params, page, open);

  if (isLoading) return <DrillLoading />;
  if (!data || data.data.length === 0)
    return <DrillEmpty label="No pre-booking runs in this period." />;

  const totalPages = Math.max(Math.ceil(data.totalCount / data.pageSize), 1);

  return (
    <div className="flex-1 space-y-3 overflow-x-auto">
      <p className="text-xs text-muted-foreground">
        {formatCount(data.totalCount)} pre-booking run{data.totalCount === 1 ? '' : 's'}
      </p>
      <table className="w-full min-w-[34rem] text-sm">
        <thead>
          <tr className="border-b text-left text-xs text-muted-foreground">
            <th className="py-2 font-medium">Uploaded</th>
            <th className="py-2 font-medium">By</th>
            <th className="py-2 font-medium">Organization</th>
            <th className="py-2 text-right font-medium">Rows</th>
            <th className="py-2 text-right font-medium">Success / Fail</th>
          </tr>
        </thead>
        <tbody>
          {data.data.map((row) => (
            <tr key={row.submissionId} className="border-b last:border-0">
              <td className="py-1.5 whitespace-nowrap text-muted-foreground">
                {formatDate(row.uploadedAt)}
              </td>
              <td className="py-1.5 max-w-[10rem] truncate">
                {row.uploadedByName || '—'}
              </td>
              <td className="py-1.5 max-w-[12rem] truncate">
                {row.organizationName}
              </td>
              <td className="py-1.5 text-right tabular-nums">
                {formatCount(row.totalRows)}
              </td>
              <td className="py-1.5 text-right">
                <div className="flex justify-end gap-1">
                  {row.successRows > 0 && (
                    <Badge variant="secondary" className="tabular-nums">
                      {formatCount(row.successRows)} ok
                    </Badge>
                  )}
                  {row.failedRows > 0 && (
                    <Badge variant="destructive" className="tabular-nums">
                      {formatCount(row.failedRows)} fail
                    </Badge>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {totalPages > 1 && (
        <DrillPager page={page} totalPages={totalPages} onChange={setPage} />
      )}
    </div>
  );
};
