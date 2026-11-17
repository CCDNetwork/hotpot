import { formatDate } from 'date-fns';
import { EditIcon, Trash2Icon } from 'lucide-react';
import { TableColumn } from '@/components/DataTable/types';
import { User, getUserRoleLabel } from '@/services/users';
import { Button } from '@/components/ui/button';
import { Tooltip } from '@/components/Tooltip';

export const columns = (
  setUserToDelete: React.Dispatch<React.SetStateAction<User | null>>,
  onUserTableRowClick: (userRow: User) => void
): TableColumn<User>[] => [
  {
    accessorKey: 'firstName',
    id: 'firstName',
    header: 'First Name',
  },
  {
    accessorKey: 'lastName',
    id: 'lastName',
    header: 'Last Name',
  },
  {
    accessorKey: 'email',
    id: 'email',
    header: 'Email',
  },
  {
    accessorKey: 'organizations',
    id: 'organizations',
    header: 'Organisation',
    cell: ({ row }) => {
      const { organizations } = row.original;

      if (!organizations.length) {
        return <div>-</div>;
      }

      return <div>{organizations[0].name ?? '-'}</div>;
    },
  },
  {
    accessorKey: 'role',
    id: 'role',
    header: 'Role',
    cell: ({ row }) => {
      const { role } = row.original;

      return <div>{getUserRoleLabel(role)}</div>;
    },
  },
  {
    accessorKey: 'createdAt',
    id: 'createdAt',
    header: 'Created On',
    isSortable: true,
    cell: ({ getValue }) => {
      return (
        <div className="flex flex-col">
          <span>{formatDate(getValue() as Date, 'dd/MM/yyyy HH:mm')}</span>
        </div>
      );
    },
  },
  {
    id: 'actions',
    header: 'Actions',
    headerClassName: 'text-right pr-5',
    cell: ({ row }) => {
      const user = row.original;

      return (
        <div className="text-right">
          <Tooltip tooltipContent={'Edit'}>
            <Button
              variant="ghost"
              size="icon"
              onClick={(e: React.SyntheticEvent) => {
                e.stopPropagation();
                onUserTableRowClick(user);
              }}
            >
              <EditIcon className="size-5" />
            </Button>
          </Tooltip>
          <Tooltip tooltipContent={'Delete'}>
            <Button
              variant="ghost"
              onClick={(e: React.SyntheticEvent) => {
                e.stopPropagation();
                setUserToDelete(user);
              }}
              size="icon"
            >
              <Trash2Icon className="size-5 text-destructive" />
            </Button>
          </Tooltip>
        </div>
      );
    },
  },
];
