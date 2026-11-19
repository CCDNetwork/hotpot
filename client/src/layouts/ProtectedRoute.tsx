import { ReactNode } from 'react';
import { Navigate, Outlet } from 'react-router-dom';

import {
  bypassesModulePermissions,
  UserPermission,
  UserRole,
} from '@/services/users';
import { useAuth } from '@/providers/GlobalProvider';

export const ProtectedRoute = ({
  children = <Outlet />,
  userPermissions,
  roles,
}: Props) => {
  const { user } = useAuth();
  console.log('ProtectedRoute userPermissions:', userPermissions);
  console.log('ProtectedRoute user.permissions:', user.permissions);

  // Module routes: everyone except Admins and User Admins needs one of the
  // permissions (matches the server). Plain users also can't open admin routes.
  if (
    (user.role === UserRole.User ||
      (userPermissions && !bypassesModulePermissions(user.role))) &&
    !user.permissions.some((p) =>
      userPermissions?.includes(p as UserPermission)
    )
  ) {
    return <Navigate to="/permission-denied" replace />;
  }

  if (roles && !roles.includes(user.role as UserRole)) {
    return <Navigate to="/permission-denied" replace />;
  }

  return children;
};

type Props = {
  children?: ReactNode;
  userPermissions?: UserPermission[];
  roles?: UserRole[];
};
