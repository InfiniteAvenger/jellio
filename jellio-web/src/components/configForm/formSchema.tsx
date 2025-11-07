import { z } from 'zod';

export const formSchema = z.object({
  serverName: z.string(),
  libraries: z.array(
    z.object({
      key: z.string(),
      name: z.string(),
      type: z.string(),
    }),
  ),
  jellyseerrEnabled: z.boolean().default(false),
  jellyseerrUrl: z.string().url().or(z.literal('')).default(''),
  jellyseerrApiKey: z.string().default(''),
});

export type ConfigFormType = z.infer<typeof formSchema>;
