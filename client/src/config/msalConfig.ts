import { Configuration, LogLevel } from '@azure/msal-browser';

export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_B2C_CLIENT_ID || '',
    authority: `https://${import.meta.env.VITE_B2C_TENANT || ''}.b2clogin.com/${import.meta.env.VITE_B2C_TENANT || ''}.onmicrosoft.com/${import.meta.env.VITE_B2C_USER_FLOW || ''}`,
    knownAuthorities: [
      `${import.meta.env.VITE_B2C_TENANT || ''}.b2clogin.com`,
    ],
    redirectUri: import.meta.env.VITE_B2C_REDIRECT_URI || window.location.origin + '/auth/callback',
    postLogoutRedirectUri: window.location.origin + '/sign-in',
    navigateToLoginRequestUrl: false,
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      logLevel: LogLevel.Warning,
    },
  },
};

export const loginRequest = {
  scopes: [
    'openid',
    'offline_access',
    import.meta.env.VITE_B2C_API_SCOPE || '',
  ].filter(Boolean),
};

export const isB2CEnabled = (): boolean => {
  return (import.meta.env.VITE_AUTH_PROVIDER || 'local') === 'b2c';
};
