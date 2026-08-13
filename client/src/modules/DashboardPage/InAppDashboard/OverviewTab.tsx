import {
  useOverviewModality,
  useOverviewPartners,
  useOverviewSummary,
  useOverviewTrend,
} from './api';
import {
  formatBucket,
  formatCount,
  formatCurrency,
  formatPercent,
} from './helpers';
import { DashboardApiParams } from './types';
import {
  BarTrendChart,
  DONUT_COLORS,
  DonutChart,
  HorizontalBars,
  LineTrendChart,
} from './components/charts';
import { ChartCard } from './components/ChartCard';
import {
  FxMiniBreakdown,
  KpiTile,
  MissingRateBadge,
  formatDisplayAmount,
} from './components/KpiTile';

const ChartSkeleton = () => (
  <div className="h-40 animate-pulse rounded bg-muted" />
);

export const OverviewTab = ({ params }: { params: DashboardApiParams }) => {
  const { data: summary, isLoading: summaryLoading } =
    useOverviewSummary(params);
  const { data: trend, isLoading: trendLoading } = useOverviewTrend(params);
  const { data: partners, isLoading: partnersLoading } =
    useOverviewPartners(params);
  const { data: modality, isLoading: modalityLoading } =
    useOverviewModality(params);

  return (
    <div className="space-y-4">
      {/* KPI row */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
        <KpiTile
          title="Households assisted"
          isLoading={summaryLoading}
          value={formatCount(summary?.householdsAssisted ?? 0)}
        />
        <KpiTile
          title="Individuals reached"
          isLoading={summaryLoading}
          value={formatCount(summary?.individualsReached ?? 0)}
          secondary={`households × ${
            summary && summary.householdsAssisted > 0
              ? Math.round(
                  summary.individualsReached / summary.householdsAssisted
                )
              : 6
          }`}
        />
        <KpiTile
          title="Active partners"
          isLoading={summaryLoading}
          value={formatCount(summary?.activePartners ?? 0)}
          secondary={`of ${formatCount(summary?.totalOnboarded ?? 0)} onboarded`}
        />
        <KpiTile
          title="Value transferred"
          isLoading={summaryLoading}
          value={
            summary
              ? formatDisplayAmount(summary.valueTransferred.display)
              : '—'
          }
        >
          {summary && (
            <FxMiniBreakdown
              breakdown={summary.valueTransferred.breakdown}
              displayCurrency={params.displayCurrency}
            />
          )}
        </KpiTile>
        <KpiTile
          title="Average transfer"
          isLoading={summaryLoading}
          value={
            summary ? formatDisplayAmount(summary.avgTransfer.display) : '—'
          }
          secondary="per booking"
        >
          {summary && (
            <div className="mt-2 space-y-0.5">
              {summary.avgTransfer.breakdown.map((row) => (
                <div
                  key={row.currency}
                  className="flex items-center justify-between gap-2 text-xs text-muted-foreground"
                >
                  <span>{formatCurrency(row.avgNative, row.currency)}</span>
                  {row.avgConverted != null ? (
                    <span>
                      →{' '}
                      {formatCurrency(row.avgConverted, params.displayCurrency)}
                    </span>
                  ) : row.currency.toUpperCase() ===
                    params.displayCurrency.toUpperCase() ? null : (
                    <MissingRateBadge />
                  )}
                </div>
              ))}
            </div>
          )}
        </KpiTile>
      </div>

      {/* Trend row */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <ChartCard
          title="Households assisted over time"
          description="Distinct households by assistance start date"
        >
          {trendLoading ? (
            <ChartSkeleton />
          ) : (
            <LineTrendChart
              data={(trend?.households ?? []).map((p) => ({
                label: formatBucket(p.bucket),
                value: p.count,
              }))}
            />
          )}
        </ChartCard>
        <ChartCard
          title="New partners over time"
          description="Organisations onboarded per bucket"
        >
          {trendLoading ? (
            <ChartSkeleton />
          ) : (
            <BarTrendChart
              data={(trend?.newPartners ?? []).map((p) => ({
                label: formatBucket(p.bucket),
                value: p.count,
              }))}
            />
          )}
        </ChartCard>
      </div>

      {/* Partner row */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <ChartCard title="Households per partner">
          {partnersLoading ? (
            <ChartSkeleton />
          ) : (
            <HorizontalBars
              data={(partners ?? []).map((p) => ({
                key: `${p.organizationId}-${p.nativeCurrency}`,
                label: p.organizationName,
                value: p.households,
              }))}
            />
          )}
        </ChartCard>
        <ChartCard title="Assistance value per partner">
          {partnersLoading ? (
            <ChartSkeleton />
          ) : (partners ?? []).length === 0 ? (
            <p className="flex h-40 items-center justify-center text-sm text-muted-foreground">
              No data in this period
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-left text-xs text-muted-foreground">
                    <th className="py-1.5 font-medium">Partner</th>
                    <th className="py-1.5 text-right font-medium">
                      Native amount
                    </th>
                    <th className="py-1.5 text-right font-medium">
                      → {params.displayCurrency}
                    </th>
                    <th className="py-1.5 text-right font-medium">Bookings</th>
                  </tr>
                </thead>
                <tbody>
                  {(partners ?? []).map((p) => (
                    <tr
                      key={`${p.organizationId}-${p.nativeCurrency}`}
                      className="border-b last:border-0"
                    >
                      <td className="max-w-[12rem] truncate py-1.5">
                        {p.organizationName}
                      </td>
                      <td className="py-1.5 text-right tabular-nums">
                        {formatCurrency(p.nativeAmount, p.nativeCurrency)}
                      </td>
                      <td className="py-1.5 text-right tabular-nums">
                        {p.convertedAmount != null ? (
                          formatCurrency(
                            p.convertedAmount,
                            params.displayCurrency,
                            { noCode: true }
                          )
                        ) : p.nativeCurrency.toUpperCase() ===
                          params.displayCurrency.toUpperCase() ? (
                          formatCurrency(
                            p.nativeAmount,
                            params.displayCurrency,
                            { noCode: true }
                          )
                        ) : (
                          <MissingRateBadge />
                        )}
                      </td>
                      <td className="py-1.5 text-right tabular-nums">
                        {formatCount(p.bookings)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </ChartCard>
      </div>

      {/* Composition row */}
      <ChartCard
        title="Reached by assistance type"
        description="Distinct households per modality"
      >
        {modalityLoading ? (
          <ChartSkeleton />
        ) : (
          <DonutChart
            segments={(modality ?? []).map((m, idx) => ({
              key: m.modality || 'unknown',
              label: `${m.modality || 'Unknown'} (${formatPercent(m.share)})`,
              value: m.households,
              color: DONUT_COLORS[idx % DONUT_COLORS.length],
            }))}
            centerLabel={formatCount(
              (modality ?? []).reduce((sum, m) => sum + m.households, 0)
            )}
          />
        )}
      </ChartCard>
    </div>
  );
};
