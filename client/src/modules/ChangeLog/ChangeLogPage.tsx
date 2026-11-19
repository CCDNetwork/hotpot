import { useQuery } from '@tanstack/react-query';
import { formatDate } from 'date-fns';

import { DataTable } from '@/components/DataTable';
import { TableColumn } from '@/components/DataTable/types';
import { PageContainer } from '@/components/PageContainer';
import { Badge } from '@/components/ui/badge';
import { APP_ROUTE } from '@/helpers/constants';
import { usePagination } from '@/helpers/pagination';
import { api } from '@/services/api';

interface ChangeLogEntry {
  id: string;
  entityType: string;
  organizationName: string | null;
  subjectEmail: string | null;
  action: string;
  oldValue: string | null;
  newValue: string | null;
  performedByEmail: string | null;
  createdAt: string;
}

const AREA_LABEL: Record<string, string> = {
  organization: 'Organisation',
  organization_email_domain: 'Allowed domain',
  user: 'User',
  user_role: 'Role',
};

const describe = (e: ChangeLogEntry) => {
  switch (e.entityType) {
    case 'organization':
      if (e.action === 'created') return `Organisation created (${e.newValue})`;
      if (e.action === 'deleted') return `Organisation "${e.oldValue}" deleted`;
      return `Organisation edited: ${e.newValue}`;
    case 'organization_email_domain':
      if (e.action === 'added') return `Allowed domain @${e.newValue} added`;
      if (e.action === 'removed')
        return `Allowed domain @${e.oldValue} removed${e.newValue ? ` ${e.newValue}` : ''}`;
      return `Allowed domain @${e.oldValue} changed to @${e.newValue}`;
    case 'user':
      if (e.action === 'created')
        return `User ${e.subjectEmail} created (${e.newValue})`;
      if (e.action === 'deleted') return `User ${e.subjectEmail} deleted`;
      return `User ${e.subjectEmail} edited: ${e.newValue}`;
    case 'user_role':
      return `${e.subjectEmail}: role changed from ${e.oldValue} to ${e.newValue}`;
    default:
      return [e.action, e.oldValue, e.newValue].filter(Boolean).join(' · ');
  }
};

const columns: TableColumn<ChangeLogEntry>[] = [
  {
    accessorKey: 'createdAt',
    id: 'createdAt',
    header: 'When',
    cell: ({ row }) =>
      formatDate(new Date(row.original.createdAt), 'dd/MM/yyyy HH:mm'),
  },
  {
    accessorKey: 'entityType',
    id: 'entityType',
    header: 'Area',
    cell: ({ row }) => (
      <Badge variant="secondary">
        {AREA_LABEL[row.original.entityType] ?? row.original.entityType}
      </Badge>
    ),
  },
  {
    accessorKey: 'action',
    id: 'action',
    header: 'Change',
    cell: ({ row }) => describe(row.original),
  },
  {
    accessorKey: 'organizationName',
    id: 'organizationName',
    header: 'Organisation',
    cell: ({ row }) => row.original.organizationName ?? '-',
  },
  {
    accessorKey: 'performedByEmail',
    id: 'performedByEmail',
    header: 'By',
    cell: ({ row }) => row.original.performedByEmail ?? '-',
  },
];

/** Platform-wide change log (allowed domains and admin roles). */
export const ChangeLogPage = () => {
  const { currentPage, pageSize, onPageChange, onPageSizeChange } =
    usePagination();

  const { data, isLoading } = useQuery(
    ['change-log', currentPage, pageSize],
    async () =>
      (await api.get(`/change-log?page=${currentPage}&pageSize=${pageSize}`))
        .data
  );

  return (
    <PageContainer
      pageTitle="Change log"
      pageSubtitle="Administrative changes across the platform (organisations, allowed email domains, users and roles): who changed what, and when."
      breadcrumbs={[{ href: APP_ROUTE.ChangeLog, name: 'Change log' }]}
    >
      <DataTable
        data={data?.data ?? []}
        pagination={data?.meta}
        isQueryLoading={isLoading}
        currentPage={currentPage}
        pageClicked={onPageChange}
        pageSizeClicked={onPageSizeChange}
        columns={columns}
      />
    </PageContainer>
  );
};
