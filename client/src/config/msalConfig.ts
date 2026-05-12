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
    // B2C accepts the bare client-ID GUID as a scope to request an access
    // token usable against the same app's own API.
    // https://learn.microsoft.com/en-us/azure/active-directory-b2c/access-tokens#openid-connect-scopes
    import.meta.env.VITE_B2C_CLIENT_ID || '',
  ].filter(Boolean),
};

export const isB2CEnabled = (): boolean => {
  return (import.meta.env.VITE_AUTH_PROVIDER || 'local') === 'b2c';
};
