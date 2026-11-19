import { Organization } from '@/services/organizations';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  status: string;
  activatedAt: Date | null;
  createdAt: Date | null;
  role: string;
  language: string;
  organizations: Organization[];
  permissions: string[];
  isSuperAdmin: false;
  /** Password sign-in: invited but has not set a password yet. */
  invitationPending: boolean;
  /** Only in the create-user response: false if the invitation email failed. */
  invitationEmailSent?: boolean | null;
}

export interface UserProfileRequestPayload {
  email: string;
  firstName: string;
  lastName: string;
  password?: string;
}
