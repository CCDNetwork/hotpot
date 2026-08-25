import { useQuery } from '@tanstack/react-query';

import { api } from '@/services';

import {
  BlockingPartnerRow,
  ConflictEventDetail,
  ConflictEventList,
  ConsecutiveMonthsDrillResponse,
  ConsecutiveMonthsHistogram,
  DashboardApiParams,
  DuplicatesSplit,
  DuplicatesSummary,
  DuplicatesTrendPoint,
  ModalityRow,
  OrganizationDrillRow,
  OverviewSummary,
  OverviewTrend,
  PartnerRow,
  PrebookingRunDrillResponse,
  RecordsCheckedDrillResponse,
  TrendGranularity,
  ValueFxDrillResponse,
} from './types';

enum QueryKeys {
  OverviewSummary = 'dashboard_overview_summary',
  OverviewTrend = 'dashboard_overview_trend',
  OverviewPartners = 'dashboard_overview_partners',
  OverviewModality = 'dashboard_overview_modality',
  DuplicatesSummary = 'dashboard_duplicates_summary',
  DuplicatesTrend = 'dashboard_duplicates_trend',
  DuplicatesSplit = 'dashboard_duplicates_split',
  BlockingPartners = 'dashboard_blocking_partners',
  ConflictEvents = 'dashboard_conflict_events',
  ConflictEvent = 'dashboard_conflict_event',
  ValueFxDrill = 'dashboard_drill_value_fx',
  OrganizationsDrill = 'dashboard_drill_organizations',
  PrebookingRunsDrill = 'dashboard_drill_prebooking_runs',
  RecordsCheckedDrill = 'dashboard_drill_records_checked',
  ConsecutiveMonthsHistogram = 'dashboard_consecutive_months',
  ConsecutiveMonthsDetails = 'dashboard_consecutive_months_details',
}

const commonOptions = { keepPreviousData: true } as const;

export const useOverviewSummary = (params: DashboardApiParams) =>
  useQuery(
    [QueryKeys.OverviewSummary, params],
    async (): Promise<OverviewSummary> =>
      (await api.get('/dashboards/overview/summary', { params })).data,
    commonOptions
  );

export const useOverviewTrend = (
  params: DashboardApiParams,
  granularity: TrendGranularity
) =>
  useQuery(
    [QueryKeys.OverviewTrend, params, granularity],
    async (): Promise<OverviewTrend> =>
      (
        await api.get('/dashboards/overview/trend', {
          params: { ...params, granularity },
        })
      ).data,
    commonOptions
  );

export const useOverviewPartners = (params: DashboardApiParams) =>
  useQuery(
    [QueryKeys.OverviewPartners, params],
    async (): Promise<PartnerRow[]> =>
      (await api.get('/dashboards/overview/partners', { params })).data,
    commonOptions
  );

export const useOverviewModality = (params: DashboardApiParams) =>
  useQuery(
    [QueryKeys.OverviewModality, params],
    async (): Promise<ModalityRow[]> =>
      (await api.get('/dashboards/overview/modality', { params })).data,
    commonOptions
  );

export const useDuplicatesSummary = (params: DashboardApiParams) =>
  useQuery(
    [QueryKeys.DuplicatesSummary, params],
    async (): Promise<DuplicatesSummary> =>
      (await api.get('/dashboards/duplicates/summary', { params })).data,
    commonOptions
  );

export const useDuplicatesTrend = (
  params: DashboardApiParams,
  granularity: TrendGranularity
) =>
  useQuery(
    [QueryKeys.DuplicatesTrend, params, granularity],
    async (): Promise<DuplicatesTrendPoint[]> =>
      (
        await api.get('/dashboards/duplicates/trend', {
          params: { ...params, granularity },
        })
      ).data,
    commonOptions
  );

export const useDuplicatesSplit = (params: DashboardApiParams) =>
  useQuery(
    [QueryKeys.DuplicatesSplit, params],
    async (): Promise<DuplicatesSplit> =>
      (await api.get('/dashboards/duplicates/split', { params })).data,
    commonOptions
  );

