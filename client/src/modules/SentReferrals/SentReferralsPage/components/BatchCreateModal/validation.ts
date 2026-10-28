import * as z from 'zod';

const OrganizationSchema = z.object(
  {
    id: z.string().min(1, { message: 'Organization Id is required' }),
    name: z.string().min(1, { message: 'Organization name is required' }),
    isMpcaActive: z.boolean(),
    isWashActive: z.boolean(),
    isShelterActive: z.boolean(),
    isLivelihoodsActive: z.boolean(),
    isFoodAssistanceActive: z.boolean(),
    isProtectionActive: z.boolean(),
    isIncentivePaymentsActive: z.boolean(),
    activities: z.array(
      z.object({
        id: z.string(),
        title: z.string(),
        serviceType: z.string(),
      })
    ),
  },
  {
    invalid_type_error: 'Organization is required',
    required_error: 'Organization is required',
  }
);

export const BatchCreateFormSchema = z.object({
  file: z.instanceof(File).refine((file) => file, {
    message: 'File is required',
  }),
  batchType: z.string().min(1, { message: 'Batch referral type required' }),
  organizationReferredTo: OrganizationSchema,
  serviceCategory: z.string().min(1, { message: 'Service category required' }),
  subactivities: z.array(
    z.object({
      id: z.string(),
      title: z.string(),
      serviceType: z.string(),
    })
  ),
});

export type BatchCreateModalForm = z.infer<typeof BatchCreateFormSchema>;
