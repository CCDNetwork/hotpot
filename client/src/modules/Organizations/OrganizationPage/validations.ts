import { requiredSafeHtmlString, safeHtmlString } from '@/helpers/common';
import * as z from 'zod';

export const OrganizationEditFormSchema = z.object({
  name: requiredSafeHtmlString('First name is required'),
  isMpcaActive: z.boolean(),
  isWashActive: z.boolean(),
  isShelterActive: z.boolean(),
  isFoodAssistanceActive: z.boolean(),
  isLivelihoodsActive: z.boolean(),
  isProtectionActive: z.boolean(),
  isIncentivePaymentsActive: z.boolean(),
  activities: z.array(
    z.object({
      id: z.string().optional(),
      title: safeHtmlString,
      serviceType: z.string(),
    })
  ),
});

export type OrganizationEditFormData = z.infer<
  typeof OrganizationEditFormSchema
>;
