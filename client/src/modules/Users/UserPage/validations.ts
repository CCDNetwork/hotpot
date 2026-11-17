import * as z from 'zod';

import { requiredSafeHtmlString, safeHtmlString } from '@/helpers/common';
import { passwordPolicySchema } from '@/helpers/passwordPolicy';
import { UserRole } from '@/services/users/consts';

const OrganizationSchema = z.object(
  {
    id: z.string().min(1, { message: 'Organization Id is required' }),
    name: z.string().min(1, { message: 'Organization name is required' }),
  },
  {
    invalid_type_error: 'Organization is required',
    required_error: 'Organization is required',
  }
);

export const UserEditFormSchema = z
  .object({
    firstName: requiredSafeHtmlString('First name is required'),
    lastName: requiredSafeHtmlString('Last name is required'),
    email: z.string().email(),
    password: z.union([z.literal(''), passwordPolicySchema]).optional(),
    confirmPassword: safeHtmlString.optional(),
    organization: OrganizationSchema,
    role: z.nativeEnum(UserRole),
    permissions: z.array(z.string()).default([]),
  })
  .refine(
    (data) => {
      if (data.password && data.password !== data.confirmPassword) {
        return false;
      }
      return true;
    },
    { message: 'Passwords must match', path: ['confirmPassword'] }
  );

export type UserEditFormData = z.infer<typeof UserEditFormSchema>;
