import { useValueFxDrill } from '../../api';
import { formatCount, formatCurrency, formatMonth } from '../../helpers';
import { DashboardApiParams } from '../../types';
import { MissingRateBadge } from '../KpiTile';
import { DrillEmpty, DrillLoading } from './DrillStatus';

const RateCell = ({ rate }: { rate: number | null }) => {
  if (rate === null) return <MissingRateBadge />;
  // Rates are typically 0.2… (ILS→USD) or 3.5… (USD→JOD-ish). 4 sig figs
  // is enough to spot a mismatch against InforEuro's own display.
  return (
    <span className="tabular-nums">
      {rate.toLocaleString(undefined, {
        minimumFractionDigits: 4,
        maximumFractionDigits: 6,
      })}
    </span>
  );
};

export const ValueFxDrill = ({
  params,
  open,
  source,
}: {
  params: DashboardApiParams;
  open: boolean;
  source: 'bookings' | 'conflicts';
}) => {
  const { data, isLoading } = useValueFxDrill(params, source, open);

  if (isLoading) return <DrillLoading />;
  if (!data || data.rows.length === 0)
    return (
      <DrillEmpty
        label={
          source === 'conflicts'
            ? 'No overlap value in this period.'
            : 'No value transferred in this period.'
        }
      />
    );

  return (
    <div className="flex-1 space-y-3 overflow-x-auto">
      <table className="w-full min-w-[36rem] text-sm">
        <thead>
          <tr className="border-b text-left text-xs text-muted-foreground">
            <th className="py-2 font-medium">Currency</th>
            <th className="py-2 font-medium">Month</th>
            <th className="py-2 text-right font-medium">Native amount</th>
            <th className="py-2 text-right font-medium">
              Rate → {data.displayCurrency}
            </th>
            <th className="py-2 text-right font-medium">Converted</th>
            <th className="py-2 text-right font-medium">Rows</th>
          </tr>
        </thead>
        <tbody>
          {data.rows.map((row, idx) => (
            <tr
              key={`${row.currency}-${row.year}-${row.month}-${idx}`}
              className="border-b last:border-0"
            >
              <td className="py-1.5 font-medium">{row.currency}</td>
              <td className="py-1.5 text-muted-foreground">
                {formatMonth(row.year, row.month)}
              </td>
              <td className="py-1.5 text-right tabular-nums">
                {formatCurrency(row.native, row.currency, { noCode: true })}
              </td>
              <td className="py-1.5 text-right">
                <RateCell rate={row.rate} />
              </td>
              <td className="py-1.5 text-right tabular-nums">
                {row.converted !== null ? (
                  formatCurrency(row.converted, data.displayCurrency, {
                    noCode: true,
                  })
                ) : (
                  <MissingRateBadge />
                )}
              </td>
              <td className="py-1.5 text-right tabular-nums text-muted-foreground">
                {formatCount(row.count)}
              </td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr className="border-t-2 font-medium">
            <td className="py-2" colSpan={2}>
              Total
            </td>
            <td className="py-2 text-right">
              <div className="flex flex-col items-end tabular-nums">
                {data.totals.native.map((n) => (
                  <span key={n.currency}>
                    {formatCurrency(n.amount, n.currency)}
                  </span>
                ))}
              </div>
            </td>
            <td className="py-2 text-right text-xs text-muted-foreground">
              —
            </td>
            <td className="py-2 text-right tabular-nums">
              {data.totals.converted !== null ? (
                formatCurrency(data.totals.converted, data.displayCurrency)
              ) : (
                <MissingRateBadge />
              )}
            </td>
            <td className="py-2 text-right tabular-nums text-muted-foreground">
              {formatCount(
                data.rows.reduce((sum, r) => sum + r.count, 0)
              )}
            </td>
          </tr>
        </tfoot>
      </table>

      <p className="text-xs text-muted-foreground">
        Rate = InforEuro monthly (native → EUR ÷ display → EUR). Rows are
        grouped by the assistance month; totals sum every row.
      </p>
    </div>
  );
};
