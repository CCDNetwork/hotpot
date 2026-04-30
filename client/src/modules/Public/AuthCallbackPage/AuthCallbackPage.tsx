import { useEffect, useState } from 'react';
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

  useEffect(() => {
    const handleCallback = async () => {
      try {
        const msalInstance = new PublicClientApplication(msalConfig);
        await msalInstance.initialize();

        const response = await msalInstance.handleRedirectPromise();

        if (!response) {
          navigate('/sign-in');
          return;
        }

        const authData = await b2cTokenExchange(response.accessToken);
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
