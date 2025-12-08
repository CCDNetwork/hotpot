import * as z from 'zod';

export const BookingUploadFormSchema = z.object({
  template: z.object(
    {
      id: z.string().min(1),
      name: z.string(),
    },
    {
      invalid_type_error: 'Template is required',
      required_error: 'Template is required',
    }
  ),
});

export type BookingUploadForm = z.infer<typeof BookingUploadFormSchema>;
