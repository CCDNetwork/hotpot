import { DataTable } from '@/components/DataTable';
import { PageContainer } from '@/components/PageContainer';
import {
  useBookings,
  useReleaseBookingMutation,
  useBookings,
  useReleaseBookingMutation,
} from '@/services/deduplication';
import { usePagination } from '@/helpers/pagination';
import { APP_ROUTE } from '@/helpers/constants';

import { getColumns } from './columns';
import { useState } from 'react';
import { DateRangePickerFilter } from '@/components/DataTable/DateRangePickerFilter';
import { FilterDropdown } from '@/components/DataTable/FilterDropdown';
import { Button } from '@/components/ui/button';
import { DownloadIcon, FileDownIcon, FileTextIcon } from 'lucide-react';
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

  const releaseMutation = useReleaseBookingMutation();

  const { data: bookings, isLoading: queryLoading } = useBookings({
    ...pagination,
    filters: Object.fromEntries(
      Object.entries(filters).filter(([key]) => key !== 'activity')
    ),
    activity: filters['activity'] || '',
  });

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
