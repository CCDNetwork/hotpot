import { formatDate } from 'date-fns';
import { EditIcon, MailIcon, Trash2Icon } from 'lucide-react';
import { TableColumn } from '@/components/DataTable/types';
import { User, getUserRoleLabel } from '@/services/users';
import { Button } from '@/components/ui/button';
import { Tooltip } from '@/components/Tooltip';
import { Badge } from '@/components/ui/badge';
import { isB2CEnabled } from '@/config/msalConfig';

export const columns = (
  setUserToDelete: React.Dispatch<React.SetStateAction<User | null>>,
  onUserTableRowClick: (userRow: User) => void,
  onResendInvitation: (userRow: User) => void
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
    cell: ({ row }) => {
      const { email, invitationPending } = row.original;
      return (
        <div className="flex flex-wrap items-center gap-2">
          <span>{email}</span>
          {invitationPending && (
            <Badge variant="secondary">Invitation pending</Badge>
          )}
        </div>
      );
    },
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

      // Password sign-in: only users who have not set a password yet.
      // B2C: the email only carries the sign-in link, so it can always be resent.
      const canResend =
        !user.isSuperAdmin && (user.invitationPending || isB2CEnabled());

      return (
        <div className="text-right">
          {canResend && (
            <Tooltip tooltipContent={'Resend invitation'}>
              <Button
                variant="ghost"
                size="icon"
                aria-label="Resend invitation"
                onClick={(e: React.SyntheticEvent) => {
                  e.stopPropagation();
                  onResendInvitation(user);
                }}
              >
                <MailIcon className="size-5" />
              </Button>
            </Tooltip>
          )}
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
