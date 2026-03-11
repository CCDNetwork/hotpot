import { DataTable } from '@/components/DataTable';
import { PageContainer } from '@/components/PageContainer';
import {
  useBookings,
  useReleaseBookingMutation,
  useBatchReleaseBookingsMutation,
  getBookingsExport,
} from '@/services/deduplication';
import { usePagination } from '@/helpers/pagination';
import { downloadFile } from '@/helpers/common';
import { APP_ROUTE } from '@/helpers/constants';

import { getColumns } from './columns';
import { useRef, useState } from 'react';
import { DateRangePickerFilter } from '@/components/DataTable/DateRangePickerFilter';
import { FilterDropdown } from '@/components/DataTable/FilterDropdown';
import { Button } from '@/components/ui/button';
import {
  DownloadIcon,
  FileDownIcon,
  FileTextIcon,
  UploadIcon,
} from 'lucide-react';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { ConfirmationDialog } from '@/components/ConfirmationDialog';
import { toast } from '@/components/ui/use-toast';

export const ViewBookingPage = () => {
  const pagination = usePagination();
  const {
    currentPage,
    onPageChange,
    onPageSizeChange,
    onSortChange,
    onSearchChange,
  } = pagination;

  const [filters, setFilters] = useState<Record<string, string>>({});
  const [releaseBookingId, setReleaseBookingId] = useState<string | null>(null);
  const [exportLoading, setExportLoading] = useState(false);
  const [batchReleaseFile, setBatchReleaseFile] = useState<File | null>(null);
  const batchReleaseInputRef = useRef<HTMLInputElement>(null);

  const releaseMutation = useReleaseBookingMutation();
  const batchReleaseMutation = useBatchReleaseBookingsMutation();

  const { data: bookings, isLoading: queryLoading } = useBookings({
    ...pagination,
    filters: Object.fromEntries(
      Object.entries(filters).filter(([key]) => key !== 'activity')
    ),
    activity: filters['activity'] || '',
  });

  const onExportBookingsClick = async () => {
    setExportLoading(true);
    try {
      const exportedData = await getBookingsExport(
        {
          page: 1,
          pageSize: 999,
          sortBy: pagination.sortBy || 'createdAt',
          sortDirection: pagination.sortDirection,
          search: pagination.debouncedSearch,
          filters: Object.fromEntries(
            Object.entries(filters).filter(([key]) => key !== 'activity')
          ),
        },
        filters['activity'] || ''
      );
      downloadFile(exportedData, 'bookings-export');
    } catch (error: any) {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage ||
          'An error has occurred. Please try again.',
      });
    }
    setExportLoading(false);
  };

  const handleBatchRelease = async () => {
    if (!batchReleaseFile) return;

    try {
      const result = await batchReleaseMutation.mutateAsync({
        file: batchReleaseFile,
      });
      setBatchReleaseFile(null);
      toast({
        title: 'Batch release complete',
        description: `${result.released} released, ${result.skipped} skipped out of ${result.total}`,
      });
    } catch {
      toast({
        title: 'Batch release failed',
        description: 'Something went wrong. Please try again.',
        variant: 'destructive',
      });
    }
  };

  const columns = getColumns(setReleaseBookingId);

  const handleRelease = async () => {
    if (!releaseBookingId) return;

    try {
      await releaseMutation.mutateAsync(releaseBookingId);
      setReleaseBookingId(null);
      toast({
        title: 'Booking released',
        description:
          'The booking has been released. The household is now available for re-booking.',
      });
    } catch {
      toast({
        title: 'Failed to release booking',
        description: 'Something went wrong. Please try again.',
        variant: 'destructive',
      });
    }
  };

  return (
    <PageContainer
      pageTitle="View Bookings"
      pageSubtitle="On this page you can view all your bookings."
      headerNode={
        <div className="flex gap-2">
          <input
            ref={batchReleaseInputRef}
            type="file"
            accept=".xlsx"
            className="hidden"
            onChange={(e) => {
              const file = e.target.files?.[0];
              if (file) setBatchReleaseFile(file);
              e.target.value = '';
            }}
          />
          <Button
            type="button"
            variant="outline"
            onClick={() => batchReleaseInputRef.current?.click()}
          >
            <UploadIcon className="mr-2 size-4" />
            Batch Release
          </Button>
          <Button
            type="button"
            variant="outline"
            isLoading={exportLoading}
            disabled={exportLoading}
            onClick={onExportBookingsClick}
          >
            <FileDownIcon className="mr-2 size-4" />
            Export Bookings
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger>
              <Button type="button">
                <DownloadIcon className="mr-2 w-4 h-4" />
                Download template
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent>
              <DropdownMenuItem className="p-0">
                <a
                  href="/booking-empty-upload-template-1.xlsx"
                  download
                  className="flex gap-1 items-center px-2 py-1"
                >
                  <FileDownIcon className="w-4 h-4" />
                  Empty template
                </a>
              </DropdownMenuItem>
              <DropdownMenuItem className="p-0">
                <a
                  href="/booking-template-with-readme-1.xlsx"
                  download
                  className="flex gap-1 items-center px-2 py-1"
                >
                  <FileTextIcon className="w-4 h-4" />
                  Template with readme
                </a>
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      }
      breadcrumbs={[
        {
          href: `${APP_ROUTE.ViewBooking}`,
          name: 'View Bookings',
        },
      ]}
    >
      <DataTable
        data={bookings?.data ?? []}
        pagination={bookings?.meta}
        isQueryLoading={queryLoading}
        currentPage={currentPage}
        pageClicked={onPageChange}
        pageSizeClicked={onPageSizeChange}
        headerClicked={onSortChange}
        onSearchChange={onSearchChange}
        columns={columns}
        tableFilterNodes={
          <div className="flex flex-wrap gap-4">
            <FilterDropdown
              currentFilters={filters}
              filterName="activity"
              setCurrentFilters={setFilters}
              title="Filter by activity"
              options={[
                { label: 'Previous', value: 'previous' },
                { label: 'Current', value: 'current' },
                { label: 'Upcoming', value: 'upcoming' },
                { label: 'Released', value: 'released' },
              ]}
            />
            <DateRangePickerFilter
              setCurrentFilters={setFilters}
              filterNameFrom="startDate[gt]"
              filterNameTo="endDate[lt]"
              placeholder="Filter by start and end date"
            />
            <DateRangePickerFilter
              setCurrentFilters={setFilters}
              placeholder="Filter by date of booking"
            />
          </div>
        }
      />

      <ConfirmationDialog
        open={!!batchReleaseFile}
        title="Batch Release Bookings"
        body={`Are you sure you want to batch release bookings from "${batchReleaseFile?.name}"? Matching bookings will have their start and end dates cleared. This action cannot be undone.`}
        confirmButtonLabel="Release"
        actionButtonVariant="destructive"
        confirmButtonLoading={batchReleaseMutation.isLoading}
        onCancel={() => setBatchReleaseFile(null)}
        onAction={handleBatchRelease}
      />

      <ConfirmationDialog
        open={!!releaseBookingId}
        title="Release Booking"
        body="Are you sure you want to release this booking? This will nullify the start and end dates, freeing the household for re-booking. This action cannot be undone."
        confirmButtonLabel="Release"
        actionButtonVariant="destructive"
        confirmButtonLoading={releaseMutation.isLoading}
        onCancel={() => setReleaseBookingId(null)}
        onAction={handleRelease}
      />
    </PageContainer>
  );
};
