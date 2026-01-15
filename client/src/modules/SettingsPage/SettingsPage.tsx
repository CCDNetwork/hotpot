import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useRef, useState } from 'react';
import { useFieldArray, useForm } from 'react-hook-form';
import { Navigate } from 'react-router-dom';
import { Building2, Loader2, Rocket, Settings, UserPlus2 } from 'lucide-react';

import { ModeToggle } from '@/components/ModeToggle';
import { Button } from '@/components/ui/button';
import {
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';
import { toast } from '@/components/ui/use-toast';
import { APP_ROUTE } from '@/helpers/constants';
import { useAuth } from '@/providers/GlobalProvider';
import { api } from '@/services/api';
import { useHasOrganizations } from '@/services/organizations/api';
import { useSettings, useSettingsMutation } from '@/services/settings';

import {
  defaultInitialSetupValues,
  InitialSetupFormData,
  InitialSetupFormSchema,
  SettingsFormData,
  SettingsFormSchema,
} from './validations';
import { defaultSettingsFormValues } from './const';
import { DeploymentSettingsFields } from './components';

export const SettingsPage = () => {
  const { isLoggedIn, user, logoutUser } = useAuth();

  const [inputValue, setInputValue] = useState<string>('');
  const [valueAlreadyExists, setValueAlreadyExists] = useState<boolean>(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [setupComplete, setSetupComplete] = useState(false);
  const inputRef = useRef<HTMLInputElement | null>(null);

  const {
    data: hasOrganizations,
    isLoading: checkingOrgs,
    refetch: refetchOrgs,
  } = useHasOrganizations();
  const {
    data: settingsData,
    isLoading: settingsLoading,
    refetch: refetchSettings,
  } = useSettings({});
  const { updateSettings } = useSettingsMutation();

  // Form for normal settings (when initialized)
  const settingsForm = useForm<SettingsFormData>({
    defaultValues: defaultSettingsFormValues,
    resolver: zodResolver(SettingsFormSchema),
  });

  // Form for initial setup (when not initialized)
  const initialSetupForm = useForm<InitialSetupFormData>({
    defaultValues: defaultInitialSetupValues,
    resolver: zodResolver(InitialSetupFormSchema),
  });

  const {
    control: settingsControl,
    formState: settingsFormState,
    handleSubmit: handleSettingsSubmit,
    reset: resetSettings,
  } = settingsForm;
  const {
    control: setupControl,
    handleSubmit: handleSetupSubmit,
    watch: watchSetup,
    setValue: setSetupValue,
  } = initialSetupForm;

  const {
    fields: settingsFields,
    append: appendSettings,
    remove: removeSettings,
  } = useFieldArray({
    name: 'fundingSources',
    control: settingsControl,
  });

  const {
    fields: setupFields,
    append: appendSetup,
    remove: removeSetup,
  } = useFieldArray({
    name: 'fundingSources',
    control: setupControl,
  });

  const currentPermissions = watchSetup('permissions');

  useEffect(() => {
    if (settingsData) {
      resetSettings({
        ...settingsData,
        fundingSources: settingsData.fundingSources.length
          ? settingsData.fundingSources.map((i) => ({ value: i }))
          : [],
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [settingsData]);

  const onUpdateSettings = handleSettingsSubmit(async (values) => {
    try {
      await updateSettings.mutateAsync(values);
      toast({
        title: 'Success!',
        variant: 'default',
        description: 'Settings successfully updated.',
      });
    } catch (error: any) {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage || 'Something went wrong',
      });
    }
  });

  const onInitialSetup = handleSetupSubmit(async (values) => {
    setIsSubmitting(true);
    try {
      await api.post('/settings/initial-setup', {
        // Deployment Settings
        deploymentName: values.deploymentName,
        deploymentCountry: values.deploymentCountry,
        adminLevel1Name: values.adminLevel1Name,
        adminLevel2Name: values.adminLevel2Name,
        adminLevel3Name: values.adminLevel3Name,
        adminLevel4Name: values.adminLevel4Name,
        metabaseUrl: values.metabaseUrl,
        fundingSources: values.fundingSources.map((f) => f.value),
        // Organization
        organizationName: values.organizationName,
        isMpcaActive: values.isMpcaActive,
        isWashActive: values.isWashActive,
        isShelterActive: values.isShelterActive,
        isFoodAssistanceActive: values.isFoodAssistanceActive,
        isLivelihoodsActive: values.isLivelihoodsActive,
        isProtectionActive: values.isProtectionActive,
        // User
        firstName: values.firstName,
        lastName: values.lastName,
        email: values.email,
        password: values.password,
        permissions: values.permissions,
      });

      toast({
        title: 'Setup Complete!',
        variant: 'default',
        description:
          'Initial setup completed successfully. You can now log out and sign in with the new admin account.',
      });

      setSetupComplete(true);
      await refetchSettings();
      refetchOrgs();
    } catch (error: any) {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage ||
          'Error during initial setup. Please try again.',
      });
    } finally {
      setIsSubmitting(false);
    }
  });

  const handleInputValueChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(e.target.value);
  };

  const handleAddFundingSource = (isSetupMode: boolean) => {
    if (!inputValue) {
      inputRef.current?.focus();
      return;
    }
    setValueAlreadyExists(false);

    const fields = isSetupMode ? setupFields : settingsFields;
    if (
      fields.some((i) => i.value.toLowerCase() === inputValue.toLowerCase())
    ) {
      setValueAlreadyExists(true);
      inputRef.current?.focus();
      return;
    }

    if (isSetupMode) {
      appendSetup({ value: inputValue });
    } else {
      appendSettings({ value: inputValue });
    }
    setInputValue('');
  };

  const onPermissionClick = (permission: string) => {
    if (currentPermissions?.includes(permission)) {
      setSetupValue(
        'permissions',
        currentPermissions.filter((p) => p !== permission)
      );
    } else {
      setSetupValue('permissions', [...(currentPermissions || []), permission]);
    }
  };

  if (!isLoggedIn) {
    return <Navigate to={APP_ROUTE.SignIn} replace />;
  }

  if (isLoggedIn && !user.isSuperAdmin) {
    return <Navigate to={APP_ROUTE.Dashboard} replace />;
  }

  if (checkingOrgs || settingsLoading) {
    return (
      <div className="flex h-screen justify-center items-center">
        <Loader2 className="w-10 h-10 lg:w-20 lg:h-20 animate-spin" />
      </div>
    );
  }

  // INITIAL SETUP MODE
  if (!hasOrganizations) {
    // Show success message if setup just completed
    if (setupComplete) {
      return (
        <div className="flex justify-center items-center min-h-[100svh] w-screen py-8">
          <div className="relative p-6 pt-10 rounded-lg sm:border sm:border-border max-w-3xl w-full">
            <div className="inline-flex items-center sm:absolute w-full sm:justify-start justify-center sm:w-fit sm:left-10 sm:-top-4 font-semibold text-xl sm:text-md sm:pt-0 pt-6 sm:pb-0 pb-6 bg-background sm:px-1">
              <Settings className="size-7 mr-2" /> Deployment Settings
            </div>
            <div className="border border-green-500 bg-green-50 dark:bg-green-950 rounded-lg p-6">
              <div className="flex items-center gap-2 text-green-700 dark:text-green-300">
                <Rocket className="size-5" />
                <h3 className="font-semibold">Initial Setup Complete!</h3>
              </div>
              <p className="text-sm text-green-600 dark:text-green-400 mt-2">
                Your deployment settings, organization, and admin user have been
                created. Click &ldquo;Back to Sign In&rdquo; below and log in
                with your new admin credentials to access the full application.
              </p>
            </div>
            <CardFooter className="flex justify-center p-0 pt-6">
              <Button type="button" variant="link" onClick={logoutUser}>
                &larr; Back to Sign In
              </Button>
            </CardFooter>
          </div>
          <div className="absolute top-4 right-4">
            <ModeToggle />
          </div>
        </div>
      );
    }

    return (
      <div className="flex justify-center items-center min-h-[100svh] w-screen py-8">
        <div className="relative p-6 pt-10 rounded-lg sm:border sm:border-border max-w-3xl w-full">
          <div className="inline-flex items-center sm:absolute w-full sm:justify-start justify-center sm:w-fit sm:left-10 sm:-top-4 font-semibold text-xl sm:text-md sm:pt-0 pt-6 sm:pb-0 pb-6 bg-background sm:px-1">
            <Settings className="size-7 mr-2" /> Deployment Settings
          </div>

          <div className="border border-amber-500 bg-amber-50 dark:bg-amber-950 rounded-lg p-6">
            <div className="flex items-center gap-2 text-amber-700 dark:text-amber-300 mb-4">
              <Rocket className="size-5" />
              <h3 className="font-semibold">Initial Setup Required</h3>
            </div>
            <p className="text-sm text-amber-600 dark:text-amber-400 mb-6">
              Configure your deployment settings and create your first
              organization and admin user.
            </p>

            <Form {...initialSetupForm}>
              <form onSubmit={onInitialSetup}>
                <div className="space-y-6">
                  {/* Deployment Settings Section */}
                  <div className="bg-background rounded-lg p-4 border">
                    <CardHeader className="!p-0 !pb-4">
                      <CardTitle className="flex items-center gap-2 text-base">
                        <Settings className="size-4" />
                        Deployment Settings
                      </CardTitle>
                      <CardDescription>
                        Configure your deployment details.
                      </CardDescription>
                    </CardHeader>
                    <CardContent className="!p-0">
                      <DeploymentSettingsFields
                        control={setupControl}
                        fields={setupFields}
                        remove={removeSetup}
                        inputValue={inputValue}
                        inputRef={inputRef}
                        valueAlreadyExists={valueAlreadyExists}
                        onInputChange={handleInputValueChange}
                        onAddFundingSource={() => handleAddFundingSource(true)}
                      />
                    </CardContent>
                  </div>

                  {/* Organization Section */}
                  <div className="bg-background rounded-lg p-4 border">
                    <CardHeader className="!p-0 !pb-4">
                      <CardTitle className="flex items-center gap-2 text-base">
                        <Building2 className="size-4" />
                        Organization Details
                      </CardTitle>
                      <CardDescription>
                        Enter the details for your first organization.
                      </CardDescription>
                    </CardHeader>
                    <CardContent className="!p-0">
                      <div className="space-y-4">
                        <FormField
                          control={setupControl}
                          name="organizationName"
                          render={({ field }) => (
                            <FormItem>
                              <FormLabel requiredField>
                                Organization Name
                              </FormLabel>
                              <FormControl>
                                <Input
                                  placeholder="e.g. UNHCR, Red Cross, etc."
                                  {...field}
                                />
                              </FormControl>
                              <FormMessage />
                            </FormItem>
                          )}
                        />
                        <Separator />
                        <div>
                          <div className="text-sm font-medium mb-3">
                            Services Provided
                          </div>
                          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                            <FormField
                              control={setupControl}
                              name="isMpcaActive"
                              render={({ field }) => (
                                <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm space-y-0">
                                  <FormLabel className="text-sm">
                                    MPCA
                                  </FormLabel>
                                  <FormControl>
                                    <Switch
                                      checked={field.value}
                                      onCheckedChange={field.onChange}
                                    />
                                  </FormControl>
                                </FormItem>
                              )}
                            />
                            <FormField
                              control={setupControl}
                              name="isWashActive"
                              render={({ field }) => (
                                <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm space-y-0">
                                  <FormLabel className="text-sm">
                                    WASH
                                  </FormLabel>
                                  <FormControl>
                                    <Switch
                                      checked={field.value}
                                      onCheckedChange={field.onChange}
                                    />
                                  </FormControl>
                                </FormItem>
                              )}
                            />
                            <FormField
                              control={setupControl}
                              name="isShelterActive"
                              render={({ field }) => (
                                <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm space-y-0">
                                  <FormLabel className="text-sm">
                                    Shelter
                                  </FormLabel>
                                  <FormControl>
                                    <Switch
                                      checked={field.value}
                                      onCheckedChange={field.onChange}
                                    />
                                  </FormControl>
                                </FormItem>
                              )}
                            />
                            <FormField
                              control={setupControl}
                              name="isFoodAssistanceActive"
                              render={({ field }) => (
                                <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm space-y-0">
                                  <FormLabel className="text-sm">
                                    Food
                                  </FormLabel>
                                  <FormControl>
                                    <Switch
                                      checked={field.value}
                                      onCheckedChange={field.onChange}
                                    />
                                  </FormControl>
                                </FormItem>
                              )}
                            />
                            <FormField
                              control={setupControl}
                              name="isLivelihoodsActive"
                              render={({ field }) => (
                                <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm space-y-0">
                                  <FormLabel className="text-sm">
                                    Livelihoods
                                  </FormLabel>
                                  <FormControl>
                                    <Switch
                                      checked={field.value}
                                      onCheckedChange={field.onChange}
                                    />
                                  </FormControl>
                                </FormItem>
                              )}
                            />
                            <FormField
                              control={setupControl}
                              name="isProtectionActive"
                              render={({ field }) => (
                                <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm space-y-0">
                                  <FormLabel className="text-sm">
                                    Protection
                                  </FormLabel>
                                  <FormControl>
                                    <Switch
                                      checked={field.value}
                                      onCheckedChange={field.onChange}
                                    />
                                  </FormControl>
                                </FormItem>
                              )}
                            />
                          </div>
                        </div>
                      </div>
                    </CardContent>
                  </div>

                  {/* User Section */}
                  <div className="bg-background rounded-lg p-4 border">
                    <CardHeader className="!p-0 !pb-4">
                      <CardTitle className="flex items-center gap-2 text-base">
                        <UserPlus2 className="size-4" />
                        Admin User Details
                      </CardTitle>
                      <CardDescription>
                        Create an admin user who will manage this organization.
                      </CardDescription>
                    </CardHeader>
                    <CardContent className="!p-0">
                      <div className="space-y-4">
                        <div className="grid sm:grid-cols-2 grid-cols-1 gap-4">
                          <FormField
                            control={setupControl}
                            name="firstName"
                            render={({ field }) => (
                              <FormItem>
                                <FormLabel requiredField>First Name</FormLabel>
                                <FormControl>
                                  <Input placeholder="John" {...field} />
                                </FormControl>
                                <FormMessage />
                              </FormItem>
                            )}
                          />
                          <FormField
                            control={setupControl}
                            name="lastName"
                            render={({ field }) => (
                              <FormItem>
                                <FormLabel requiredField>Last Name</FormLabel>
                                <FormControl>
                                  <Input placeholder="Doe" {...field} />
                                </FormControl>
                                <FormMessage />
                              </FormItem>
                            )}
                          />
                        </div>
                        <FormField
                          control={setupControl}
                          name="email"
                          render={({ field }) => (
                            <FormItem>
                              <FormLabel requiredField>Email</FormLabel>
                              <FormControl>
                                <Input
                                  type="email"
                                  placeholder="admin@example.com"
                                  {...field}
                                />
                              </FormControl>
                              <FormMessage />
                            </FormItem>
                          )}
                        />
                        <div className="grid sm:grid-cols-2 grid-cols-1 gap-4">
                          <FormField
                            control={setupControl}
                            name="password"
                            render={({ field }) => (
                              <FormItem>
                                <FormLabel requiredField>Password</FormLabel>
                                <FormControl>
                                  <Input
                                    type="password"
                                    autoComplete="new-password"
                                    placeholder="Min. 8 characters"
                                    {...field}
                                  />
                                </FormControl>
                                <FormMessage />
                              </FormItem>
                            )}
                          />
                          <FormField
                            control={setupControl}
                            name="confirmPassword"
                            render={({ field }) => (
                              <FormItem>
                                <FormLabel requiredField>
                                  Confirm Password
                                </FormLabel>
                                <FormControl>
                                  <Input
                                    type="password"
                                    autoComplete="new-password"
                                    placeholder="Confirm password"
                                    {...field}
                                  />
                                </FormControl>
                                <FormMessage />
                              </FormItem>
                            )}
                          />
                        </div>
                        <Separator />
                        <div>
                          <div className="text-sm font-medium mb-3">
                            User Permissions
                          </div>
                          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                            <div className="flex flex-row items-center space-x-3 space-y-0 rounded-md border p-3">
                              <Checkbox
                                className="shadow-none"
                                checked={currentPermissions?.includes(
                                  'deduplication'
                                )}
                                onCheckedChange={() =>
                                  onPermissionClick('deduplication')
                                }
                              />
                              <FormLabel className="text-sm">
                                Deduplication
                              </FormLabel>
                            </div>
                            <div className="flex flex-row items-center space-x-3 space-y-0 rounded-md border p-3">
                              <Checkbox
                                className="shadow-none"
                                checked={currentPermissions?.includes(
                                  'referral'
                                )}
                                onCheckedChange={() =>
                                  onPermissionClick('referral')
                                }
                              />
                              <FormLabel className="text-sm">
                                Referrals
                              </FormLabel>
                            </div>
                            <div className="flex flex-row items-center space-x-3 space-y-0 rounded-md border p-3">
                              <Checkbox
                                className="shadow-none"
                                checked={currentPermissions?.includes(
                                  'booking'
                                )}
                                onCheckedChange={() =>
                                  onPermissionClick('booking')
                                }
                              />
                              <FormLabel className="text-sm">Booking</FormLabel>
                            </div>
                          </div>
                        </div>
                      </div>
                    </CardContent>
                  </div>

                  <Button
                    type="submit"
                    isLoading={isSubmitting}
                    disabled={isSubmitting}
                    className="w-full"
                    size="lg"
                  >
                    <Rocket className="size-4 mr-2" />
                    Complete Initial Setup
                  </Button>
                </div>
              </form>
            </Form>
          </div>

          <CardFooter className="flex justify-center p-0 pt-6">
            <Button type="button" variant="link" onClick={logoutUser}>
              &larr; Back to Sign In
            </Button>
          </CardFooter>
        </div>
        <div className="absolute top-4 right-4">
          <ModeToggle />
        </div>
      </div>
    );
  }

  // NORMAL MODE (initialized)
  return (
    <div className="flex justify-center items-center min-h-[100svh] w-screen py-8">
      <div className="relative p-6 pt-10 rounded-lg sm:border sm:border-border max-w-3xl w-full">
        <div className="inline-flex items-center sm:absolute w-full sm:justify-start justify-center sm:w-fit sm:left-10 sm:-top-4 font-semibold text-xl sm:text-md sm:pt-0 pt-6 sm:pb-0 pb-6 bg-background sm:px-1">
          <Settings className="size-7 mr-2" /> Deployment Settings
        </div>
        <Form {...settingsForm}>
          <div className="grid grid-cols-1 gap-4">
            <DeploymentSettingsFields
              control={settingsControl}
              fields={settingsFields}
              remove={removeSettings}
              inputValue={inputValue}
              inputRef={inputRef}
              valueAlreadyExists={valueAlreadyExists}
              onInputChange={handleInputValueChange}
              onAddFundingSource={() => handleAddFundingSource(false)}
            />
            <CardFooter className="flex justify-between p-0">
              <Button
                type="button"
                variant="link"
                className="px-0"
                onClick={logoutUser}
                disabled={settingsFormState.isSubmitting}
              >
                &larr; Back to Sign In
              </Button>
              <Button
                type="button"
                onClick={onUpdateSettings}
                isLoading={settingsFormState.isSubmitting}
                disabled={settingsFormState.isSubmitting}
              >
                Save settings
              </Button>
            </CardFooter>
          </div>
        </Form>
      </div>
      <div className="absolute top-4 right-4">
        <ModeToggle />
      </div>
    </div>
  );
};
