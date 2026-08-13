export type DashboardPeriod =
  | '7d'
  | '30d'
  | '90d'
  | 'current-month'
  | 'all-time';
export type DisplayCurrency = 'EUR' | 'USD';
export type ConversionStatusValue = 'ok' | 'partial' | 'missing_rate';

export type DashboardApiParams = {
  period: DashboardPeriod;
  displayCurrency: DisplayCurrency;
  organizationId?: string;
};

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
  activePartners: number;
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
