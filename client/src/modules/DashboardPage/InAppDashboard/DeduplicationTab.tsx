import { useState } from 'react';

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
} from './helpers';
import { DashboardApiParams } from './types';
import { BarTrendChart, DonutChart, HorizontalBars } from './components/charts';
import { ChartCard } from './components/ChartCard';
import { DrilldownSheet } from './components/DrilldownSheet';
import {
  FxMiniBreakdown,
  KpiTile,
  MissingRateBadge,
  formatDisplayAmount,
} from './components/KpiTile';

const ChartSkeleton = () => (
  <div className="h-40 animate-pulse rounded bg-muted" />
);

export const DeduplicationTab = ({
  params,
}: {
  params: DashboardApiParams;
}) => {
  const [isDrilldownOpen, setIsDrilldownOpen] = useState(false);

  const { data: summary, isLoading: summaryLoading } =
    useDuplicatesSummary(params);
  const { data: trend, isLoading: trendLoading } = useDuplicatesTrend(params);
  const { data: split, isLoading: splitLoading } = useDuplicatesSplit(params);
  const { data: blockingPartners, isLoading: blockingLoading } =
    useBlockingPartners(params);
  // Recent events preview (first page) shown inline on the tab
  const { data: recentEvents, isLoading: recentLoading } = useConflictEvents(
    params,
    1,
    true
  );

  return (
    <div className="space-y-4">
      {/* KPI row */}
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
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
          title="Unique overlaps over time"
          description="By first detection date"
        >
          {trendLoading ? (
            <ChartSkeleton />
          ) : (
            <BarTrendChart
              data={(trend ?? []).map((p) => ({
                label: formatBucket(p.bucket),
                value: p.uniqueOverlaps,
                secondary: `${formatCount(p.householdRecordsChecked)} checked`,
              }))}
            />
          )}
        </ChartCard>
        <ChartCard title="Cross-agency vs within-agency">
          {splitLoading ? (
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
          description="Whose existing bookings caused the blocks"
        >
          {blockingLoading ? (
            <ChartSkeleton />
          ) : (
            <HorizontalBars
              data={(blockingPartners ?? []).map((p) => ({
                key: p.organizationId,
                label: p.organizationName,
                value: p.overlapsCaused,
              }))}
            />
          )}
        </ChartCard>
        <ChartCard title="Recent overlap events">
          {recentLoading ? (
            <ChartSkeleton />
          ) : (recentEvents?.data ?? []).length === 0 ? (
            <p className="flex h-40 items-center justify-center text-sm text-muted-foreground">
              No overlap events in this period
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
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
