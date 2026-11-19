import { useState } from 'react';
import { Building2, SendHorizonal } from 'lucide-react';
import { useFieldArray, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';

import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { useOrganizationMutation } from '@/services/organizations/api';
import { toast } from '@/components/ui/use-toast';

import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';
import {
  NewOrganizationEmailDomains,
  PendingEmailDomain,
  ServiceActivities,
} from '@/modules/Organizations/components';

import {
  AddOrganizationModalFormSchema,
  AddOrganizationModalForm,
} from './validation';
import { defaultNewOrganizationFormFormValues } from './const';
import { OrgActivity } from '@/services/organizations';
import {
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';

export const AddOrganizationModal = () => {
  const [open, setOpen] = useState<boolean>(false);
  const [emailDomains, setEmailDomains] = useState<PendingEmailDomain[]>([]);
  const [domainDraft, setDomainDraft] = useState('');
  const form = useForm<AddOrganizationModalForm>({
    defaultValues: defaultNewOrganizationFormFormValues,
    mode: 'onSubmit',
    resolver: zodResolver(AddOrganizationModalFormSchema),
  });

  const { control, handleSubmit, reset, watch } = form;

  const [
    isMPCAChecked,
    isWashChecked,
    isShelterChecked,
    isFoodAssistanceChecked,
    isLivelihoodsChecked,
    isProtectionChecked,
    isIncentivePaymentsChecked,
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

  const { addOrganization } = useOrganizationMutation();

  const onSubmit = handleSubmit(async (values) => {
    if (domainDraft.trim()) {
      toast({
        title: 'Domain not added',
        variant: 'destructive',
        description: `Click "Add" to include '${domainDraft.trim()}' in the allowed email domains, or clear the field.`,
      });
      return;
    }
    try {
      await addOrganization.mutateAsync({
        ...values,
        emailDomains: emailDomains.map((d) => d.domain),
      });
      toast({
        title: 'Success!',
        variant: 'default',
        description: 'Organisation successfully created.',
      });
      setOpen(false);
    } catch (error: any) {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage || 'Error creating organisation.',
      });
    }
  });

  const onOpenChange = () => {
    setOpen((old) => !old);
    reset();
    setEmailDomains([]);
    setDomainDraft('');
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogTrigger asChild>
        <Button variant="outline">
          <Building2 className="mr-2 size-5" />
          Add new
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[500px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Add a new organisation</DialogTitle>
          <DialogDescription>
            Submit organisation information below.
          </DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form onSubmit={onSubmit}>
            <div className="grid grid-cols-1 gap-4">
              <FormField
                control={control}
                name="name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel requiredField>Organisation name</FormLabel>
                    <FormControl>
                      <Input
                        id="name"
                        placeholder="Organisation name"
                        type="text"
                        {...field}
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <Separator />
              <CardHeader className="!p-0">
                <CardTitle>Services</CardTitle>
                <CardDescription>
                  Which services does this organisation provide?
                </CardDescription>
              </CardHeader>
              <CardContent className="!p-0">
                <div className="grid grid-cols-1 gap-4">
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
                    {isFoodAssistanceChecked && (
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
                    {isLivelihoodsChecked && (
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
                    {isProtectionChecked && (
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
                    {isIncentivePaymentsChecked && (
                      <ServiceActivities
                        activities={fields}
                        addActivity={append}
                        removeActivity={remove}
                        serviceType={OrgActivity.IncentivePayments}
                      />
                    )}
                  </div>
                  <Separator />
                  <NewOrganizationEmailDomains
                    value={emailDomains}
                    onChange={setEmailDomains}
                    draft={domainDraft}
                    onDraftChange={setDomainDraft}
                  />
                </div>
              </CardContent>
              <DialogFooter>
                <div className="w-full">
                  <Button
                    type="submit"
                    onClick={onSubmit}
                    isLoading={addOrganization.isLoading}
                    disabled={addOrganization.isLoading}
                    size="icon"
                    className="w-full mt-2"
                  >
                    <SendHorizonal className="size-5 mr-2" />
                    Submit
                  </Button>
                </div>
              </DialogFooter>
            </div>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
};
