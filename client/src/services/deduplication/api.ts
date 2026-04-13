import {
  DataWithMeta,
  PaginationRequest,
  paginationRequestToUrl,
  SortDirection,
} from '@/helpers/pagination';
import { useBookingProvider } from '@/modules/BookingPage';
import { useDeduplicationProvider } from '@/modules/DeduplicationPage';
import { api } from '@/services';
import {
  dataToDatasetRequest,
  resToBooking,
  resToDatasetResponse,
  resToDeduplicationListing,
  resToSameOrgDedupResponse,
  resToSystemDedupeResponse,
} from '@/services/deduplication/transformations';
import {
  BatchReleaseResponse,
  BookingDataset,
  DeduplicationDataset,
  DeduplicationListing,
  SameOrgDedupeResponse,
  SystemOrgDedupeResponse,
} from '@/services/deduplication/types';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

enum QueryKeys {
  DeduplicationListings = 'deduplication-listings',
  Bookings = 'bookings',
}

export const fetchDeduplicationListings = async (
  pagination: PaginationRequest
): Promise<DataWithMeta<DeduplicationListing>> => {
  const url = paginationRequestToUrl('deduplication/listings', pagination);

  const resp = await api.get(url);
  return {
    meta: resp.data.meta,
    data: resp.data.data?.map(resToDeduplicationListing) ?? [],
  };
};

export const fetchBookings = async (
  pagination: PaginationRequest,
  activity: string
): Promise<DataWithMeta<DeduplicationListing>> => {
  let url = paginationRequestToUrl('deduplication/bookings', pagination);
  if (activity) {
    url += `&activity=${activity}`;
  }

  const resp = await api.get(url);
  return {
    meta: resp.data.meta,
    data: resp.data.data?.map(resToBooking) ?? [],
  };
};

export const getBookingsExport = async (
  pagination: PaginationRequest,
  activity: string
): Promise<any> => {
  let url = paginationRequestToUrl('deduplication/bookings/export', pagination);
  if (activity) {
    url += `&activity=${activity}`;
  }

  const resp = await api.get(url, { responseType: 'blob' });
  return resp.data;
};

const postReleaseBooking = async (bookingId: string): Promise<void> => {
  await api.post(`/deduplication/booking/${bookingId}/release`);
};

const postBatchReleaseBookings = async (data: {
  file: File;
}): Promise<BatchReleaseResponse> => {
  const formData = new FormData();
  formData.append('file', data.file);
  const resp = await api.post(
    '/deduplication/booking/batch-release',
    formData,
    {
      headers: { 'Content-Type': 'multipart/form-data' },
    }
  );
  return resp.data;
};

export const deleteDeduplicationData = async (): Promise<object> => {
  const resp = await api.delete('/deduplication');
  return resp.data;
};

const postDeduplicationDataset = async (data: {
  file: File;
  templateId: string;
}): Promise<DeduplicationDataset> => {
  const resp = await api.post(
    '/deduplication/dataset',
    dataToDatasetRequest(data),
    {
      headers: { 'Content-Type': 'multipart/form-data' },
    }
  );

  return resToDatasetResponse(resp.data);
};

const postDeduplicationSameOrganization = async (data: {
  fileId: string;
  templateId: string;
}): Promise<SameOrgDedupeResponse> => {
  const resp = await api.post('/deduplication/same-organization', data);

  return resToSameOrgDedupResponse(resp.data);
};

const postDeduplicationSystemOrganizations = async (data: {
  fileId: string;
  templateId: string;
}): Promise<SystemOrgDedupeResponse> => {
  const resp = await api.post('/deduplication/system-organizations', data);

  return resToSystemDedupeResponse(resp.data);
};

const postDeduplicationFinish = async (data: {
  fileId: string;
  templateId: string;
}): Promise<DeduplicationDataset> => {
  const resp = await api.post('/deduplication/finish', data);

  return resToDatasetResponse(resp.data);
};

const postBookingStep1 = async (data: {
  file: File;
}): Promise<BookingDataset> => {
  const resp = await api.post('/deduplication/booking/step-1', data, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });

  return resp.data;
};

