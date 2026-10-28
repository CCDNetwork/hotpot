import { useEffect } from 'react';
import { useFieldArray, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';

import { PageContainer } from '@/components/PageContainer';
import { useIdFromParams } from '@/helpers/common';
import { toast } from '@/components/ui/use-toast';
import { Button } from '@/components/ui/button';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import {
  useOrganization,
  useOrganizationMutation,
} from '@/services/organizations/api';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { APP_ROUTE } from '@/helpers/constants';
import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';
import { OrgActivity } from '@/services/organizations';

import { dataToOrganizationEditFormData } from './form-transformation';
import {
  OrganizationEditFormSchema,
  OrganizationEditFormData,
} from './validations';
import { defaultOrganizationEditFormFormValues } from './const';

import { ServiceActivities } from '../components';

export const OrganizationPage = () => {
  const { id: organizationId } = useIdFromParams();

  const { data: organizationData, isLoading: queryLoading } = useOrganization({
    id: organizationId,
    isCreate: false,
  });

  const { editOrganization } = useOrganizationMutation();

  const form = useForm<OrganizationEditFormData>({
    defaultValues: defaultOrganizationEditFormFormValues,
    resolver: zodResolver(OrganizationEditFormSchema),
  });

  const { control, formState, handleSubmit, reset, watch } = form;

  const [
    isMPCAChecked,
    isWashChecked,
    isShelterChecked,
    isFoodAssistanceActive,
    isLivelihoodsActive,
    isProtectionActive,
    isIncentivePaymentsActive,
  ] = watch([
    'isMpcaActive',
    'isWashActive',
    'isShelterActive',
    'isFoodAssistanceActive',
    'isLivelihoodsActive',
    'isProtectionActive',
    'isIncentivePaymentsActive',
  ]);

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'activities',
  });

  useEffect(() => {
    if (organizationData) {
      reset(dataToOrganizationEditFormData(organizationData));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [organizationData]);

  const onSubmit = handleSubmit(async (values) => {
    try {
      await editOrganization.mutateAsync({ payload: values, organizationId });
      toast({
        title: 'Success!',
        variant: 'default',
        description: 'Organization successfully updated.',
      });
    } catch (error: any) {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage || 'Error updating organisation.',
      });
    }
  });

  return (
    <PageContainer
      pageTitle="Organization"
      pageSubtitle="Organization Details"
      isLoading={queryLoading}
      headerNode={
        <Button
          type="submit"
          onClick={onSubmit}
          isLoading={editOrganization.isLoading}
          disabled={
            formState.isSubmitting ||
            editOrganization.isLoading ||
            !formState.isDirty
          }
        >
          Save
        </Button>
      }
      breadcrumbs={[
        { href: `${APP_ROUTE.Organizations}`, name: 'Organisations' },
        { name: `${organizationData?.name}` },
      ]}
    >
      <Form {...form}>
        <div className="space-y-8 max-w-2xl">
          <Card className="sm:bg-secondary/10 border-0 sm:border sm:dark:bg-secondary/10 shadow-none">
            <CardContent className="pt-6">
              <div className="grid sm:grid-cols-2 grid-cols-1">
                <FormField
                  control={control}
                  name="name"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel requiredField>Organization name</FormLabel>
                      <FormControl>
                        <Input
                          id="name"
                          placeholder="Organization name"
                          {...field}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
            </CardContent>

            <Separator />

            <CardHeader>
              <CardTitle>Services</CardTitle>
              <CardDescription>
                Which services does this organisation provide?
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 gap-6">
                <div>
                  <FormField
                    control={control}
                    name="isMpcaActive"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm space-y-0">
                        <div className="space-y-0.5">
                          <FormLabel>MPCA</FormLabel>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />
                  {isMPCAChecked && (
                    <ServiceActivities
                      activities={fields}
                      addActivity={append}
                      removeActivity={remove}
                      serviceType={OrgActivity.Mpca}
                    />
                  )}
                </div>
                <Separator />
                <div>
                  <FormField
                    control={control}
                    name="isWashActive"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm space-y-0">
                        <div className="space-y-0.5">
                          <FormLabel>WASH</FormLabel>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />
                  {isWashChecked && (
                    <ServiceActivities
                      activities={fields}
                      addActivity={append}
                      removeActivity={remove}
                      serviceType={OrgActivity.Wash}
                    />
                  )}
                </div>
                <Separator />
                <div>
                  <FormField
                    control={control}
                    name="isShelterActive"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm space-y-0">
                        <div className="space-y-0.5">
                          <FormLabel>Shelter</FormLabel>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />
                  {isShelterChecked && (
                    <ServiceActivities
                      activities={fields}
                      addActivity={append}
                      removeActivity={remove}
                      serviceType={OrgActivity.Shelter}
                    />
                  )}
                </div>
                <Separator />
                <div>
                  <FormField
                    control={control}
                    name="isFoodAssistanceActive"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm space-y-0">
                        <div className="space-y-0.5">
                          <FormLabel>Food Assistance</FormLabel>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />
                  {isFoodAssistanceActive && (
                    <ServiceActivities
                      activities={fields}
                      addActivity={append}
                      removeActivity={remove}
                      serviceType={OrgActivity.FoodAssistance}
                    />
                  )}
                </div>
                <Separator />
                <div>
                  <FormField
                    control={control}
                    name="isLivelihoodsActive"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm space-y-0">
                        <div className="space-y-0.5">
                          <FormLabel>Livelihoods</FormLabel>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />
                  {isLivelihoodsActive && (
                    <ServiceActivities
                      activities={fields}
                      addActivity={append}
                      removeActivity={remove}
                      serviceType={OrgActivity.Livelihoods}
                    />
                  )}
                </div>
                <Separator />
                <div>
                  <FormField
                    control={control}
                    name="isProtectionActive"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm space-y-0">
                        <div className="space-y-0.5">
                          <FormLabel>Protection</FormLabel>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />
                  {isProtectionActive && (
                    <ServiceActivities
                      activities={fields}
                      addActivity={append}
                      removeActivity={remove}
                      serviceType={OrgActivity.Protection}
                    />
                  )}
                </div>
                <Separator />
                <div>
                  <FormField
                    control={control}
                    name="isIncentivePaymentsActive"
                    render={({ field }) => (
                      <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm space-y-0">
                        <div className="space-y-0.5">
                          <FormLabel>Incentive Payments</FormLabel>
                        </div>
                        <FormControl>
                          <Switch
                            checked={field.value}
                            onCheckedChange={field.onChange}
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />
                  {isIncentivePaymentsActive && (
                    <ServiceActivities
                      activities={fields}
                      addActivity={append}
                      removeActivity={remove}
                      serviceType={OrgActivity.IncentivePayments}
                    />
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </Form>
    </PageContainer>
  );
};
