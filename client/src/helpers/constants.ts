import { UserPermission } from '@/services/users';
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
} from 'lucide-react';
import { HomepageData } from '@/providers/DirectusProvider/types.ts';

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
  // PUBLIC
  SignIn = '/sign-in',
  PermissionDenied = '/permission-denied',
  ReferralData = '/referral-data',
  BeneficiaryDataView = '/beneficiary-data',
  RequestNewPassword = '/request-new-password',
  SetNewPassword = '/reset-password',
}

export const getNavigationItems = (cmsData?: HomepageData) => [
  {
    categoryName: cmsData?.deduplication ?? '',
    userPermissions: [UserPermission.Deduplication],
    routes: [
      {
        name: cmsData?.deduplication ?? '',
        to: APP_ROUTE.Deduplication,
        userPermissions: [UserPermission.Deduplication],
        icon: BookCopyIcon,
      },
      {
        name: cmsData?.manage_duplicates ?? '',
        to: APP_ROUTE.BeneficiaryList,
        icon: BookUserIcon,
        userPermissions: [UserPermission.Deduplication],
      },
      {
        name: cmsData?.manage_templates ?? '',
        to: APP_ROUTE.Templates,
        icon: FilesIcon,
        userPermissions: [UserPermission.Deduplication],
      },
    ],
  },
  {
    categoryName: 'Booking',
    userPermissions: [UserPermission.Booking],
    routes: [
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
    categoryName: cmsData?.referrals ?? '',
    userPermissions: [UserPermission.Referrals],
    routes: [
      {
        name: cmsData?.manage_received ?? '',
        to: APP_ROUTE.ReceivedReferrals,
        icon: LogInIcon,
        userPermissions: [UserPermission.Referrals],
      },
      {
        name: cmsData?.manage_sent ?? '',
        to: APP_ROUTE.SentReferrals,
        icon: LogOutIcon,
        userPermissions: [UserPermission.Referrals],
      },
    ],
  },
  {
    categoryName: cmsData?.admin ?? '',
    routes: [
      {
        name: cmsData?.organisations ?? '',
        to: APP_ROUTE.Organizations,
        icon: Building2Icon,
      },
      { name: cmsData?.users ?? '', to: APP_ROUTE.Users, icon: UsersIcon },
      {
        name: cmsData?.rules ?? '',
        to: APP_ROUTE.Rules,

        icon: TextSelectIcon,
      },
      {
        name: cmsData?.handbook_entries ?? '',
        to: APP_ROUTE.Handbook,
        icon: BookOpenTextIcon,
      },
    ],
  },
];
