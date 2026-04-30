import { User } from '@/services/users';

export enum UserRole {
  User = 'user',
  Admin = 'admin',
}

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
