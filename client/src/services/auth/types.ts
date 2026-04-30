import { Organization } from '@/services/organizations';
import { User } from '@/services/users';

export interface AuthData {
  token: string;
  user: User;
  organization: Organization;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface ResetPasswordRequest {
  email: string;
  passwordResetCode: string;
  password: string;
  passwordConfirmation: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface LoginInitRequest {
  email: string;
}

export interface LoginInitResponse {
  action: string;
  loginHint: string;
}

export interface B2cTokenExchangeRequest {
  accessToken: string;
}
