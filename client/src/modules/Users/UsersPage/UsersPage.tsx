import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { DataTable } from '@/components/DataTable';
import { PageContainer } from '@/components/PageContainer';
import { usePagination } from '@/helpers/pagination';
import { useUserMutation, useUsers } from '@/services/users/api';
import { AddUserModal } from '@/modules/Users/UsersPage/components';
import { User, UserRole } from '@/services/users';
import { APP_ROUTE } from '@/helpers/constants';
import { ConfirmationDialog } from '@/components/ConfirmationDialog';
import { toast } from '@/components/ui/use-toast';

import { columns } from './columns';

export const UsersPage = () => {
  const navigate = useNavigate();
  const pagination = usePagination();
  const {
    currentPage,
    onPageChange,
    onPageSizeChange,
    onSortChange,
    onSearchChange,
  } = pagination;

  const [userToDelete, setUserToDelete] = useState<User | null>(null);

  const { data: users, isLoading: queryLoading } = useUsers({
    ...pagination,
    filters: { 'role[in]': Object.values(UserRole).join('|') },
  });

  const { deleteUser, resendInvitation } = useUserMutation();

  const handleResendInvitation = async (userRow: User) => {
    try {
      await resendInvitation.mutateAsync(userRow.id);
      toast({
        title: 'Invitation sent',
        variant: 'default',
        description: `A new invitation email was sent to ${userRow.email}.`,
      });
    } catch (error: any) {
      toast({
        title: 'Invitation not sent',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage ||
          'The invitation email could not be sent.',
      });
    }
  };

  const handleDeleteUser = async () => {
    if (!userToDelete) return;

    try {
      await deleteUser.mutateAsync(userToDelete.id);
      toast({
        title: 'Success!',
        variant: 'default',
        description: 'User successfully deleted!',
      });
    } catch (error: any) {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage || 'Failed to delete user.',
      });
    }
    setUserToDelete(null);
  };

  const onUserTableRowClick = (userRow: User) =>
    navigate(`${APP_ROUTE.Users}/${userRow.id}`);

  return (
    <PageContainer
      pageTitle="Manage Users"
      pageSubtitle="On this page you can create and edit profiles for individual users from the organisations using the platform."
      headerNode={<AddUserModal />}
      breadcrumbs={[{ href: `${APP_ROUTE.Users}`, name: 'Users' }]}
    >
      <DataTable
        data={users?.data ?? []}
        pagination={users?.meta}
        isQueryLoading={queryLoading}
        currentPage={currentPage}
        pageClicked={onPageChange}
        pageSizeClicked={onPageSizeChange}
        headerClicked={onSortChange}
        onSearchChange={onSearchChange}
        columns={columns(
          setUserToDelete,
          onUserTableRowClick,
          handleResendInvitation
        )}
      />
      <ConfirmationDialog
        open={!!userToDelete}
        title="Delete User"
        body={`Are you sure you want to delete user "${userToDelete?.firstName} ${userToDelete?.lastName}"?`}
        onAction={handleDeleteUser}
        confirmButtonLoading={deleteUser.isLoading}
        actionButtonVariant="destructive"
        onCancel={() => setUserToDelete(null)}
      />
    </PageContainer>
  );
};