const postBookingStep2 = async (data: {
  fileId: string;
  isPrebooking?: boolean;
}): Promise<BookingDataset> => {
  const resp = await api.post('/deduplication/booking/step-2', data);

  return resp.data;
};

const postWizardFinish = async (data: { fileId: string }): Promise<void> => {
  await api.post('/deduplication/wizard-finish', data);
};

export const useWizardFinishMutation = () => {
  return useMutation(postWizardFinish, {
    onError: (error) => {
      console.error('wizard-finish cleanup failed', error);
    },
  });
};

export const useDeduplicationListings = ({
  currentPage,
  pageSize,
  sortBy = 'createdAt',
  sortDirection = SortDirection.Desc,
  debouncedSearch,
}: any) => {
  return useQuery(
    [
      QueryKeys.DeduplicationListings,
      currentPage,
      pageSize,
      sortBy,
      sortDirection,
      debouncedSearch,
    ],
    () =>
      fetchDeduplicationListings({
        page: currentPage,
        pageSize,
        sortBy,
        sortDirection,
        search: debouncedSearch,
      })
  );
};

export const useBookings = ({
  currentPage,
  pageSize,
  sortBy = 'createdAt',
  sortDirection = SortDirection.Desc,
  debouncedSearch,
  filters,
  activity,
}: any) => {
  return useQuery(
    [
      QueryKeys.Bookings,
      currentPage,
      pageSize,
      sortBy,
      sortDirection,
      debouncedSearch,
      filters,
      activity,
    ],
    () =>
      fetchBookings(
        {
          page: currentPage,
          pageSize,
          sortBy,
          sortDirection,
          search: debouncedSearch,
          filters,
        },
        activity
      )
  );
};

export const useReleaseBookingMutation = () => {
  const queryClient = useQueryClient();

  return useMutation(postReleaseBooking, {
    onSuccess: () => {
      queryClient.invalidateQueries([QueryKeys.Bookings]);
    },
  });
};

export const useBatchReleaseBookingsMutation = () => {
  const queryClient = useQueryClient();

  return useMutation(postBatchReleaseBookings, {
    onSuccess: () => {
      queryClient.invalidateQueries([QueryKeys.Bookings]);
    },
  });
};

export const useBookingMutation = () => {
  const { setBookingWizardError } = useBookingProvider();

  return {
    bookingStep1: useMutation(postBookingStep1, {
      onError: (error) => setBookingWizardError(error),
    }),
    bookingStep2: useMutation(postBookingStep2, {
      onError: (error) => setBookingWizardError(error),
    }),
  };
};

export const usePrebookingMutation = (setError: (error: any) => void) => {
  return {
    bookingStep1: useMutation(postBookingStep1, {
      onError: (error) => setError(error),
    }),
    bookingStep2: useMutation(
      (data: { fileId: string }) =>
        postBookingStep2({ ...data, isPrebooking: true }),
      {
        onError: (error) => setError(error),
      }
    ),
  };
};

export const useDeduplicationMutation = () => {
  const queryClient = useQueryClient();
  const { setDeduplicationWizardError } = useDeduplicationProvider();

  return {
    deduplicateFile: useMutation(postDeduplicationDataset, {
      onError: (error) => setDeduplicationWizardError(error),
    }),
    deduplicateSameOrganization: useMutation(
      postDeduplicationSameOrganization,
      {
        onError: (error) => setDeduplicationWizardError(error),
      }
    ),
    deduplicateSystemOrganizations: useMutation(
      postDeduplicationSystemOrganizations,
      {
        onError: (error) => setDeduplicationWizardError(error),
      }
    ),
    deduplicateFinish: useMutation(postDeduplicationFinish, {
      onSuccess: () =>
        queryClient.invalidateQueries([QueryKeys.DeduplicationListings]),
      onError: (error) => setDeduplicationWizardError(error),
    }),
    bookingStep1: useMutation(postBookingStep1, {
      onError: (error) => setDeduplicationWizardError(error),
    }),
    bookingStep2: useMutation(postBookingStep2, {
      onError: (error) => setDeduplicationWizardError(error),
    }),
  };
};
