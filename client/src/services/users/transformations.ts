import { resToOrganization } from '@/services/organizations';
import { User } from '@/services/users';

export const resToUser = (res: any): User => {
  return {
    id: res.id,
    email: res.email ?? '',
    firstName: res.firstName ?? '',
    lastName: res.lastName ?? '',
    status: res.status ?? 'Active',
    activatedAt: res.activatedAt ? new Date(res.activatedAt) : null,
    createdAt: res.createdAt ? new Date(res.createdAt) : null,
    role: res.role ?? '',
    language: res.language ?? '',
    organizations: res.organizations
      ? res.organizations.map(resToOrganization)
      : [],
    permissions: res.permissions ?? [],
    isSuperAdmin: res.isSuperAdmin ?? false,
  };
};

export const userToReq = (data: any): Omit<User, 'id'> => {
  const req: any = {
    email: data.email,
    firstName: data.firstName,
    lastName: data.lastName,
    organizationId: data.organization?.id,
    permissions: data.permissions,
    role: data.role,
  };

  if (data.password) {
    req.password = data.password;
  }

  return req;
};
