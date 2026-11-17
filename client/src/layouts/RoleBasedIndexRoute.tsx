import { useAuth } from '@/providers/GlobalProvider';

import { UserRole } from '@/services/users';

import { Navigate } from 'react-router-dom';
import { APP_ROUTE } from '@/helpers/constants';

export const RoleBasedIndexRoute = () => {
  const {
    user: { role, isSuperAdmin },
  } = useAuth();

  if (isSuperAdmin) {
    return <Navigate to={APP_ROUTE.Settings} replace />;
  }

  switch (role) {
    case UserRole.Admin:
    case UserRole.UserAdmin:
    case UserRole.OrganizationAdmin:
      return <Navigate to={APP_ROUTE.Dashboard} replace />;

    case UserRole.User:
      return <Navigate to={APP_ROUTE.UserHandbookList} replace />;

    default:
      return <Navigate to={APP_ROUTE.UserHandbookList} replace />;
  }
};
