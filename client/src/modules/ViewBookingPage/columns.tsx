import { formatDate } from 'date-fns';

import { TableColumn } from '@/components/DataTable/types';
import { BookingResponse } from '@/services/deduplication';

export const columns: TableColumn<BookingResponse>[] = [
  {
    accessorKey: 'householdId',
    id: 'householdId',
    isSortable: true,
    header: 'Head of Household',
  },
  {
    accessorKey: 'spouseId',
    id: 'spouseId',
    isSortable: true,
    header: 'ID Spouse',
    cell: ({ row }) => {
      const spouseId = row.original.spouseId;
      return <div className="flex flex-col">{spouseId || '-'}</div>;
    },
  },
  {
    accessorKey: 'createdAt',
    isSortable: true,
    id: 'createdAt',
    header: 'Date of booking',
    cell: ({ row }) => {
      const createdAt = row.original.createdAt;

      return (
        <div className="flex flex-col">
          <span>
            {createdAt ? formatDate(createdAt, 'dd/MM/yyyy HH:mm') : '-'}
          </span>
        </div>
      );
    },
  },
  {
    accessorKey: 'startDate',
    isSortable: true,
    id: 'startDate',
    header: 'Start Date',
    cell: ({ row }) => {
      const startDate = row.original.startDate;

      return (
        <div className="flex flex-col">
          <span>
            {startDate ? formatDate(startDate, 'dd/MM/yyyy HH:mm') : '-'}
          </span>
        </div>
      );
    },
  },
  {
    accessorKey: 'endDate',
    isSortable: true,
    id: 'endDate',
    header: 'End Date',
    cell: ({ row }) => {
      const endDate = row.original.endDate;

      return (
        <div className="flex flex-col">
          <span>{endDate ? formatDate(endDate, 'dd/MM/yyyy HH:mm') : '-'}</span>
        </div>
      );
    },
  },
];
