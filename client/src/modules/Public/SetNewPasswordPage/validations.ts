import * as z from 'zod';

import { passwordPolicySchema } from '@/helpers/passwordPolicy';

export const SetNewPasswordFormSchema = z
  .object({
    email: z.string().email(),
    passwordResetCode: z.string().min(1),
    password: passwordPolicySchema,
    passwordConfirmation: z.string().min(1, {
      message: 'Confirm password is required',
    }),
  })
  .refine((data) => data.password === data.passwordConfirmation, {
    message: 'Passwords must match',
    path: ['passwordConfirmation'],
  });

export type SetNewPasswordForm = z.infer<typeof SetNewPasswordFormSchema>;
