import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { NavLink } from 'react-router-dom';
import { APP_ROUTE } from '@/helpers/constants';

import { Button } from '@/components/ui/button';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { PublicPage } from '@/layouts/PublicPage';
import { toast } from '@/components/ui/use-toast';
import { useAuthMutation } from '@/services/auth';
import { useAuth } from '@/providers/GlobalProvider';
import { isB2CEnabled, loginRequest } from '@/config/msalConfig';

import { SignInFormData, SignInFormSchema, B2cSignInFormData, B2cSignInFormSchema } from './validations';
import { PasswordInput } from '@/components/PasswordInput';

const LocalSignInPage = () => {
  const { loginUser } = useAuth();
  const form = useForm<SignInFormData>({
    defaultValues: {
      username: '',
      password: '',
    },
    resolver: zodResolver(SignInFormSchema),
  });

  const { control, formState, handleSubmit } = form;

  const { login } = useAuthMutation();

  const onSubmit = handleSubmit(async (values) => {
    try {
      const authData = await login.mutateAsync(values);
      loginUser(authData);
      toast({
        title: 'Successfully logged in!',
        variant: 'default',
        description: `Welcome, ${authData.user.firstName}.`,
      });
    } catch (error: any) {
      toast({
        title: 'An error has occured!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage || 'Something went wrong',
      });
    }
  });

  return (
    <PublicPage
      title="Welcome back"
      subtitle="Enter your credentials to sign in to your account"
      boxClassName="sm:max-w-[400px]"
    >
      <Form {...form}>
        <form onSubmit={onSubmit} className="w-full">
          <div className="grid grid-cols-1 gap-4">
            <FormField
              control={control}
              name="username"
              render={({ field }) => (
                <FormItem>
                  <FormControl>
                    <Input
                      id="username"
                      placeholder="email@example.com"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={control}
              name="password"
              render={({ field }) => (
                <FormItem>
                  <FormControl>
                    <PasswordInput
                      id="password"
                      autoComplete="current-password"
                      placeholder="Password"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>
          <div className="flex justify-end pt-2 pb-4">
            <NavLink
              to={APP_ROUTE.RequestNewPassword}
              className="text-sm text-primary font-medium hover:underline focus:outline-primary"
            >
              Forgot your password?
            </NavLink>
          </div>
          <Button
            isLoading={formState.isSubmitting || login.isLoading}
            disabled={formState.isSubmitting || login.isLoading}
            type="submit"
            variant="default"
            className="w-full"
          >
            Sign in
          </Button>
        </form>
      </Form>
    </PublicPage>
  );
};

const B2cSignInPage = () => {
  const form = useForm<B2cSignInFormData>({
    defaultValues: {
      email: '',
    },
    resolver: zodResolver(B2cSignInFormSchema),
  });

  const { control, formState, handleSubmit } = form;
  const { loginInit } = useAuthMutation();

  const onSubmit = handleSubmit(async (values) => {
    try {
      const result = await loginInit.mutateAsync({ email: values.email });

      // Dynamic import to avoid loading MSAL when not in B2C mode
      const { PublicClientApplication } = await import('@azure/msal-browser');
      const { msalConfig } = await import('@/config/msalConfig');

      const msalInstance = new PublicClientApplication(msalConfig);
      await msalInstance.initialize();
      // Consumes any pending redirect response and clears stale
      // `interaction.status` from a previous incomplete flow that would
      // otherwise throw `interaction_in_progress` here.
      await msalInstance.handleRedirectPromise();

      await msalInstance.loginRedirect({
        ...loginRequest,
        loginHint: result.loginHint,
        prompt: 'login',
      });
    } catch (error: any) {
      if (error.response?.status === 400) {
        toast({
          title: 'Invalid email',
          variant: 'destructive',
          description: 'Please enter a valid email address.',
        });
      } else if (error.response?.status === 429) {
        toast({
          title: 'Too many requests',
          variant: 'destructive',
          description: 'Please wait a moment before trying again.',
        });
      } else {
        toast({
          title: 'An error has occured!',
          variant: 'destructive',
          description:
            error.response?.data?.errorMessage || 'Something went wrong',
        });
      }
    }
  });

  return (
    <PublicPage
      title="Welcome back"
      subtitle="Enter your email to sign in. You will be redirected to UNICEF's secure sign-in page."
      boxClassName="sm:max-w-[400px]"
    >
      <Form {...form}>
        <form onSubmit={onSubmit} className="w-full">
          <div className="grid grid-cols-1 gap-4">
            <FormField
              control={control}
              name="email"
              render={({ field }) => (
                <FormItem>
                  <FormControl>
                    <Input
                      id="email"
                      type="email"
                      placeholder="email@example.com"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>
          <div className="pt-4">
            <Button
              isLoading={formState.isSubmitting || loginInit.isLoading}
              disabled={formState.isSubmitting || loginInit.isLoading}
              type="submit"
              variant="default"
              className="w-full"
            >
              Continue
            </Button>
          </div>
        </form>
      </Form>
    </PublicPage>
  );
};

export const SignInPage = () => {
  if (isB2CEnabled()) {
    return <B2cSignInPage />;
  }
  return <LocalSignInPage />;
};
