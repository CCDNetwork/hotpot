import * as z from 'zod';

import { requiredSafeHtmlString, safeHtmlString } from '@/helpers/common';

export const AddOrganizationModalFormSchema = z.object({
  name: requiredSafeHtmlString('Organization name is required'),
  isMpcaActive: z.boolean(),
  isWashActive: z.boolean(),
  isShelterActive: z.boolean(),
  isFoodAssistanceActive: z.boolean(),
  isLivelihoodsActive: z.boolean(),
  isProtectionActive: z.boolean(),
  isIncentivePaymentsActive: z.boolean(),
  activities: z
    .array(
      z.object({
        id: z.string().optional(),
        title: safeHtmlString,
        serviceType: z.string(),
      })
    )
    .default([]),
});

export type AddOrganizationModalForm = z.infer<
  typeof AddOrganizationModalFormSchema
>;
