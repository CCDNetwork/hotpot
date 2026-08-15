export type DashboardPeriod =
  | '7d'
  | '30d'
  | '90d'
  | 'current-month'
  | 'all-time';
export type DisplayCurrency = 'EUR' | 'USD' | 'ILS';
export type ConversionStatusValue = 'ok' | 'partial' | 'missing_rate';

export type DashboardApiParams = {
  period: DashboardPeriod;
  displayCurrency: DisplayCurrency;
  organizationId?: string;
};

// Cross-org only (default) hides overlaps where the requesting and blocking
// organization are the same — those tend to be data-entry issues inside one
// org, not coordination failures. Kept as a dedup-tab-local filter so
// switching doesn't invalidate the Overview queries.
export type OverlapScope = 'cross' | 'all';
export type DedupApiParams = DashboardApiParams & { overlapScope: OverlapScope };

export type CurrencyAmount = {
  currency: string;
  amount: number;
};

export type DisplayAmount = {
  currency: string;
  amount: number | null;
  conversionStatus: ConversionStatusValue;
};

export type CurrencyBreakdownRow = {
  currency: string;
  native: number;
  converted: number | null;
  conversionStatus: ConversionStatusValue;
  count: number;
};

export type ValueSummary = {
  native: CurrencyAmount[];
  display: DisplayAmount;
  breakdown: CurrencyBreakdownRow[];
};

export type AvgTransferBreakdownRow = {
  currency: string;
  avgNative: number;
  avgConverted: number | null;
  conversionStatus: ConversionStatusValue;
  avgRounds: number;
  bookings: number;
};

export type AvgTransfer = {
  display: DisplayAmount;
  breakdown: AvgTransferBreakdownRow[];
};

export type OverviewSummary = {
  householdsAssisted: number;
  individualsReached: number;
  activeOrganizations: number;
  totalOnboarded: number;
  valueTransferred: ValueSummary;
  avgTransfer: AvgTransfer;
};

export type TrendPoint = {
  bucket: string;
  count: number;
};

export type OverviewTrend = {
  households: TrendPoint[];
  newPartners: TrendPoint[];
};

export type PartnerRow = {
  organizationId: string;
  organizationName: string;
  households: number;
  nativeCurrency: string;
  nativeAmount: number;
  convertedAmount: number | null;
  conversionStatus: ConversionStatusValue;
  bookings: number;
};

export type ModalityRow = {
  modality: string;
  households: number;
  share: number;
};

export type DuplicatesSummary = {
  uniqueOverlaps: number;
  prebookingRuns: number;
  householdRecordsChecked: number;
  overlapRate: number | null;
  valueOfOverlaps: ValueSummary;
};

export type DuplicatesTrendPoint = {
  bucket: string;
  uniqueOverlaps: number;
  householdRecordsChecked: number;
};

export type DuplicatesSplit = {
  withinAgency: number;
  acrossAgency: number;
};

export type BlockingPartnerRow = {
  organizationId: string;
  organizationName: string;
  overlapsCaused: number;
};

export type ConflictEventCategory = 'within_same_agency' | 'across_agencies';

export type ConflictEventRow = {
  id: string;
  subjectLabel: string;
  requestingOrganizationId: string;
  requestingOrganizationName: string;
  blockingOrganizationId: string;
  blockingOrganizationName: string;
  overlapStartDate: string;
  overlapEndDate: string;
  proposedAmount: number;
  proposedCurrency: string;
  convertedAmount: number | null;
  conversionStatus: ConversionStatusValue;
  proposedModality: string;
  proposedRounds: number;
  firstDetectedAt: string;
  lastDetectedAt: string;
  detectionCount: number;
  category: ConflictEventCategory;
};

export type ConflictEventList = {
  data: ConflictEventRow[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type BlockingBooking = {
  startDate: string | null;
  endDate: string | null;
  amount: number;
  currency: string;
  convertedAmount: number | null;
  conversionStatus: ConversionStatusValue;
  modality: string;
  rounds: number;
  createdAt: string;
};

export type ConflictEventDetail = ConflictEventRow & {
  blockingBooking: BlockingBooking | null;
};

/**
 * Discriminated union identifying which drill the sheet should render.
 * `null` closes the sheet. The `label` is the human-readable name of the
 * source tile — used in the sheet header so the user knows which aggregate
 * they're auditing.
 */
export type DrillTarget =
  | {
      type: 'unique-overlaps';
      label: string;
      // Optional: when set, the drill opens with this event pre-expanded.
      // Only takes effect if the event appears on the first page.
      focusEventId?: string;
    }
  | { type: 'value-fx'; source: 'bookings' | 'conflicts'; label: string }
  | { type: 'organizations'; label: string }
  | { type: 'prebooking-runs'; label: string }
  | { type: 'records-checked'; label: string }
  | { type: 'consecutive-months'; bucket: string; label: string };

export type ValueFxRow = {
  currency: string;
  year: number;
  month: number;
  native: number;
  rate: number | null;
  converted: number | null;
  count: number;
  conversionStatus: ConversionStatusValue;
};

export type ValueFxDrillResponse = {
  displayCurrency: string;
  rows: ValueFxRow[];
  totals: {
    native: CurrencyAmount[];
    converted: number | null;
    conversionStatus: ConversionStatusValue;
  };
};

export type OrganizationDrillRow = {
  organizationId: string;
  organizationName: string;
  bookingsCount: number;
  householdsCount: number;
  nativeCurrency: string;
  nativeAmount: number;
  convertedAmount: number | null;
  conversionStatus: ConversionStatusValue;
};

export type PrebookingRunRow = {
  submissionId: string;
  uploadedAt: string;
  uploadedByName: string;
  organizationName: string;
  totalRows: number;
  successRows: number;
  failedRows: number;
};

export type PrebookingRunDrillResponse = {
  data: PrebookingRunRow[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type RecordsCheckedRow = {
  id: string;
  submissionId: string | null;
  createdAt: string;
  organizationName: string;
  isSuccess: boolean;
  currency: string | null;
  amount: number | null;
  startDate: string | null;
  endDate: string | null;
};

export type RecordsCheckedDrillResponse = {
  data: RecordsCheckedRow[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type ConsecutiveMonthsBucket = {
  bucket: string; // "1".."5" | "6+"
  episodes: number;
};

export type ConsecutiveMonthsHistogram = {
  windowStart: string;
  windowEnd: string;
  buckets: ConsecutiveMonthsBucket[];
};

export type ConsecutiveMonthsRun = {
  householdIdMasked: string;
  runStartMonth: string;
  runEndMonth: string;
  monthsCount: number;
  organizationNames: string[];
};

export type ConsecutiveMonthsDrillResponse = {
  bucket: string;
  data: ConsecutiveMonthsRun[];
  page: number;
  pageSize: number;
  totalCount: number;
};
