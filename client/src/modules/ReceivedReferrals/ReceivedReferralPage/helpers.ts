import { OrgActivity } from '@/services/organizations';

export const serviceCategoryToLabel = (serviceCategory: string) => {
  switch (serviceCategory) {
    case OrgActivity.Mpca:
      return 'MPCA';
    case OrgActivity.Wash:
      return 'WASH';
    case OrgActivity.Shelter:
      return 'Shelter';
    case OrgActivity.Livelihoods:
      return 'Livelihoods';
    case OrgActivity.FoodAssistance:
      return 'Food Assistance';
    case OrgActivity.Protection:
      return 'Protection';
    case OrgActivity.IncentivePayments:
      return 'Incentive Payments';
    default:
      return 'N/A';
  }
};
