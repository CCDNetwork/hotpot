import { User } from '@/services/users';

export enum UserRole {
  User = 'user',
  Admin = 'admin',
  UserAdmin = 'user-admin',
  OrganizationAdmin = 'organization-admin',
}

export const USER_ROLE_LABELS: Record<UserRole, string> = {
  [UserRole.User]: 'User',
  [UserRole.Admin]: 'Admin',
  [UserRole.UserAdmin]: 'User Admin',
  [UserRole.OrganizationAdmin]: 'Organisation Admin',
};

export const USER_MANAGER_ROLES = [UserRole.Admin, UserRole.UserAdmin];

export const ORGANIZATION_MANAGER_ROLES = [
  UserRole.Admin,
  UserRole.OrganizationAdmin,
];

/** Roles that can open the admin Change log view. */
export const CHANGE_LOG_ROLES = [
  UserRole.Admin,
  UserRole.UserAdmin,
  UserRole.OrganizationAdmin,
];

export const getUserRoleLabel = (role: string) =>
  USER_ROLE_LABELS[role as UserRole] ?? role;

// Only admins can grant the admin role; an existing admin keeps it when others edit them.
export const getAssignableRoles = (
  currentUserRole: string,
  targetCurrentRole?: string
) =>
  Object.values(UserRole).filter(
    (role) =>
      role !== UserRole.Admin ||
      currentUserRole === UserRole.Admin ||
      targetCurrentRole === UserRole.Admin
  );

export enum UserPermission {
  Deduplication = 'deduplication',
  Referrals = 'referral',
  Booking = 'booking',
}

export const initialUser: User = {
  id: '',
  email: '',
  firstName: '',
  lastName: '',
  status: 'Active',
  activatedAt: null,
  createdAt: null,
  role: '',
  language: '',
  organizations: [],
  permissions: [],
  isSuperAdmin: false,
};
