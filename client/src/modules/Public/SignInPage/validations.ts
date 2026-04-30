import * as z from 'zod';

export const SignInFormSchema = z.object({
  username: z
    .string()
    .refine(
      (val) =>
        val === 'superadmin' || z.string().email().safeParse(val).success,
      {
        message: 'Invalid email address',
      }
    ),
  password: z.string().min(8, {
    message: 'Password should contain at least 8 characters',
  }),
});

export type SignInFormData = z.infer<typeof SignInFormSchema>;

export const B2cSignInFormSchema = z.object({
  email: z.string().email({
    message: 'Please enter a valid email address',
  }),
});

export type B2cSignInFormData = z.infer<typeof B2cSignInFormSchema>;
