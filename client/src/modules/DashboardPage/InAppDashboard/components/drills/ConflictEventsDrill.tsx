import { useState } from 'react';
import { ChevronRight } from 'lucide-react';

import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';

import { useConflictEvent, useConflictEvents } from '../../api';
import { formatCurrency, formatDate } from '../../helpers';
import { ConflictEventRow, DashboardApiParams } from '../../types';
import { MissingRateBadge } from '../KpiTile';
import { ConvertedAmount } from './shared';
import { DrillPager } from './DrillPager';
import { DrillEmpty, DrillLoading } from './DrillStatus';

const CategoryBadge = ({
  category,
}: {
  category: ConflictEventRow['category'];
}) => (
  <Badge variant={category === 'across_agencies' ? 'default' : 'secondary'}>
    {category === 'across_agencies' ? 'Across agencies' : 'Within agency'}
  </Badge>
);

const SectionHeading = ({ children }: { children: React.ReactNode }) => (
  <div className="mt-3 mb-1 text-sm font-medium first:mt-0">{children}</div>
);

/** Level 2: the blocking booking's operational metadata, loaded per event. */
const EventDetail = ({
  params,
  eventId,
}: {
  params: DashboardApiParams;
  eventId: string;
}) => {
  const { data, isLoading } = useConflictEvent(params, eventId);

  if (isLoading) {
    return (
      <div className="space-y-2 py-2">
        <Skeleton className="h-3 w-3/4" />
        <Skeleton className="h-3 w-2/3" />
        <Skeleton className="h-3 w-1/2" />
      </div>
    );
  }

  if (!data) {
    return (
      <p className="py-2 text-sm text-muted-foreground">
        Event data is no longer available.
      </p>
    );
  }

  const booking = data.blockingBooking;

  return (
    <div className="py-2 text-sm">
      <SectionHeading>Blocking booking</SectionHeading>
      {booking ? (
        <dl className="grid grid-cols-[10rem_1fr] gap-x-4 gap-y-1">
          <dt className="text-xs uppercase tracking-wide text-muted-foreground">
            Owner
          </dt>
          <dd>{data.blockingOrganizationName}</dd>
          <dt className="text-xs uppercase tracking-wide text-muted-foreground">
            Period
          </dt>
          <dd>
            {formatDate(booking.startDate)} – {formatDate(booking.endDate)}
          </dd>
          <dt className="text-xs uppercase tracking-wide text-muted-foreground">
            Native
          </dt>
          <dd>
            {formatCurrency(booking.amount, booking.currency)}
            {booking.modality ? ` · ${booking.modality}` : ''}
            {' · '}
            {booking.rounds} round{booking.rounds === 1 ? '' : 's'}
          </dd>
          <dt className="text-xs uppercase tracking-wide text-muted-foreground">
            {params.displayCurrency} equiv
          </dt>
          <dd>
            {booking.currency.toUpperCase() ===
            params.displayCurrency.toUpperCase() ? (
              <span className="text-muted-foreground">
                same as native
              </span>
            ) : booking.convertedAmount != null ? (
              formatCurrency(booking.convertedAmount, params.displayCurrency)
            ) : (
              <MissingRateBadge />
            )}
          </dd>
          <dt className="text-xs uppercase tracking-wide text-muted-foreground">
            Created
          </dt>
          <dd>{formatDate(booking.createdAt)}</dd>
        </dl>
      ) : (
        <p className="text-muted-foreground">
          The blocking booking is no longer available (deleted or released).
        </p>
      )}

      <SectionHeading>Detection history</SectionHeading>
      <dl className="grid grid-cols-[10rem_1fr] gap-x-4 gap-y-1">
        <dt className="text-xs uppercase tracking-wide text-muted-foreground">
          First detected
        </dt>
        <dd>{formatDate(data.firstDetectedAt)}</dd>
        <dt className="text-xs uppercase tracking-wide text-muted-foreground">
          Last detected
        </dt>
        <dd>{formatDate(data.lastDetectedAt)}</dd>
        <dt className="text-xs uppercase tracking-wide text-muted-foreground">
          Times seen
        </dt>
        <dd>
          {data.detectionCount}× — fingerprint-deduplicated across pre-booking
          and booking runs
        </dd>
      </dl>

      <SectionHeading>Overlap subject</SectionHeading>
      <dl className="grid grid-cols-[10rem_1fr] gap-x-4 gap-y-1">
        <dt className="text-xs uppercase tracking-wide text-muted-foreground">
          Household ref
        </dt>
        <dd className="font-mono text-xs">{data.subjectLabel}</dd>
        <dt className="text-xs uppercase tracking-wide text-muted-foreground">
          Overlap window
        </dt>
        <dd>
          {formatDate(data.overlapStartDate)} –{' '}
          {formatDate(data.overlapEndDate)}
        </dd>
      </dl>

      {data.category === 'within_same_agency' && (
        <p className="mt-3 text-xs text-muted-foreground">
          Same organization on both sides — likely an operational workflow
          issue within {data.requestingOrganizationName}.
        </p>
      )}
    </div>
  );
};

