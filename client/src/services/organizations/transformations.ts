import { Organization, OrganizationActivity } from '@/services/organizations';

export const resToActivities = (res: any): OrganizationActivity => {
  return {
    id: res.id ?? null,
    title: res.title ?? '',
    serviceType: res.serviceType ?? '',
  };
};

export const resToOrganization = (res: any): Organization => {
  return {
    id: res.id ?? '',
    name: res.name ?? '',
    isMpcaActive: res.isMpcaActive ?? false,
    isShelterActive: res.isShelterActive ?? false,
    isWashActive: res.isWashActive ?? false,
    isFoodAssistanceActive: res.isFoodAssistanceActive ?? false,
    isLivelihoodsActive: res.isLivelihoodsActive ?? false,
    isProtectionActive: res.isProtectionActive ?? false,
    isIncentivePaymentsActive: res.isIncentivePaymentsActive ?? false,
    activities: res.activities ? res.activities.map(resToActivities) : [],
    createdAt: res.createdAt ? new Date(res.createdAt) : null,
    updatedAt: res.updatedAt ? new Date(res.updatedAt) : null,
  };
};

export const organizationToReq = (data: any): Omit<Organization, 'id'> => {
  const req: any = {
    name: data.name ?? '',
    isMpcaActive: data.isMpcaActive ?? false,
    isWashActive: data.isWashActive ?? false,
    isShelterActive: data.isShelterActive ?? false,
    isFoodAssistanceActive: data.isFoodAssistanceActive ?? false,
    isLivelihoodsActive: data.isLivelihoodsActive ?? false,
    isProtectionActive: data.isProtectionActive ?? false,
    isIncentivePaymentsActive: data.isIncentivePaymentsActive ?? false,
    activities: data.activities ? data.activities.map(resToActivities) : [],
  };

  return req;
};

export const organizationMeToReq = (data: any): Omit<Organization, 'id'> => {
  const req: any = {
    name: data.name ?? '',
    isMpcaActive: data.isMpcaActive ?? false,
    isWashActive: data.isWashActive ?? false,
    isShelterActive: data.isShelterActive ?? false,
    isFoodAssistanceActive: data.isFoodAssistanceActive ?? false,
    isLivelihoodsActive: data.isLivelihoodsActive ?? false,
    isProtectionActive: data.isProtectionActive ?? false,
    isIncentivePaymentsActive: data.isIncentivePaymentsActive ?? false,
    activities: data.activities ? data.activities.map(resToActivities) : [],
  };

  return req;
};
