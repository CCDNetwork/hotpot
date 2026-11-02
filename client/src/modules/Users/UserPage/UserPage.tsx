import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';

import { PageContainer } from '@/components/PageContainer';
import { useUser, useUserMutation } from '@/services/users/api';
import { useIdFromParams } from '@/helpers/common';
import { toast } from '@/components/ui/use-toast';
import { Button } from '@/components/ui/button';
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import { APP_ROUTE } from '@/helpers/constants';
import { Checkbox } from '@/components/ui/checkbox';
import { UserRole } from '@/services/users';

import { dataToUserEditFormData } from './form-transformation';
import { defaultUserEditFormFormValues } from './const';
import { UserEditFormData, UserEditFormSchema } from './validations';

export const UserPage = () => {
  const { id: userId } = useIdFromParams();

  const { data: userData, isLoading: queryLoading } = useUser({
    id: userId,
    isCreate: false,
  });
  const { patchUser } = useUserMutation();

  const form = useForm<UserEditFormData>({
    defaultValues: defaultUserEditFormFormValues,
    resolver: zodResolver(UserEditFormSchema),
  });

  const { control, formState, handleSubmit, reset, watch, setValue } = form;

  const currentFormPermissions = watch('permissions');

  useEffect(() => {
    if (userData) {
      reset(dataToUserEditFormData(userData));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userData]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      await patchUser.mutateAsync({ data: values, userId });
      toast({
        title: 'Success!',
        variant: 'default',
        description: 'User successfully updated.',
      });
    } catch (error: any) {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage || 'Error updating user.',
      });
    }
  });

  const onPermissionClick = (permission: string) => {
    if (currentFormPermissions?.includes(permission)) {
      const filteredFormPermissions = currentFormPermissions?.filter(
        (i) => i !== permission
      );
      setValue('permissions', filteredFormPermissions);
      return;
    }

    setValue('permissions', [...currentFormPermissions, permission]);
  };

  return (
    <PageContainer
      pageTitle="User Details"
      isLoading={queryLoading}
      breadcrumbs={[
        { href: `${APP_ROUTE.Users}`, name: 'Users' },
        { name: `${userData?.firstName} ${userData?.lastName}` },
      ]}
      headerNode={
        <Button
          type="submit"
          onClick={onSubmit}
          isLoading={patchUser.isLoading}
          disabled={formState.isSubmitting || patchUser.isLoading}
        >
          Save
        </Button>
      }
    >
      <Form {...form}>
        <form onSubmit={onSubmit}>
          <div className="space-y-8 max-w-2xl">
            <Card className="sm:bg-secondary/10 border-0 sm:border sm:dark:bg-secondary/10 shadow-none">
              <CardContent className="space-y-2 pt-6">
                <div className="grid grid-cols-1 gap-4">
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <FormField
                      control={control}
                      name="firstName"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel requiredField>First name</FormLabel>
                          <FormControl>
                            <Input
                              id="firstName"
                              placeholder="First name"
                              {...field}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={control}
                      name="lastName"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel requiredField>Last name</FormLabel>
                          <FormControl>
                            <Input
                              id="lastName"
                              placeholder="Last name"
                              {...field}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>
                  <FormField
                    control={control}
                    name="email"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Email</FormLabel>
                        <FormControl>
                          <Input
                            id="email"
                            disabled
                            autoComplete="new-password"
                            placeholder="Email"
                            type="email"
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  {userData?.role === UserRole.User && (
                    <div className="flex flex-col gap-3">
                      <div className="text-sm font-medium leading-none">
                        Permissions
                      </div>
                      <div className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
                        <Checkbox
                          className="shadow-none"
                          checked={currentFormPermissions?.includes(
                            'deduplication'
                          )}
                          onCheckedChange={() =>
                            onPermissionClick('deduplication')
                          }
                        />
                        <div className="space-y-1 leading-3">
                          <FormLabel>Deduplication</FormLabel>
                          <FormDescription>
                            User can use deduplication.
                          </FormDescription>
                        </div>
                      </div>
                      <div className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
                        <Checkbox
                          className="shadow-none"
                          checked={currentFormPermissions?.includes('referral')}
                          onCheckedChange={() => onPermissionClick('referral')}
                        />
                        <div className="space-y-1 leading-3">
                          <FormLabel>Referrals</FormLabel>
                          <FormDescription>
                            User can use referrals.
                          </FormDescription>
                        </div>
                      </div>
                      <div className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
                        <Checkbox
                          className="shadow-none"
                          checked={currentFormPermissions?.includes('booking')}
                          onCheckedChange={() => onPermissionClick('booking')}
                        />
                        <div className="space-y-1 leading-3">
                          <FormLabel>Booking</FormLabel>
                          <FormDescription>
                            User can use booking.
                          </FormDescription>
                        </div>
                      </div>
                    </div>
                  )}
                  <FormField
                    control={control}
                    name="password"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Password</FormLabel>
                        <FormControl>
                          <Input
                            id="password"
                            autoComplete="new-password"
                            placeholder="Password"
                            type="password"
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={control}
                    name="confirmPassword"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Confirm password</FormLabel>
                        <FormControl>
                          <Input
                            id="confirmPassword"
                            autoComplete="new-password"
                            placeholder="Confirm password"
                            type="password"
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>
              </CardContent>
            </Card>
          </div>
        </form>
      </Form>
    </PageContainer>
  );
};
