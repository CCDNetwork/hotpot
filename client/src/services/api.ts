import axios from 'axios';

import { LocalStorage } from '@/helpers/localStorage';

export const api = axios.create({
  baseURL: `${import.meta.env.VITE_API_URL}/api/v1`,
  timeout: 100000, // 100seconds,
});

api.defaults.headers.common.Authorization = `Bearer ${LocalStorage.getToken()}`;
api.defaults.headers.common['organization-id'] =
  `${LocalStorage.getOrganization()?.id}`;

// Track if we're reloading to prevent infinite reload loops
let isReloading = false;

api.interceptors.response.use(
  (config) => config,
  (error) => {
    const status = error.response?.status;

    if ((status === 401 || status === 403) && !isReloading) {
      const hadToken = LocalStorage.getToken();

      LocalStorage.removeToken();
      LocalStorage.removeUser();
      LocalStorage.removeOrganization();

      // If we had a stale token, reload to get a clean state
      if (hadToken) {
        isReloading = true;
        window.location.reload();
        return new Promise(() => {});
      }
    }

    return Promise.reject(error);
  }
);

export const unauthorizedHandler = (logoutFc: () => void) => (error: any) => {
  if (error.response !== undefined && error.response.status === 401) {
    logoutFc();
  }

  return Promise.reject(error);
};
