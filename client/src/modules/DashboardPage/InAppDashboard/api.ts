import { useQuery } from '@tanstack/react-query';

import { api } from '@/services';

import {
  BlockingPartnerRow,
  ConflictEventDetail,
  ConflictEventList,
  DashboardApiParams,
  DuplicatesSplit,
  DuplicatesSummary,
  DuplicatesTrendPoint,
  ModalityRow,
  OverviewSummary,
  OverviewTrend,
  PartnerRow,
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
}

const commonOptions = { keepPreviousData: true } as const;

export const useOverviewSummary = (params: DashboardApiParams) =>
  useQuery(
    [QueryKeys.OverviewSummary, params],
    async (): Promise<OverviewSummary> =>
      (await api.get('/dashboards/overview/summary', { params })).data,
    commonOptions
  );

export const useOverviewTrend = (params: DashboardApiParams) =>
  useQuery(
    [QueryKeys.OverviewTrend, params],
    async (): Promise<OverviewTrend> =>
      (await api.get('/dashboards/overview/trend', { params })).data,
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

export const useDuplicatesTrend = (params: DashboardApiParams) =>
  useQuery(
    [QueryKeys.DuplicatesTrend, params],
    async (): Promise<DuplicatesTrendPoint[]> =>
      (await api.get('/dashboards/duplicates/trend', { params })).data,
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
