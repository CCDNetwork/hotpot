import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { PublicClientApplication } from '@azure/msal-browser';
import { msalConfig } from '@/config/msalConfig';
import { b2cTokenExchange } from '@/services/auth/api';
import { useAuth } from '@/providers/GlobalProvider';
import { PublicPage } from '@/layouts/PublicPage';
import { toast } from '@/components/ui/use-toast';

export const AuthCallbackPage = () => {
  const { loginUser } = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const hasHandledRef = useRef(false);

  useEffect(() => {
    // StrictMode runs effects twice in dev; the second run would race
    // handleRedirectPromise against the first and resolve to null after
    // the hash was already consumed, then redirect to /sign-in.
    if (hasHandledRef.current) return;
    hasHandledRef.current = true;

    const handleCallback = async () => {
      try {
        const msalInstance = new PublicClientApplication(msalConfig);
        await msalInstance.initialize();

        const response = await msalInstance.handleRedirectPromise();

        if (!response) {
          navigate('/sign-in');
          return;
        }

        // TODO: revert to response.accessToken once the B2C admin exposes
        // the `access_as_user` scope on the app and grants admin consent.
        // Until then, send the ID token: SPA and API are the same app
        // registration, so its `aud` already matches B2C_CLIENT_ID.
        const authData = await b2cTokenExchange(response.idToken);
        loginUser(authData);

        toast({
          title: 'Successfully logged in!',
          variant: 'default',
          description: `Welcome, ${authData.user.firstName}.`,
        });

        navigate('/');
      } catch (err: any) {
        const message =
          err.response?.data?.errorMessage || err.message || 'Authentication failed';
        setError(message);
        toast({
          title: 'Authentication failed',
          variant: 'destructive',
          description: message,
        });
      }
    };

    handleCallback();
  }, [loginUser, navigate]);

  if (error) {
    return (
      <PublicPage
        title="Authentication Failed"
        subtitle={error}
        boxClassName="sm:max-w-[400px]"
        shouldRedirect={false}
      >
        <a
          href="/sign-in"
          className="text-sm text-primary font-medium hover:underline"
        >
          Back to sign in
        </a>
      </PublicPage>
    );
  }

  return (
    <PublicPage
      title="Signing you in..."
      subtitle="Please wait while we complete your authentication."
      boxClassName="sm:max-w-[400px]"
      isLoading={true}
      shouldRedirect={false}
    />
  );
};
