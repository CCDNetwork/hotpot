import { useMemo, useState } from 'react';

import { cn } from '@/helpers/utils';

import {
  useBlockingPartners,
  useConflictEvents,
  useDuplicatesSplit,
  useDuplicatesSummary,
  useDuplicatesTrend,
} from './api';
import {
  formatBucket,
  formatCount,
  formatCurrency,
  formatDate,
  formatPercent,
  spansMultipleYears,
} from './helpers';
import { DashboardApiParams } from './types';
import {
  ChartSkeleton,
  DonutChart,
  HorizontalBars,
  RateTrendChart,
} from './components/charts';
import { ChartCard } from './components/ChartCard';
import { DrilldownSheet } from './components/DrilldownSheet';
import {
  FxMiniBreakdown,
  KpiTile,
  MissingRateBadge,
  formatDisplayAmount,
} from './components/KpiTile';

export const DeduplicationTab = ({
  params,
}: {
  params: DashboardApiParams;
}) => {
  const [isDrilldownOpen, setIsDrilldownOpen] = useState(false);

  const summaryQuery = useDuplicatesSummary(params);
  const trendQuery = useDuplicatesTrend(params);
  const splitQuery = useDuplicatesSplit(params);
  const blockingQuery = useBlockingPartners(params);
  // Recent events preview (first page) shown inline on the tab
  const recentQuery = useConflictEvents(params, 1, true);

  const summary = summaryQuery.data;
  const summaryLoading = summaryQuery.isLoading;
  const split = splitQuery.data;
  const recentEvents = recentQuery.data;

  // keepPreviousData keeps stale data on screen during filter changes; dim it
  // so the change is visibly acknowledged.
  const isRefreshing = [
    summaryQuery,
    trendQuery,
    splitQuery,
    blockingQuery,
    recentQuery,
  ].some((q) => q.isFetching && !q.isLoading);

  // Per-bucket overlap rate, derived from the counts the trend already carries.
  const rateTrend = useMemo(() => {
    const points = trendQuery.data ?? [];
    const withYear = spansMultipleYears(points.map((p) => p.bucket));
    return points.map((p) => ({
      label: formatBucket(p.bucket, withYear),
      count: p.uniqueOverlaps,
      checked: p.householdRecordsChecked,
      rate:
        p.householdRecordsChecked > 0
          ? p.uniqueOverlaps / p.householdRecordsChecked
          : null,
    }));
  }, [trendQuery.data]);

  // Each blocking partner's share of all overlaps in the period.
  const blockingBars = useMemo(() => {
    const rows = blockingQuery.data ?? [];
    const total = rows.reduce((sum, r) => sum + r.overlapsCaused, 0);
    return rows.map((r) => ({
      key: r.organizationId,
      label: r.organizationName,
      value: r.overlapsCaused,
      valueLabel:
        total > 0
          ? `${formatCount(r.overlapsCaused)} (${formatPercent(
              r.overlapsCaused / total
            )})`
          : formatCount(r.overlapsCaused),
    }));
  }, [blockingQuery.data]);

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
          title="Unique overlaps detected"
          isLoading={summaryLoading}
          value={formatCount(summary?.uniqueOverlaps ?? 0)}
          secondary="click to drill down"
          onClick={() => setIsDrilldownOpen(true)}
        />
        <KpiTile
          title="Pre-booking runs"
          isLoading={summaryLoading}
          value={formatCount(summary?.prebookingRuns ?? 0)}
        />
        <KpiTile
          title="Household records checked"
          isLoading={summaryLoading}
          value={formatCount(summary?.householdRecordsChecked ?? 0)}
          secondary="via pre-booking"
        />
        <KpiTile
          title="Overlap rate"
          isLoading={summaryLoading}
          value={
            summary?.overlapRate != null
              ? formatPercent(summary.overlapRate)
              : '—'
          }
          secondary="unique overlaps ÷ pre-booking records checked"
        />
        <KpiTile
          title="Value of overlaps"
          isLoading={summaryLoading}
          value={
            summary ? formatDisplayAmount(summary.valueOfOverlaps.display) : '—'
          }
          secondary="payments avoided"
        >
          {summary && (
            <FxMiniBreakdown
              breakdown={summary.valueOfOverlaps.breakdown}
              displayCurrency={params.displayCurrency}
            />
          )}
        </KpiTile>
      </div>

      {/* Trend + split row */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <ChartCard
          title="Overlaps and overlap rate over time"
          description="Bars: unique overlaps · line: overlaps ÷ records checked, by first detection date"
        >
          {trendQuery.isLoading ? (
            <ChartSkeleton />
          ) : (
            <RateTrendChart data={rateTrend} />
          )}
        </ChartCard>
        <ChartCard title="Cross-agency vs within-agency">
          {splitQuery.isLoading ? (
            <ChartSkeleton />
          ) : (
            <DonutChart
              segments={[
                {
                  key: 'across',
                  label: 'Across agencies',
                  value: split?.acrossAgency ?? 0,
                  color: '#0ea5e9', // sky-500
                },
                {
                  key: 'within',
                  label: 'Within agency',
                  value: split?.withinAgency ?? 0,
                  color: '#f59e0b', // amber-500
                },
              ]}
              centerLabel={formatCount(
                (split?.acrossAgency ?? 0) + (split?.withinAgency ?? 0)
              )}
            />
          )}
        </ChartCard>
      </div>

      {/* Blocking + recent row */}
      <div className="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <ChartCard
          title="Blocking partners"
          description="Whose existing bookings caused the blocks · share of all overlaps"
        >
          {blockingQuery.isLoading ? (
            <ChartSkeleton />
          ) : (
            <HorizontalBars data={blockingBars} />
          )}
        </ChartCard>
        <ChartCard title="Recent overlap events">
          {recentQuery.isLoading ? (
            <ChartSkeleton />
          ) : (recentEvents?.data ?? []).length === 0 ? (
            <p className="flex h-40 items-center justify-center text-sm text-muted-foreground">
              No overlap events in this period
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full min-w-[26rem] text-sm">
                <thead>
                  <tr className="border-b text-left text-xs text-muted-foreground">
                    <th className="py-1.5 font-medium">
                      Requester · Blocked by
                    </th>
                    <th className="py-1.5 text-right font-medium">
                      Native amount
                    </th>
                    <th className="py-1.5 text-right font-medium">
                      → {params.displayCurrency}
                    </th>
                    <th className="py-1.5 text-right font-medium">Seen</th>
                  </tr>
                </thead>
                <tbody>
                  {(recentEvents?.data ?? []).slice(0, 8).map((event) => (
                    <tr key={event.id} className="border-b last:border-0">
                      <td className="max-w-[14rem] truncate py-1.5">
                        {event.requestingOrganizationName} ·{' '}
                        <span className="text-muted-foreground">
                          {event.blockingOrganizationName}
                        </span>
                      </td>
                      <td className="py-1.5 text-right tabular-nums">
                        {formatCurrency(
                          event.proposedAmount,
                          event.proposedCurrency
                        )}
                      </td>
                      <td className="py-1.5 text-right tabular-nums">
                        {event.convertedAmount != null ? (
                          formatCurrency(
                            event.convertedAmount,
                            params.displayCurrency,
                            { noCode: true }
                          )
                        ) : event.proposedCurrency.toUpperCase() ===
                          params.displayCurrency.toUpperCase() ? (
                          formatCurrency(
                            event.proposedAmount,
                            params.displayCurrency,
                            { noCode: true }
                          )
                        ) : (
                          <MissingRateBadge />
                        )}
                      </td>
                      <td className="whitespace-nowrap py-1.5 text-right text-xs text-muted-foreground">
                        {formatDate(event.lastDetectedAt)} ·{' '}
                        {event.detectionCount}×
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </ChartCard>
      </div>

      <DrilldownSheet
        open={isDrilldownOpen}
        onOpenChange={setIsDrilldownOpen}
        params={params}
      />
    </div>
  );
};
