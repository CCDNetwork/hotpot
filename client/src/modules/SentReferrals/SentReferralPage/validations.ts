import * as z from 'zod';

import { requiredSafeHtmlString, safeHtmlString } from '@/helpers/common';

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

const AdminLevelSchema = z
  .object({
    id: z.string(),
    parentId: z.string().optional(),
    level: z.number(),
    name: z.string(),
    code: z.string().optional(),
  })
  .nullable();

const FocalPointSchema = z
  .object(
    {
      id: z.string().min(1, { message: 'Focal Point Id is required' }),
      firstName: z
        .string()
        .min(1, { message: 'Focal Point first name is required' }),
      lastName: z
        .string()
        .min(1, { message: 'Focal Point last name is required' }),
    },
    {
      invalid_type_error: 'Focal point is required',
      required_error: 'Focal point is required',
    }
  )
  .nullable()
  .optional();

export const SentReferralSchema = z
  .object({
    isUrgent: z.boolean(),
    serviceCategory: z
      .string()
      .min(1, { message: 'Service category required' }),
    subactivities: z.array(
      z.object({
        id: z.string(),
        title: z.string(),
        serviceType: z.string(),
      })
    ),
    fundingSource: z.string({ message: 'Funding source required' }),
    organizationReferredTo: OrganizationSchema,
    displacementStatus: z.string().optional(),
    householdSize: z.string().optional(),
    householdMonthlyIncome: z.string().optional(),
    householdsVulnerabilityCriteria: z.array(z.string()),
    firstName: requiredSafeHtmlString('First name required'),
    patronymicName: requiredSafeHtmlString('Patronymic name required'),
    surname: requiredSafeHtmlString('Surname required'),
    dateOfBirth: z
      .date({
        required_error: 'Date of birth required',
        invalid_type_error: 'Date of birth required',
      })
      .optional(),
    gender: z.string().min(1, { message: 'Gender required' }),
    taxId: safeHtmlString.optional(),
    address: safeHtmlString,

    oblast: safeHtmlString,
    ryon: safeHtmlString,
    hromada: safeHtmlString,
    settlement: safeHtmlString,

    administrativeRegion1: AdminLevelSchema,
    administrativeRegion2: AdminLevelSchema,
    administrativeRegion3: AdminLevelSchema,
    administrativeRegion4: AdminLevelSchema,

    email: safeHtmlString.optional(),
    phone: safeHtmlString.optional(),
    contactPreference: z.string(),
    restrictions: safeHtmlString,
    consent: z.literal<boolean>(true, {
      errorMap: () => ({ message: 'Consent is required' }),
    }),
    required: requiredSafeHtmlString('Reason is required'),
    needForService: safeHtmlString,
    isSeparated: z.boolean(),
    isCaregiverInformed: z.string(),
    caregiverExplanation: safeHtmlString.optional(),
    caregiver: safeHtmlString.optional(),
    relationshipToChild: safeHtmlString.optional(),
    caregiverEmail: safeHtmlString.optional(),
    caregiverPhone: safeHtmlString,
    caregiverContactPreference: z.string(),
    caregiverNote: safeHtmlString,
    focalPoint: FocalPointSchema,
    status: z.string(),
    isDraft: z.boolean(),
    userCreated: FocalPointSchema,
    files: z.array(z.any()).optional(),
    focalPointId: z.string(),
    isMinor: z.boolean(),
    noTaxId: z.boolean(),
    subactivitiesIds: z.array(z.string()),
    isRejected: z.boolean().optional(),
  })
  .superRefine((data, ctx) => {
    if (data.isMinor) {
      if (!data.caregiver) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Caregiver name required',
          path: ['caregiver'],
        });
      }

      if (
        data.caregiverContactPreference &&
        data.caregiverContactPreference === 'email' &&
        !data.caregiverEmail
      ) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Caregiver email required',
          path: ['caregiverEmail'],
        });
      }

      if (
        data.caregiverContactPreference &&
        data.caregiverContactPreference === 'phone' &&
        !data.caregiverPhone
      ) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Caregiver phone required',
          path: ['caregiverPhone'],
        });
      }

      if (!data.relationshipToChild) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Relationship to child required',
          path: ['relationshipToChild'],
        });
      }
    }

    if (data.isCaregiverInformed === 'false' && data.isMinor) {
      if (!data.caregiverExplanation) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Caregiver explanation required',
          path: ['caregiverExplanation'],
        });
      }
    }

    if (data.serviceCategory === 'mpca') {
      if (!data.displacementStatus) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Displacement status required',
          path: ['displacementStatus'],
        });
      }

      if (!data.householdSize) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Household size required',
          path: ['householdSize'],
        });
      }

      if (!data.householdMonthlyIncome) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Household monthly income required',
          path: ['householdMonthlyIncome'],
        });
      }
    }

    if (!data.noTaxId) {
      if (!data.taxId) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Tax ID required',
          path: ['taxId'],
        });
      }
    }

    if (data.contactPreference && data.contactPreference === 'phone') {
      if (!data.phone) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Phone required',
          path: ['phone'],
        });
      }
    }

    if (data.contactPreference && data.contactPreference === 'email') {
      if (!data.email) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Email required',
          path: ['email'],
        });
      }
    }
  });

export type SentReferralFormData = z.infer<typeof SentReferralSchema>;
