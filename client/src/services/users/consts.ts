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

/**
 * Admins and User Admins see every module (Booking, Deduplication, Referrals);
 * everyone else, incl. Organisation Admins, only the modules they are ticked
 * for. The server enforces the same rule.
 */
export const bypassesModulePermissions = (role: string) =>
  role === UserRole.Admin || role === UserRole.UserAdmin;

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

/**
 * Roles that can be assigned to a user of the given organisation (same rule as
 * the server, UserRole.AssignableRoles):
 * - User: always;
 * - User Admin: if the organisation allows it (USER_ADMIN_ORGANIZATION), and
 *   also in the Organisation Admin organisation (UNICEF can take over user management);
 * - Organisation Admin: only if it allows it (ORGANIZATION_ADMIN_ORGANIZATION);
 * - Admin: only granted by an Admin; an existing Admin keeps it when others edit them.
 * Without an organisation (not chosen yet) only User and Admin are offered.
 */
export const getAssignableRoles = (
  currentUserRole: string,
  organization?: {
    allowsUserAdmin?: boolean;
    allowsOrganizationAdmin?: boolean;
  } | null,
  targetCurrentRole?: string
): UserRole[] => {
  const roles: UserRole[] = [];
  if (
    currentUserRole === UserRole.Admin ||
    targetCurrentRole === UserRole.Admin
  )
    roles.push(UserRole.Admin);
  if (organization?.allowsUserAdmin || organization?.allowsOrganizationAdmin)
    roles.push(UserRole.UserAdmin);
  if (organization?.allowsOrganizationAdmin)
    roles.push(UserRole.OrganizationAdmin);
  roles.push(UserRole.User);
  return roles;
};

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
  invitationPending: false,
};
