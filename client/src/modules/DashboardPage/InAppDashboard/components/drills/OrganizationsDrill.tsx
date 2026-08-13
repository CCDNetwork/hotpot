import { useOrganizationsDrill } from '../../api';
import { formatCount, formatCurrency } from '../../helpers';
import { DashboardApiParams } from '../../types';
import { MissingRateBadge } from '../KpiTile';
import { DrillEmpty, DrillLoading } from './DrillStatus';

export const OrganizationsDrill = ({
  params,
  open,
}: {
  params: DashboardApiParams;
  open: boolean;
}) => {
  const { data, isLoading } = useOrganizationsDrill(params, open);

  if (isLoading) return <DrillLoading />;
  if (!data || data.length === 0)
    return <DrillEmpty label="No organizations feeding data in this period." />;

  return (
    <div className="flex-1 space-y-3 overflow-x-auto">
      <p className="text-xs text-muted-foreground">
        {formatCount(data.length)} organization{data.length === 1 ? '' : 's'}
      </p>
      <table className="w-full min-w-[32rem] text-sm">
        <thead>
          <tr className="border-b text-left text-xs text-muted-foreground">
            <th className="py-2 font-medium">Organization</th>
            <th className="py-2 text-right font-medium">Households</th>
            <th className="py-2 text-right font-medium">Bookings</th>
            <th className="py-2 text-right font-medium">Native value</th>
            <th className="py-2 text-right font-medium">
              → {params.displayCurrency}
            </th>
          </tr>
        </thead>
        <tbody>
          {data.map((row) => (
            <tr
              key={`${row.organizationId}-${row.nativeCurrency}`}
              className="border-b last:border-0"
            >
              <td className="py-1.5 max-w-[14rem] truncate">
                {row.organizationName}
              </td>
              <td className="py-1.5 text-right tabular-nums">
                {formatCount(row.householdsCount)}
              </td>
              <td className="py-1.5 text-right tabular-nums">
                {formatCount(row.bookingsCount)}
              </td>
              <td className="py-1.5 text-right tabular-nums">
                {formatCurrency(row.nativeAmount, row.nativeCurrency)}
              </td>
              <td className="py-1.5 text-right tabular-nums">
                {row.convertedAmount !== null ? (
                  formatCurrency(row.convertedAmount, params.displayCurrency, {
                    noCode: true,
                  })
                ) : row.nativeCurrency.toUpperCase() ===
                  params.displayCurrency.toUpperCase() ? (
                  formatCurrency(row.nativeAmount, params.displayCurrency, {
                    noCode: true,
                  })
                ) : (
                  <MissingRateBadge />
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};
