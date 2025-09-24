import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import {
  DataWithMeta,
  PaginationContext,
  paginationContextToPaginationRequest,
  PaginationRequest,
  paginationRequestToUrl,
} from '@/helpers/pagination';
import { useGlobalErrors } from '@/providers/GlobalProvider';

import {
  dataToBatchCreateRequest,
  referralPatchToReq,
  referralPostToReq,
  resToBatchCreate,
  resToReferral,
  resToReferralUser,
} from './transformations';
import { api } from '../api';
import { SentReferralFormData } from '@/modules/SentReferrals/SentReferralPage/validations';
import { BatchCreateModalForm } from '@/modules/SentReferrals/SentReferralsPage/components/BatchCreateModal/validation';

import { BatchCreateResponse, Referral, ReferralUser } from './types';

enum QueryKeys {
  Referrals = 'referrals',
  SingleReferral = 'single_referral',
  ReferralUsers = 'referral_users',
}

//
// API calls
//
export const fetchReferrals = async ({
  pagination,
  received = false,
}: {
  pagination: PaginationRequest;
  received?: boolean;
}): Promise<DataWithMeta<Referral>> => {
  const url = paginationRequestToUrl('referrals', pagination);
  const resp = await api.get(url + `&received=${received}`);

  return {
    meta: resp.data.meta,
    data: resp.data.data?.map(resToReferral) ?? [],
  };
};

export const fetchReferralUsers = async (
  pagination: PaginationRequest
): Promise<DataWithMeta<ReferralUser>> => {
  const url = paginationRequestToUrl('referrals/focal-point/users', pagination);

  const resp = await api.get(url);
  return {
    meta: resp.data.meta,
    data: resp.data.data?.map(resToReferralUser) ?? [],
  };
};

const fetchReferral = async (id: string): Promise<Referral> => {
  const resp = await api.get(`/referrals/${id}`);
  return resToReferral(resp.data);
};

const postReferral = async (data: SentReferralFormData): Promise<Referral> => {
  const resp = await api.post(`/referrals`, referralPostToReq(data));
  return resToReferral(resp.data);
};

const patchReferralReason = async (data: {
  referralId: string;
  referralType: string;
  text: string;
}): Promise<Referral> => {
  const resp = await api.patch(
    `/referrals/${data.referralId}/${data.referralType}`,
    { text: data.text }
  );
  return resToReferral(resp.data);
};

const patchReferral = async ({
  referralId,
  data,
}: {
  referralId: string;
  data: Partial<SentReferralFormData>;
}): Promise<Referral> => {
  const resp = await api.patch(
    `/referrals/${referralId}`,
    referralPatchToReq(data)
  );
  return resToReferral(resp.data);
};

const deleteReferral = async (referralId: string): Promise<Referral> => {
  const resp = await api.delete(`/referrals/${referralId}`);
  return resToReferral(resp.data);
};

const postBatchCreateReferrals = async (
  data: BatchCreateModalForm
): Promise<BatchCreateResponse> => {
  const resp = await api.post(
    '/referrals/batch-create',
    dataToBatchCreateRequest(data),
    {
      headers: { 'Content-Type': 'multipart/form-data' },
    }
  );

  return resToBatchCreate(resp.data);
};

export const getReferralsExport = async (
  pagination: PaginationContext,
  received: boolean
): Promise<any> => {
  const url = paginationRequestToUrl(
    'referrals/export',
    paginationContextToPaginationRequest(pagination)
  );

  const resp = await api.get(
    url + (received ? '&received=true' : '&received=false'),
    { responseType: 'blob' }
  );
  return resp.data;
};

export const getReferralsTemplateFile = async (): Promise<any> => {
  const resp = await api.get('/referrals/template', { responseType: 'blob' });
  return resp.data;
};

//
// GET hooks
//

export const useReferrals = ({
  currentPage,
  pageSize,
  sortBy,
  sortDirection,
  debouncedSearch,
  filters,
  received,
}: any) => {
  return useQuery(
    [
      QueryKeys.Referrals,
      currentPage,
      pageSize,
      sortBy,
      sortDirection,
      debouncedSearch,
      filters,
      received,
    ],
    () =>
      fetchReferrals({
        pagination: {
          page: currentPage,
          pageSize,
          sortBy,
          sortDirection,
          search: debouncedSearch,
          filters,
        },
        received,
      })
  );
};

export const useReferralUsers = ({
  currentPage,
  pageSize,
  sortBy,
  sortDirection,
  debouncedSearch,
  filters,
  queryFilters,
}: any) => {
  return useQuery(
    [
      QueryKeys.ReferralUsers,
      currentPage,
      pageSize,
      sortBy,
      sortDirection,
      debouncedSearch,
      filters,
      queryFilters,
    ],
    () =>
      fetchReferralUsers({
        page: currentPage,
        pageSize,
        sortBy,
        sortDirection,
        search: debouncedSearch,
        filters,
        queryFilters,
      })
  );
};

export const useReferral = ({
  id,
  isCreate,
}: {
  id: string;
  isCreate: boolean;
}) => {
  const { onSetCollectionNotFound } = useGlobalErrors();

  return useQuery([QueryKeys.SingleReferral, id], () => fetchReferral(id), {
    enabled: !isCreate,
    onError: () => onSetCollectionNotFound(true),
  });
};

//
// Mutation hooks
//

export const useReferralMutation = () => {
  const queryClient = useQueryClient();

  return {
    createReferral: useMutation(postReferral, {
      onSuccess: () => queryClient.invalidateQueries([QueryKeys.Referrals]),
    }),
    updateReferralReason: useMutation(patchReferralReason, {
      onSuccess: () => {
        queryClient.invalidateQueries([QueryKeys.Referrals]);
        queryClient.invalidateQueries([QueryKeys.SingleReferral]);
      },
    }),
    patchReferral: useMutation(patchReferral, {
      onSuccess: () => {
        queryClient.invalidateQueries([QueryKeys.Referrals]);
        queryClient.invalidateQueries([QueryKeys.SingleReferral]);
      },
    }),
    removeReferral: useMutation(deleteReferral, {
      onSuccess: () => queryClient.invalidateQueries([QueryKeys.Referrals]),
    }),
    batchCreateReferrals: useMutation(postBatchCreateReferrals, {
      onSuccess: () => queryClient.invalidateQueries([QueryKeys.Referrals]),
    }),
  };
};
