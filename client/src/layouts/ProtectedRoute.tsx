import { ReactNode } from 'react';
import { Navigate, Outlet } from 'react-router-dom';

import { UserPermission, UserRole } from '@/services/users';
import { useAuth } from '@/providers/GlobalProvider';

export const ProtectedRoute = ({
  children = <Outlet />,
  userPermissions,
}: Props) => {
  const { user } = useAuth();
  console.log('ProtectedRoute userPermissions:', userPermissions);
  console.log('ProtectedRoute user.permissions:', user.permissions);

  if (
    user.role === UserRole.User &&
    !user.permissions.some((p) =>
      userPermissions?.includes(p as UserPermission)
    )
  ) {
    return <Navigate to="/permission-denied" replace />;
  }

  return children;
};

type Props = {
  children?: ReactNode;
  userPermissions?: UserPermission[];
};