export const ConflictEventsDrill = ({
  params,
  open,
  focusEventId,
}: {
  params: DashboardApiParams;
  open: boolean;
  // Pre-expand this event on first render — used when the drill was opened
  // from a row-click on the Recent overlap events table.
  focusEventId?: string;
}) => {
  const [page, setPage] = useState(1);
  const [expandedId, setExpandedId] = useState<string | null>(
    focusEventId ?? null
  );
  const { data, isLoading } = useConflictEvents(params, page, open);

  if (isLoading) return <DrillLoading />;
  if (!data || data.data.length === 0)
    return <DrillEmpty label="No overlap events in this period." />;

  const totalPages = Math.max(Math.ceil(data.totalCount / data.pageSize), 1);

  return (
    <>
      <div className="flex-1 divide-y divide-border">
        {data.data.map((event) => {
          const isExpanded = expandedId === event.id;
          return (
            <div key={event.id} className="py-1">
              <button
                type="button"
                onClick={() => setExpandedId(isExpanded ? null : event.id)}
                className="-mx-2 flex w-[calc(100%+1rem)] items-start gap-2 rounded-md px-2 py-2 text-left outline-none transition-colors hover:bg-muted/50 focus-visible:bg-muted/50"
              >
                <ChevronRight
                  className={`mt-0.5 h-4 w-4 shrink-0 text-muted-foreground transition-transform ${
                    isExpanded ? 'rotate-90' : ''
                  }`}
                />
                <div className="min-w-0 flex-1 space-y-0.5">
                  <p className="truncate text-sm font-medium">
                    {event.requestingOrganizationName} ·{' '}
                    <span className="text-muted-foreground">blocked by</span>{' '}
                    {event.blockingOrganizationName}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {event.subjectLabel} ·{' '}
                    {formatDate(event.overlapStartDate)} –{' '}
                    {formatDate(event.overlapEndDate)} ·{' '}
                    {event.proposedModality || '—'} · {event.proposedRounds}{' '}
                    round{event.proposedRounds === 1 ? '' : 's'}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    First seen {formatDate(event.firstDetectedAt)} · last seen{' '}
                    {formatDate(event.lastDetectedAt)} · detected{' '}
                    {event.detectionCount}×
                  </p>
                </div>
                <div className="flex shrink-0 flex-col items-end gap-1 text-sm">
                  <ConvertedAmount
                    amount={event.proposedAmount}
                    currency={event.proposedCurrency}
                    converted={event.convertedAmount}
                    displayCurrency={params.displayCurrency}
                  />
                  <CategoryBadge category={event.category} />
                </div>
              </button>
              {isExpanded && (
                <div className="ml-6 mt-1 rounded-md border bg-muted/30 px-3">
                  <EventDetail params={params} eventId={event.id} />
                </div>
              )}
            </div>
          );
        })}
      </div>

      {data.totalCount > data.pageSize && (
        <DrillPager
          page={page}
          totalPages={totalPages}
          onChange={setPage}
        />
      )}
    </>
  );
};

// Kept for legacy import path; internal helpers use ../KpiTile directly.
export { MissingRateBadge };
