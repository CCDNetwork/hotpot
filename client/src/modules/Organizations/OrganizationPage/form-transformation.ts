import { Organization } from '@/services/organizations';

import { OrganizationEditFormData } from './validations';

export const dataToOrganizationEditFormData = (
  data: Organization
): OrganizationEditFormData => {
  return {
    name: data.name,
    isMpcaActive: data.isMpcaActive,
    isWashActive: data.isWashActive,
    isShelterActive: data.isShelterActive,
    isFoodAssistanceActive: data.isFoodAssistanceActive,
    isLivelihoodsActive: data.isLivelihoodsActive,
    isProtectionActive: data.isProtectionActive,
    isIncentivePaymentsActive: data.isIncentivePaymentsActive,
    activities: data.activities,
  };
};
