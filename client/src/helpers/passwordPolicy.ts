import * as z from 'zod';

export const PASSWORD_POLICY_RULES = [
  'At least 8 characters',
  'At least one uppercase letter',
  'At least one lowercase letter',
  'At least one number',
  'At least one special character',
];

export const passwordPolicySchema = z
  .string()
  .min(8, { message: 'Password must be at least 8 characters' })
  .regex(/[A-Z]/, {
    message: 'Password must contain at least one uppercase letter',
  })
  .regex(/[a-z]/, {
    message: 'Password must contain at least one lowercase letter',
  })
  .regex(/[0-9]/, { message: 'Password must contain at least one number' })
  .regex(/[^A-Za-z0-9]/, {
    message: 'Password must contain at least one special character',
  });
