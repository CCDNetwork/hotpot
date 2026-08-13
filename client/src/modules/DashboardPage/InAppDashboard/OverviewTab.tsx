import { useMemo } from 'react';

import { cn } from '@/helpers/utils';

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
  spansMultipleYears,
} from './helpers';
import { DashboardApiParams, TrendPoint } from './types';
import {
  AreaTrendChart,
  BarTrendChart,
  ChartSkeleton,
  DONUT_COLORS,
  DonutChart,
  HorizontalBars,
} from './components/charts';
import { ChartCard } from './components/ChartCard';
import {
  FxMiniBreakdown,
  KpiTile,
  MissingRateBadge,
  formatDisplayAmount,
} from './components/KpiTile';

const toTrendData = (points: TrendPoint[]) => {
  const withYear = spansMultipleYears(points.map((p) => p.bucket));
  return points.map((p) => ({
    label: formatBucket(p.bucket, withYear),
    value: p.count,
  }));
};

export const OverviewTab = ({ params }: { params: DashboardApiParams }) => {
  const summaryQuery = useOverviewSummary(params);
  const trendQuery = useOverviewTrend(params);
  const partnersQuery = useOverviewPartners(params);
  const modalityQuery = useOverviewModality(params);

  const summary = summaryQuery.data;
  const summaryLoading = summaryQuery.isLoading;
  const partners = partnersQuery.data ?? [];

  // keepPreviousData keeps stale data on screen during filter changes; dim it
  // so the change is visibly acknowledged.
  const isRefreshing = [
    summaryQuery,
    trendQuery,
    partnersQuery,
    modalityQuery,
  ].some((q) => q.isFetching && !q.isLoading);

  const householdsTrend = useMemo(
    () => toTrendData(trendQuery.data?.households ?? []),
    [trendQuery.data]
  );
  const newPartnersTrend = useMemo(
    () => toTrendData(trendQuery.data?.newPartners ?? []),
    [trendQuery.data]
  );
  const partnerBars = useMemo(
    () =>
      (partnersQuery.data ?? []).map((p) => ({
        key: `${p.organizationId}-${p.nativeCurrency}`,
        label: p.organizationName,
        value: p.households,
      })),
    [partnersQuery.data]
  );
  const modalitySegments = useMemo(
    () =>
      (modalityQuery.data ?? []).map((m, idx) => ({
        key: m.modality || 'unknown',
        label: `${m.modality || 'Unknown'} (${formatPercent(m.share)})`,
        value: m.households,
        color: DONUT_COLORS[idx % DONUT_COLORS.length],
      })),
    [modalityQuery.data]
  );
  const modalityTotal = useMemo(
    () => (modalityQuery.data ?? []).reduce((sum, m) => sum + m.households, 0),
    [modalityQuery.data]
  );

  return (
    <div
      className={cn(
        'space-y-4 transition-opacity duration-300',
        isRefreshing && 'opacity-60'
      )}
    >
      {/* KPI row */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5">
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
                  className="flex flex-wrap items-center justify-between gap-x-2 text-xs text-muted-foreground"
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
          {trendQuery.isLoading ? (
            <ChartSkeleton />
          ) : (
            <AreaTrendChart data={householdsTrend} />
          )}
        </ChartCard>
        <ChartCard
          title="New partners over time"
          description="Organisations onboarded per bucket"
        >
          {trendQuery.isLoading ? (
            <ChartSkeleton />
          ) : (
            <BarTrendChart data={newPartnersTrend} />
          )}
        </ChartCard>
      </div>

      {/* Partner row */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <ChartCard title="Households per partner">
          {partnersQuery.isLoading ? (
            <ChartSkeleton />
          ) : (
            <HorizontalBars data={partnerBars} />
          )}
        </ChartCard>
        <ChartCard title="Assistance value per partner">
          {partnersQuery.isLoading ? (
            <ChartSkeleton />
          ) : partners.length === 0 ? (
            <p className="flex h-40 items-center justify-center text-sm text-muted-foreground">
              No data in this period
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[26rem] text-sm">
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
                  {partners.map((p) => (
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
        {modalityQuery.isLoading ? (
          <ChartSkeleton />
        ) : (
          <DonutChart
            segments={modalitySegments}
            centerLabel={formatCount(modalityTotal)}
          />
        )}
      </ChartCard>
    </div>
  );
};
