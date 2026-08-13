import { zodResolver } from '@hookform/resolvers/zod';
import { useEffect, useRef, useState } from 'react';
import { useFieldArray, useForm } from 'react-hook-form';
import { Navigate } from 'react-router-dom';
import { Loader2, Settings, Trash2 } from 'lucide-react';

import { ModeToggle } from '@/components/ModeToggle';
import { Button } from '@/components/ui/button';
import { CardFooter } from '@/components/ui/card';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { toast } from '@/components/ui/use-toast';
import { APP_ROUTE } from '@/helpers/constants';
import { useAuth } from '@/providers/GlobalProvider';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

import { SettingsFormData, SettingsFormSchema } from './validations';
import { COUNTRIES_LIST, defaultSettingsFormValues } from './const';
import { useSettings, useSettingsMutation } from '@/services/settings';
import { Tooltip } from '@/components/Tooltip';

export const SettingsPage = () => {
  const { isLoggedIn, user, logoutUser } = useAuth();

  const [inputValue, setInputValue] = useState<string>('');
  const [valueAlreadyExists, setValueAlreadyExists] = useState<boolean>(false);
  const inputRef = useRef<HTMLInputElement | null>(null);

  const { data: settingsData, isLoading: settingsLoading } = useSettings({});

  const { updateSettings } = useSettingsMutation();

  const form = useForm<SettingsFormData>({
    defaultValues: defaultSettingsFormValues,
    resolver: zodResolver(SettingsFormSchema),
  });

  const { control, formState, handleSubmit, reset } = form;

  const { fields, append, remove } = useFieldArray({
    name: 'fundingSources',
    control,
  });

  useEffect(() => {
    if (settingsData) {
      reset({
        ...settingsData,
        fundingSources: settingsData.fundingSources.length
          ? settingsData.fundingSources.map((i) => ({ value: i }))
          : [],
      });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [settingsData]);

  const onSubmit = handleSubmit(async (values) => {
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

  const handleInputValueChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(e.target.value);
  };

  const handleAddBtnClick = () => {
    if (!inputValue) {
      inputRef.current?.focus();
      return;
    }
    setValueAlreadyExists(false);

    if (
      fields.some((i) => i.value.toLowerCase() === inputValue.toLowerCase())
    ) {
      setValueAlreadyExists(true);
      inputRef.current?.focus();
      return;
    }

    append({ value: inputValue });
    setInputValue('');
  };

  if (!isLoggedIn) {
    return <Navigate to={APP_ROUTE.SignIn} replace />;
  }

  if (isLoggedIn && !user.isSuperAdmin) {
    return <Navigate to={APP_ROUTE.Dashboard} replace />;
  }

  if (settingsLoading) {
    return (
      <div className="flex h-screen justify-center items-center">
        <Loader2 className="w-10 h-10 lg:w-20 lg:h-20 animate-spin" />
      </div>
    );
  }

  return (
    <div className="flex justify-center items-center h-[100svh] w-screen">
      <div className="relative p-6 pt-10 rounded-lg sm:border sm:border-border max-w-3xl w-full">
        <div className="inline-flex items-center sm:absolute w-full sm:justify-start justify-center sm:w-fit sm:left-10 sm:-top-4 font-semibold text-xl sm:text-md sm:pt-0 pt-6 sm:pb-0 pb-6 bg-background sm:px-1">
          <Settings className="size-7 mr-2" /> Deployment Settings
        </div>
        <Form {...form}>
          <div className="grid grid-cols-1 gap-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FormField
                control={control}
                name="deploymentName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Deployment Name</FormLabel>
                    <FormControl>
                      <Input
                        id="deploymentName"
                        placeholder="e.g. CCD Data Portal"
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={control}
                name="deploymentCountry"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Deployment Country</FormLabel>
                    <FormControl>
                      <Select
                        onValueChange={field.onChange}
                        value={field.value}
                      >
                        <FormControl>
                          <SelectTrigger>
                            <SelectValue placeholder="Select country" />
                          </SelectTrigger>
                        </FormControl>
                        <SelectContent>
                          {COUNTRIES_LIST.map(({ name, code }) => (
                            <SelectItem key={code} value={name}>
                              {name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FormField
                control={control}
                name="adminLevel1Name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Admin Level 1 Name</FormLabel>
                    <FormControl>
                      <Input
                        id="adminLevel1Name"
                        placeholder="Name"
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={control}
                name="adminLevel2Name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Admin Level 2 Name</FormLabel>
                    <FormControl>
                      <Input
                        id="adminLevel2Name"
                        placeholder="Name"
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <FormField
                control={control}
                name="adminLevel3Name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Admin Level 3 Name</FormLabel>
                    <FormControl>
                      <Input
                        id="adminLevel3Name"
                        placeholder="Name"
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={control}
                name="adminLevel4Name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Admin Level 4 Name</FormLabel>
                    <FormControl>
                      <Input
                        id="adminLevel4Name"
                        placeholder="Name"
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
              name="metabaseUrl"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Metabase Iframe URL</FormLabel>
                  <FormControl>
                    <Input
                      id="metabaseUrl"
                      maxLength={100}
                      placeholder="URL"
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <div>
              <FormLabel>Funding Sources</FormLabel>
              {fields.length > 0 && (
                <div className="pt-4 flex flex-wrap gap-2 w-full">
                  {fields.map((activity, idx) => (
                    <div
                      key={activity.id}
                      className="flex w-fit bg-muted/50 items-center justify-between gap-2 border border-border py-1 px-2.5 rounded-md animate-opacity"
                    >
                      <p className="text-sm line-clamp-2">{activity.value}</p>
                      <div>
                        <Tooltip tooltipContent="Remove">
                          <Button
                            size="icon"
                            onClick={() => remove(idx)}
                            className="h-7 w-7 text-destructive hover:text-red-600"
                            variant="ghost"
                          >
                            <Trash2 className="w-4 h-4" />
                          </Button>
                        </Tooltip>
                      </div>
                    </div>
                  ))}
                </div>
              )}
              <div className="pt-4">
                <div className="flex gap-4">
                  <div className="flex flex-col w-full">
                    <Input
                      type="text"
                      placeholder="Add a new funding source..."
                      ref={inputRef}
                      value={inputValue}
                      onChange={handleInputValueChange}
                    />
                    {valueAlreadyExists && (
                      <p className="text-sm pt-2 text-red-500">
                        Funding source already exists!
                      </p>
                    )}
                  </div>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={handleAddBtnClick}
                  >
                    Add
                  </Button>
                </div>
              </div>
            </div>
            <CardFooter className="flex justify-between p-0">
              <Button
                type="button"
                variant="link"
                className="px-0"
                onClick={logoutUser}
                disabled={formState.isSubmitting}
              >
                &larr; Back to Sign In
              </Button>
              <Button
                type="button"
                onClick={onSubmit}
                isLoading={formState.isSubmitting}
                disabled={formState.isSubmitting}
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
