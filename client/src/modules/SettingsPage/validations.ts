import * as z from 'zod';

import { safeHtmlString } from '@/helpers/common';

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
