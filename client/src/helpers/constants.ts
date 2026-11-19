import {
  CHANGE_LOG_ROLES,
  ORGANIZATION_MANAGER_ROLES,
  USER_MANAGER_ROLES,
  UserPermission,
} from '@/services/users';
import {
  BookCopyIcon,
  BookOpenTextIcon,
  BookUserIcon,
  Building2Icon,
  FilesIcon,
  LogInIcon,
  LogOutIcon,
  TextSelectIcon,
  UsersIcon,
  HistoryIcon,
} from 'lucide-react';
import { HomepageData } from '@/providers/DirectusProvider/types.ts';
import { NavigationItem } from '@/helpers/types';

export enum PAGE_TYPE {
  Create = 'new',
}

export enum APP_ROUTE {
  // PRIVATE
  Users = '/users',
  MyProfile = '/my-profile',
  BeneficiaryList = '/beneficiary-list',
  Deduplication = '/deduplication',
  Booking = '/booking',
  Prebooking = '/prebooking',
  ViewBooking = '/view-booking',
  SentReferrals = '/sent-referrals',
  ReceivedReferrals = '/received-referrals',
  ServiceList = '/service-list',
  Organizations = '/organizations',
  Handbook = '/handbook',
  UserHandbookList = '/handbook-list',
  Rules = '/rules',
  Templates = '/templates',
  Dashboard = '/dashboard',
  Settings = '/settings',
  ChangeLog = '/change-log',
  // PUBLIC
  AuthCallback = '/auth/callback',
  SignIn = '/sign-in',
  PermissionDenied = '/permission-denied',
  ReferralData = '/referral-data',
  RequestNewPassword = '/request-new-password',
  SetNewPassword = '/reset-password',
}

// Menu labels: built-in English defaults. If a deployment configures Directus
// (VITE_DIRECTUS_URL), its translated labels override these; if Directus is not
// configured or unavailable, the English defaults are shown.
export const getNavigationItems = (
  cmsData?: HomepageData
): NavigationItem[] => [
  {
    categoryName: 'Booking',
    userPermissions: [UserPermission.Booking],
    routes: [
      {
        name: 'Pre-booking Check',
        to: APP_ROUTE.Prebooking,
        userPermissions: [UserPermission.Booking],
        icon: BookCopyIcon,
      },
      {
        name: 'Make Bookings',
        to: APP_ROUTE.Booking,
        userPermissions: [UserPermission.Booking],
        icon: BookCopyIcon,
      },
      {
        name: 'View Bookings',
        to: APP_ROUTE.ViewBooking,
        userPermissions: [UserPermission.Booking],
        icon: BookOpenTextIcon,
      },
    ],
  },
  {
    categoryName: cmsData?.deduplication || 'Deduplication',
    userPermissions: [UserPermission.Deduplication],
    routes: [
      {
        name: cmsData?.deduplication || 'Deduplicate Cases',
        to: APP_ROUTE.Deduplication,
        userPermissions: [UserPermission.Deduplication],
        icon: BookCopyIcon,
      },
      {
        name: cmsData?.manage_duplicates || 'Manage Duplicates',
        to: APP_ROUTE.BeneficiaryList,
        icon: BookUserIcon,
        userPermissions: [UserPermission.Deduplication],
      },
      {
        name: cmsData?.manage_templates || 'Manage Templates',
        to: APP_ROUTE.Templates,
        icon: FilesIcon,
        userPermissions: [UserPermission.Deduplication],
      },
    ],
  },
  {
    categoryName: cmsData?.referrals || 'Referrals',
    userPermissions: [UserPermission.Referrals],
    routes: [
      {
        name: cmsData?.manage_received || 'Manage Received',
        to: APP_ROUTE.ReceivedReferrals,
        icon: LogInIcon,
        userPermissions: [UserPermission.Referrals],
      },
      {
        name: cmsData?.manage_sent || 'Manage Sent',
        to: APP_ROUTE.SentReferrals,
        icon: LogOutIcon,
        userPermissions: [UserPermission.Referrals],
      },
    ],
  },
  {
    categoryName: cmsData?.admin || 'Admin',
    routes: [
      {
        name: cmsData?.organisations || 'Organisations',
        to: APP_ROUTE.Organizations,
        icon: Building2Icon,
        roles: ORGANIZATION_MANAGER_ROLES,
      },
      {
        name: cmsData?.users || 'Users',
        to: APP_ROUTE.Users,
        icon: UsersIcon,
        roles: USER_MANAGER_ROLES,
      },
      {
        name: cmsData?.rules || 'Rules',
        to: APP_ROUTE.Rules,

        icon: TextSelectIcon,
      },
      {
        name: cmsData?.handbook_entries || 'Handbook entries',
        to: APP_ROUTE.Handbook,
        icon: BookOpenTextIcon,
      },
      {
        name: 'Change log',
        to: APP_ROUTE.ChangeLog,
        icon: HistoryIcon,
        roles: CHANGE_LOG_ROLES,
      },
    ],
  },
];