export const useBlockingPartners = (params: DashboardApiParams) =>
  useQuery(
    [QueryKeys.BlockingPartners, params],
    async (): Promise<BlockingPartnerRow[]> =>
      (await api.get('/dashboards/duplicates/blocking-partners', { params }))
        .data,
    commonOptions
  );

export const useConflictEvents = (
  params: DashboardApiParams,
  page: number,
  enabled: boolean
) =>
  useQuery(
    [QueryKeys.ConflictEvents, params, page],
    async (): Promise<ConflictEventList> =>
      (
        await api.get('/dashboards/duplicates/events', {
          params: { ...params, page, pageSize: 20 },
        })
      ).data,
    { ...commonOptions, enabled }
  );

export const useConflictEvent = (
  params: DashboardApiParams,
  id: string | null
) =>
  useQuery(
    [QueryKeys.ConflictEvent, params, id],
    async (): Promise<ConflictEventDetail> =>
      (
        await api.get(`/dashboards/duplicates/events/${id}`, {
          params: { displayCurrency: params.displayCurrency },
        })
      ).data,
    { enabled: !!id }
  );

export const useValueFxDrill = (
  params: DashboardApiParams,
  source: 'bookings' | 'conflicts',
  enabled: boolean
) =>
  useQuery(
    [QueryKeys.ValueFxDrill, params, source],
    async (): Promise<ValueFxDrillResponse> =>
      (
        await api.get('/dashboards/drills/value', {
          params: { ...params, source },
        })
      ).data,
    { ...commonOptions, enabled }
  );

export const useOrganizationsDrill = (
  params: DashboardApiParams,
  enabled: boolean
) =>
  useQuery(
    [QueryKeys.OrganizationsDrill, params],
    async (): Promise<OrganizationDrillRow[]> =>
      (
        await api.get('/dashboards/overview/drills/organizations', {
          params,
        })
      ).data,
    { ...commonOptions, enabled }
  );

export const usePrebookingRunsDrill = (
  params: DashboardApiParams,
  page: number,
  enabled: boolean
) =>
  useQuery(
    [QueryKeys.PrebookingRunsDrill, params, page],
    async (): Promise<PrebookingRunDrillResponse> =>
      (
        await api.get('/dashboards/duplicates/drills/prebooking-runs', {
          params: { ...params, page, pageSize: 25 },
        })
      ).data,
    { ...commonOptions, enabled }
  );

export const useRecordsCheckedDrill = (
  params: DashboardApiParams,
  page: number,
  enabled: boolean
) =>
  useQuery(
    [QueryKeys.RecordsCheckedDrill, params, page],
    async (): Promise<RecordsCheckedDrillResponse> =>
      (
        await api.get('/dashboards/duplicates/drills/records-checked', {
          params: { ...params, page, pageSize: 25 },
        })
      ).data,
    { ...commonOptions, enabled }
  );

// Consecutive-months histogram intentionally takes no params — the server
// pins it to trailing 12 months, platform-wide (cross-org linkage is the
// whole point of the metric).
export const useConsecutiveMonthsHistogram = () =>
  useQuery(
    [QueryKeys.ConsecutiveMonthsHistogram],
    async (): Promise<ConsecutiveMonthsHistogram> =>
      (await api.get('/dashboards/overview/consecutive-months')).data,
    commonOptions
  );

export const useConsecutiveMonthsDetails = (
  bucket: string,
  page: number,
  enabled: boolean
) =>
  useQuery(
    [QueryKeys.ConsecutiveMonthsDetails, bucket, page],
    async (): Promise<ConsecutiveMonthsDrillResponse> =>
      (
        await api.get('/dashboards/overview/consecutive-months/details', {
          params: { bucket, page, pageSize: 25 },
        })
      ).data,
    { ...commonOptions, enabled }
  );
