import { Organization } from '@/services/organizations';
import { Beneficiary } from '../beneficiaryList';

export interface UserCreated {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  activatedAt: Date | null;
  createdAt: Date | null;
  role: '';
  language: string;
  organizations: Organization[] | [];
}

export interface DeduplicationListing {
  id: string;
  fileName: string;
  duplicates: number;
  userCreated: UserCreated | null;
  createdAt: Date | null;
  updatedAt: Date | null;
}

export interface DeduplicationDataset {
  file: {
    id: string;
    url: string;
    name: string;
  };
  templateId: string;
  duplicates: number;
}

export interface BookingDataset {
  isValid: boolean;
  fileUrl: string;
  fileId: string;
}

export interface BookingResponse {
  id: string;
  householdId: string;
  spouseId: string;
  currency: string;
  amount: number;
  frequency: number;
  modality: string;
  startDate: Date | null;
  endDate: Date | null;
  organizationId: string;
  createdAt: Date | null;
  updatedAt: Date | null;
}

export interface SameOrgDedupeResponse {
  totalRecords: number;
  identicalRecords: number;
  potentialDuplicateRecords: number;
}

export interface SystemOrgDedupeResponse {
  duplicates: number;
  duplicateBeneficiaries: Beneficiary[];
  ruleFields: string[];
}
