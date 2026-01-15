import * as z from 'zod';

import { requiredSafeHtmlString, safeHtmlString } from '@/helpers/common';

// Schema for normal settings update (when initialized)
export const SettingsFormSchema = z.object({
  deploymentCountry: safeHtmlString,
  deploymentName: safeHtmlString,
  adminLevel1Name: safeHtmlString,
  adminLevel2Name: safeHtmlString,
  adminLevel3Name: safeHtmlString,
  adminLevel4Name: safeHtmlString,
  metabaseUrl: z.string(),
  fundingSources: z.array(z.object({ value: safeHtmlString })),
});

export type SettingsFormData = z.infer<typeof SettingsFormSchema>;

// Schema for initial setup (includes org + user fields)
export const InitialSetupFormSchema = z
  .object({
    // Deployment Settings (required for initial setup)
    deploymentCountry: requiredSafeHtmlString('Deployment country is required'),
    deploymentName: requiredSafeHtmlString('Deployment name is required'),
    adminLevel1Name: requiredSafeHtmlString('Admin Level 1 name is required'),
    adminLevel2Name: requiredSafeHtmlString('Admin Level 2 name is required'),
    adminLevel3Name: requiredSafeHtmlString('Admin Level 3 name is required'),
    adminLevel4Name: requiredSafeHtmlString('Admin Level 4 name is required'),
    metabaseUrl: z.string(),
    fundingSources: z.array(z.object({ value: safeHtmlString })),

    // Organization fields
    organizationName: requiredSafeHtmlString('Organization name is required'),
    isMpcaActive: z.boolean(),
    isWashActive: z.boolean(),
    isShelterActive: z.boolean(),
    isFoodAssistanceActive: z.boolean(),
    isLivelihoodsActive: z.boolean(),
    isProtectionActive: z.boolean(),

    // User fields
    firstName: requiredSafeHtmlString('First name is required'),
    lastName: requiredSafeHtmlString('Last name is required'),
    email: z.string().email('Invalid email address'),
    password: z.string().min(8, 'Password must be at least 8 characters'),
    confirmPassword: z.string(),
    permissions: z.array(z.string()),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Passwords must match',
    path: ['confirmPassword'],
  });

export type InitialSetupFormData = z.infer<typeof InitialSetupFormSchema>;

export const defaultInitialSetupValues: InitialSetupFormData = {
  // Deployment Settings
  deploymentCountry: '',
  deploymentName: '',
  adminLevel1Name: '',
  adminLevel2Name: '',
  adminLevel3Name: '',
  adminLevel4Name: '',
  metabaseUrl: 'https://default.metabase.url',
  fundingSources: [{ value: 'BHA' }, { value: 'Others' }],

  // Organization
  organizationName: '',
  isMpcaActive: false,
  isWashActive: false,
  isShelterActive: false,
  isFoodAssistanceActive: false,
  isLivelihoodsActive: false,
  isProtectionActive: false,

  // User
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  confirmPassword: '',
  permissions: ['deduplication', 'referral', 'booking'],
};
