import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { api } from '@/services/api';

export interface OrganizationEmailDomain {
  id: string;
  domain: string;
  /** Other organisations whose list contains the same domain. */
  alsoUsedBy: string[];
  createdAt: string;
  updatedAt: string;
}

export interface EmailDomainCheck {
  /** The domain as it will be saved (normalised). */
  domain: string;
  alsoUsedBy: string[];
}

export interface EmailDomainChange {
  id: string;
  action: 'added' | 'updated' | 'removed';
  oldValue: string | null;
  newValue: string | null;
  performedByEmail: string | null;
  createdAt: string;
}

const base = (organizationId: string) =>
  `/organizations/${organizationId}/email-domains`;

const QueryKey = 'organization-email-domains';

export const useOrganizationEmailDomains = (organizationId: string) =>
  useQuery(
    [QueryKey, organizationId],
    async (): Promise<OrganizationEmailDomain[]> =>
      (await api.get(base(organizationId))).data,
    { enabled: !!organizationId }
  );

export const useOrganizationEmailDomainHistory = (organizationId: string) =>
  useQuery(
    [QueryKey, organizationId, 'history'],
    async (): Promise<EmailDomainChange[]> =>
      (await api.get(`${base(organizationId)}/history`)).data,
    { enabled: !!organizationId }
  );

/** Validates and normalises a domain without saving it. */
export const checkEmailDomain = async (
  organizationId: string,
  domain: string,
  excludeDomainId?: string
): Promise<EmailDomainCheck> =>
  (
    await api.post(
      `${base(organizationId)}/check${
        excludeDomainId ? `?excludeDomainId=${excludeDomainId}` : ''
      }`,
      { domain }
    )
  ).data;

/** Validates a domain on the Add Organisation form (organisation not created yet). */
export const checkEmailDomainForNewOrganization = async (
  domain: string
): Promise<EmailDomainCheck> =>
  (await api.post('/organizations/email-domains/check', { domain })).data;

export const useOrganizationEmailDomainMutations = (organizationId: string) => {
  const queryClient = useQueryClient();
  const onSuccess = () =>
    queryClient.invalidateQueries([QueryKey, organizationId]);

  return {
    addDomain: useMutation(
      async (domain: string) =>
        (await api.post(base(organizationId), { domain })).data,
      { onSuccess }
    ),
    updateDomain: useMutation(
      async ({ id, domain }: { id: string; domain: string }) =>
        (await api.put(`${base(organizationId)}/${id}`, { domain })).data,
      { onSuccess }
    ),
    removeDomain: useMutation(
      async (id: string) => api.delete(`${base(organizationId)}/${id}`),
      { onSuccess }
    ),
  };
};
