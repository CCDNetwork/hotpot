import { UserRole } from '@/services/users/consts';

export const defaultUserEditFormFormValues = {
  firstName: '',
  lastName: '',
  email: '',
  password: '',
  confirmPassword: '',
  // organization: null,
  role: UserRole.User,
  permissions: [],
};
