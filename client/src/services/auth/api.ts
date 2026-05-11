import { api } from '@/services';
import { useMutation } from '@tanstack/react-query';

import { resToAuthData } from './transformations';
import {
  AuthData,
  ForgotPasswordRequest,
  LoginInitRequest,
  LoginInitResponse,
  LoginRequest,
  ResetPasswordRequest,
} from './types';

export const login = async (data: LoginRequest): Promise<AuthData> => {
  const resp = await api.post('/authentication/login', data);
  return resToAuthData(resp.data);
};

export const forgotPassword = async (
  data: ForgotPasswordRequest
): Promise<object> => {
  const resp = await api.post('/authentication/forgot-password', data);
  return resp.data;
};

export const resetPassword = async (
  data: ResetPasswordRequest
): Promise<object> => {
  const resp = await api.post('/authentication/reset-password', data);
  return resp.data;
};

export const loginInit = async (
  data: LoginInitRequest
): Promise<LoginInitResponse> => {
  const resp = await api.post('/authentication/login-init', data);
  return resp.data;
};

export const b2cTokenExchange = async (token: string): Promise<AuthData> => {
  const resp = await api.post(
    '/authentication/b2c/token-exchange',
    {},
    { headers: { Authorization: `Bearer ${token}` } }
  );
  return resToAuthData(resp.data);
};

//
// Mutation hooks
//
export const useAuthMutation = () => {
  return {
    login: useMutation(login),
    forgotPassword: useMutation(forgotPassword),
    resetPassword: useMutation(resetPassword),
    loginInit: useMutation(loginInit),
    b2cTokenExchange: useMutation(b2cTokenExchange),
  };
};
