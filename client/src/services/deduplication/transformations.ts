import { resToOrganization } from '@/services/organizations';

import {
  BookingResponse,
  DeduplicationDataset,
  DeduplicationListing,
  SameOrgDedupeResponse,
  SystemOrgDedupeResponse,
  UserCreated,
} from './types';
import { resToBeneficiary } from '../beneficiaryList/transformations';

const resToUserCreated = (res: any): UserCreated => {
  return {
    id: res.id ?? '',
    activatedAt: res.activatedAt ? new Date(res.activatedAt) : null,
    createdAt: res.createdAt ? new Date(res.activatedAt) : null,
    email: res.email ?? '',
    firstName: res.firstName ?? '',
    language: res.language ?? '',
    lastName: res.lastName ?? '',
    organizations: res.organizations
      ? res.organizations(resToOrganization)
      : [],
    role: res.role ?? '',
  };
};

export const resToDeduplicationListing = (res: any): DeduplicationListing => {
  return {
    id: res.id ?? '',
    fileName: res.fileName ?? '',
    duplicates: res.duplicates ?? 0,
    userCreated: res.userCreated ? resToUserCreated(res.userCreated) : null,
    createdAt: res.createdAt ?? null,
    updatedAt: res.updatedAt ?? null,
  };
};

export const resToBooking = (res: any): BookingResponse => {
  return {
    id: res.id ?? '',
    householdId: res.householdId ?? '',
    spouseId: res.spouseId ?? '',
    currency: res.currency ?? '',
    amount: res.amount ?? 0,
    rounds: res.rounds ?? 0,
    modality: res.modality ?? '',
    startDate: res.startDate ? new Date(res.startDate) : null,
    endDate: res.endDate ? new Date(res.endDate) : null,
    organizationId: res.organizationId ?? '',
    createdAt: res.createdAt ? new Date(res.createdAt) : null,
    updatedAt: res.updatedAt ? new Date(res.updatedAt) : null,
  };
};

export const dataToDatasetRequest = (data: {
  file: File;
  templateId: string;
}): FormData => {
  const formData = new FormData();
  formData.append('file', data.file);
  formData.append('templateId', data.templateId);
  return formData;
};

export const resToDatasetResponse = (res: any): DeduplicationDataset => {
  return {
    file: res.file ?? null,
    templateId: res.templateId ?? '',
    duplicates: res.duplicates ?? 0,
  };
};

export const resToSameOrgDedupResponse = (res: any): SameOrgDedupeResponse => {
  return {
    identicalRecords: res.identicalRecords ?? 0,
    potentialDuplicateRecords: res.potentialDuplicateRecords ?? 0,
    totalRecords: res.totalRecords ?? 0,
  };
};

export const resToSystemDedupeResponse = (
  res: any
): SystemOrgDedupeResponse => {
  return {
    duplicates: res.duplicates ?? 0,
    duplicateBeneficiaries: res.duplicateBeneficiaries
      ? res.duplicateBeneficiaries.map(resToBeneficiary)
      : [],
    ruleFields: res.ruleFields,
  };
};
