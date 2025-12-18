import { DataTable } from '@/components/DataTable';
import { PageContainer } from '@/components/PageContainer';
import { useBookings } from '@/services/deduplication';
import { usePagination } from '@/helpers/pagination';
import { APP_ROUTE } from '@/helpers/constants';

import { columns } from './columns';

export const ViewBookingPage = () => {
  const pagination = usePagination();
  const {
    currentPage,
    onPageChange,
    onPageSizeChange,
    onSortChange,
    onSearchChange,
  } = pagination;

  const { data: bookings, isLoading: queryLoading } = useBookings(pagination);

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
      />
    </PageContainer>
  );
};
