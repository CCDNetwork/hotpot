import * as z from 'zod';

import { requiredSafeHtmlString } from '@/helpers/common';
import { UserRole } from '@/services/users';

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

export const AddUserModalFormSchema = z.object({
  firstName: requiredSafeHtmlString('First name is required'),
  lastName: requiredSafeHtmlString('Last name is required'),
  email: z.string().email(),
  organization: OrganizationSchema,
  role: z.nativeEnum(UserRole),
  permissions: z.array(z.string()).default([]),
});

export type AddUserModalForm = z.infer<typeof AddUserModalFormSchema>;
