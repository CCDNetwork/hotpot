import { useState } from 'react';
import { ChevronRight, Loader2 } from 'lucide-react';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { Skeleton } from '@/components/ui/skeleton';

import { useConflictEvent, useConflictEvents } from '../api';
import { formatCurrency, formatDate } from '../helpers';
import { ConflictEventRow, DashboardApiParams } from '../types';
import { MissingRateBadge } from './KpiTile';

const CategoryBadge = ({
  category,
}: {
  category: ConflictEventRow['category'];
}) => (
  <Badge variant={category === 'across_agencies' ? 'default' : 'secondary'}>
    {category === 'across_agencies' ? 'Across agencies' : 'Within agency'}
  </Badge>
);

const ConvertedAmount = ({
  amount,
  currency,
  converted,
  displayCurrency,
}: {
  amount: number;
  currency: string;
  converted: number | null;
  displayCurrency: string;
}) => (
  <span className="whitespace-nowrap tabular-nums">
    {formatCurrency(amount, currency)}
    {currency.toUpperCase() !== displayCurrency.toUpperCase() && (
      <span className="text-muted-foreground">
        {' '}
        {converted != null ? (
          <>→ {formatCurrency(converted, displayCurrency)}</>
        ) : (
          <MissingRateBadge />
        )}
      </span>
    )}
  </span>
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

  if (!data?.blockingBooking) {
    return (
      <p className="py-2 text-sm text-muted-foreground">
        The blocking booking is no longer available.
      </p>
    );
  }

  const booking = data.blockingBooking;

  return (
    <dl className="grid grid-cols-2 gap-x-4 gap-y-1 py-2 text-sm">
      <dt className="text-muted-foreground">Blocking booking by</dt>
      <dd>{data.blockingOrganizationName}</dd>
      <dt className="text-muted-foreground">Booking period</dt>
      <dd>
        {formatDate(booking.startDate)} – {formatDate(booking.endDate)}
      </dd>
      <dt className="text-muted-foreground">Booking value</dt>
      <dd>
        <ConvertedAmount
          amount={booking.amount}
          currency={booking.currency}
          converted={booking.convertedAmount}
          displayCurrency={params.displayCurrency}
        />
      </dd>
      <dt className="text-muted-foreground">Modality</dt>
      <dd>{booking.modality || '—'}</dd>
      <dt className="text-muted-foreground">Rounds</dt>
      <dd>{booking.rounds}</dd>
      <dt className="text-muted-foreground">Booking created</dt>
      <dd>{formatDate(booking.createdAt)}</dd>
    </dl>
  );
};

/**
 * Right-side drill-down sheet: Level 1 is the paginated unique-overlap list,
 * Level 2 expands a row into the blocking booking's metadata. Re-renders with
 * the active display currency while open.
 */
export const DrilldownSheet = ({
  open,
  onOpenChange,
  params,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  params: DashboardApiParams;
}) => {
  const [page, setPage] = useState(1);
  const [expandedId, setExpandedId] = useState<string | null>(null);

  const { data, isLoading } = useConflictEvents(params, page, open);

  const totalPages = data
    ? Math.max(Math.ceil(data.totalCount / data.pageSize), 1)
    : 1;

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent
        side="right"
        className="flex w-full flex-col overflow-y-auto sm:max-w-2xl"
        // Don't auto-focus the first row on open — it paints a focus ring on
        // a row the user never interacted with.
        onOpenAutoFocus={(e) => e.preventDefault()}
      >
        <SheetHeader>
          <SheetTitle>Unique overlaps detected</SheetTitle>
          <SheetDescription>
            Each row is one unique cross-partner overlap
            (fingerprint-deduplicated). Click a row for the blocking
            booking&apos;s details.
          </SheetDescription>
        </SheetHeader>

        {isLoading && (
          <div className="flex flex-1 items-center justify-center py-10">
            <Loader2 className="h-8 w-8 animate-spin" />
          </div>
        )}

        {!isLoading && data && data.data.length === 0 && (
          <p className="py-10 text-center text-sm text-muted-foreground">
            No overlap events in this period.
          </p>
        )}

        {!isLoading && data && data.data.length > 0 && (
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
                        <span className="text-muted-foreground">
                          blocked by
                        </span>{' '}
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
                        First seen {formatDate(event.firstDetectedAt)} · last
                        seen {formatDate(event.lastDetectedAt)} · detected{' '}
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
        )}

        {data && data.totalCount > data.pageSize && (
          <div className="flex items-center justify-between border-t pt-3">
            <Button
              variant="outline"
              size="sm"
              className="focus-visible:ring-0"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              Previous
            </Button>
            <span className="text-sm text-muted-foreground">
              Page {page} of {totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              className="focus-visible:ring-0"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
            </Button>
          </div>
        )}
      </SheetContent>
    </Sheet>
  );
};
