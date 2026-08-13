import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';

import { DashboardApiParams, DrillTarget } from '../types';
import { ConflictEventsDrill } from './drills/ConflictEventsDrill';
import { ValueFxDrill } from './drills/ValueFxDrill';
import { OrganizationsDrill } from './drills/OrganizationsDrill';
import { PrebookingRunsDrill } from './drills/PrebookingRunsDrill';
import { RecordsCheckedDrill } from './drills/RecordsCheckedDrill';

const describe = (target: DrillTarget): { title: string; description: string } => {
  switch (target.type) {
    case 'unique-overlaps':
      return {
        title: target.label,
        description:
          "Each row is one unique cross-organization overlap (fingerprint-deduplicated). Click a row for the blocking booking's details.",
      };
    case 'value-fx':
      return {
        title: target.label,
        description:
          target.source === 'conflicts'
            ? 'Per-currency breakdown of the overlap amounts, with the InforEuro monthly rate used for each conversion.'
            : 'Per-currency breakdown of the transferred amounts, with the InforEuro monthly rate used for each conversion.',
      };
    case 'organizations':
      return {
        title: target.label,
        description: 'Organizations that have booked in this period, with their totals.',
      };
    case 'prebooking-runs':
      return {
        title: target.label,
        description: 'Each submission the wizard has run in this period, with success and failure counts.',
      };
    case 'records-checked':
      return {
        title: target.label,
        description: 'Every household row that went through the pre-booking wizard, one per row.',
      };
  }
};

const renderBody = (target: DrillTarget, params: DashboardApiParams, open: boolean) => {
  switch (target.type) {
    case 'unique-overlaps':
      return (
        <ConflictEventsDrill
          params={params}
          open={open}
          focusEventId={target.focusEventId}
        />
      );
    case 'value-fx':
      return <ValueFxDrill params={params} open={open} source={target.source} />;
    case 'organizations':
      return <OrganizationsDrill params={params} open={open} />;
    case 'prebooking-runs':
      return <PrebookingRunsDrill params={params} open={open} />;
    case 'records-checked':
      return <RecordsCheckedDrill params={params} open={open} />;
  }
};

/**
 * Right-side drill-down sheet. Dispatches on `target.type` to the matching
 * drill body; sheet chrome + title/description live here. `null` target keeps
 * the sheet closed. Body queries are gated on `open` so they don't run while
 * the sheet is hidden.
 */
export const DrilldownSheet = ({
  target,
  onClose,
  params,
}: {
  target: DrillTarget | null;
  onClose: () => void;
  params: DashboardApiParams;
}) => {
  const open = target !== null;

  return (
    <Sheet open={open} onOpenChange={(next) => { if (!next) onClose(); }}>
      <SheetContent
        side="right"
        className="flex w-full flex-col overflow-y-auto sm:max-w-2xl"
        onOpenAutoFocus={(e) => e.preventDefault()}
      >
        {target && (
          <>
            <SheetHeader>
              <SheetTitle>{describe(target).title}</SheetTitle>
              <SheetDescription>{describe(target).description}</SheetDescription>
            </SheetHeader>
            {renderBody(target, params, open)}
          </>
        )}
      </SheetContent>
    </Sheet>
  );
};
