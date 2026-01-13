import { DataTable } from '@/components/DataTable';
import { PageContainer } from '@/components/PageContainer';
import { useBookings } from '@/services/deduplication';
import { usePagination } from '@/helpers/pagination';
import { APP_ROUTE } from '@/helpers/constants';

import { columns } from './columns';
import { useState } from 'react';
import { DateRangePickerFilter } from '@/components/DataTable/DateRangePickerFilter';
import { FilterDropdown } from '@/components/DataTable/FilterDropdown';

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
  console.log(filters);

  const { data: bookings, isLoading: queryLoading } = useBookings({
    ...pagination,
    filters: Object.fromEntries(
      Object.entries(filters).filter(([key]) => key !== 'activity')
    ),
    activity: filters['activity'] || '',
  });

  return (
    <PageContainer
      pageTitle="View Bookings"
      pageSubtitle="On this page you can view all your bookings."
      headerNode={null}
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
    </PageContainer>
  );
};
