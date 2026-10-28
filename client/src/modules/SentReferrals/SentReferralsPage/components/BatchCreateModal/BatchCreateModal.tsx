import { useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { BadgeXIcon, Download } from 'lucide-react';

import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { toast } from '@/components/ui/use-toast';
import { AsyncSelect } from '@/components/AsyncSelect';
import { useOrganizationsInfinite } from '@/services/organizations/api';
import { Checkbox } from '@/components/ui/checkbox';
import { useReferralMutation } from '@/services/referrals/api';
import { SingleFileDropzone } from '@/components/SingleFileDropzone';
import { createDownloadLink } from '@/helpers/common';
import { BatchCreateResponse } from '@/services/referrals';

import { BatchCreateFormSchema, BatchCreateModalForm } from './validation';

interface BatchCreateModalProps {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
}

export const BatchCreateModal: React.FC<BatchCreateModalProps> = ({
  open,
  setOpen,
}) => {
  const [badFileResponse, setBadFileResponse] =
    useState<BatchCreateResponse | null>(null);

  const form = useForm<BatchCreateModalForm>({
    defaultValues: {
      file: undefined,
      organizationReferredTo: undefined,
      serviceCategory: '',
      batchType: '',
    },
    mode: 'onChange',
    resolver: zodResolver(BatchCreateFormSchema),
  });

  const { control, handleSubmit, reset, watch, setValue } = form;

  const currentFormSelectedOrganization = watch('organizationReferredTo');
  const currentFormServiceCategory = watch('serviceCategory');

  const { batchCreateReferrals } = useReferralMutation();

  const resolvedServiceCategories = useMemo(() => {
    if (!currentFormSelectedOrganization) return;

    const categoryMappings: {
      key:
        | 'isMpcaActive'
        | 'isWashActive'
        | 'isShelterActive'
        | 'isLivelihoodsActive'
        | 'isFoodAssistanceActive'
        | 'isProtectionActive'
        | 'isIncentivePaymentsActive';
      id: string;
      label: string;
    }[] = [
      { key: 'isMpcaActive', id: 'mpca', label: 'MPCA' },
      { key: 'isWashActive', id: 'wash', label: 'WASH' },
      { key: 'isShelterActive', id: 'shelter', label: 'Shelter' },
      { key: 'isLivelihoodsActive', id: 'livelihoods', label: 'Livelihoods' },
      {
        key: 'isFoodAssistanceActive',
        id: 'foodAssistance',
        label: 'Food Assistance',
      },
      { key: 'isProtectionActive', id: 'protection', label: 'Protection' },
      {
        key: 'isIncentivePaymentsActive',
        id: 'incentivePayments',
        label: 'Incentive Payments',
      },
    ];

    return (
      categoryMappings
        .filter((mapping) => currentFormSelectedOrganization[mapping.key])
        .map((mapping) => ({ id: mapping.id, label: mapping.label })) || []
    );
  }, [currentFormSelectedOrganization]);

  const selectedOrganizationHasServices = useMemo(() => {
    if (!currentFormSelectedOrganization) return false;

    return [
      currentFormSelectedOrganization.isMpcaActive,
      currentFormSelectedOrganization.isShelterActive,
      currentFormSelectedOrganization.isWashActive,
      currentFormSelectedOrganization.isFoodAssistanceActive,
      currentFormSelectedOrganization.isProtectionActive,
      currentFormSelectedOrganization.isIncentivePaymentsActive,
      currentFormSelectedOrganization.isLivelihoodsActive,
    ].some(Boolean);
  }, [currentFormSelectedOrganization]);

  const resolvedCurrentOrgSubactivities = useMemo(() => {
    if (!currentFormServiceCategory && !currentFormSelectedOrganization) {
      return;
    }

    return (
      currentFormSelectedOrganization?.activities.filter(
        (i) => i.serviceType === currentFormServiceCategory
      ) ?? []
    );
  }, [currentFormServiceCategory, currentFormSelectedOrganization]);

  const handleOpenChange = () => {
    setOpen(false);

    setTimeout(() => {
      setBadFileResponse(null);
      reset();
    }, 300);
  };

  const onSubmit = handleSubmit(async (values) => {
    try {
      const resp = await batchCreateReferrals.mutateAsync(values);
      if (resp.missingRequiredFields) {
        setBadFileResponse(resp);

        return;
      }

      handleOpenChange();
      toast({
        title: 'Success!',
        variant: 'default',
        description: 'Referrals successfully imported.',
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

  const handleDownloadBadFile = (url: string, filename?: string) => {
    createDownloadLink(url, filename);
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-[700px]">
        {!badFileResponse ? (
          <>
            <DialogHeader>
              <DialogTitle className="text-xl leading-none">
                Batch Import Referrals
              </DialogTitle>
            </DialogHeader>
            <Form {...form}>
              <form onSubmit={onSubmit} className="flex flex-col gap-4">
                <div className="grid gap-4">
                  <FormField
                    control={control}
                    name="batchType"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel requiredField>Import data type</FormLabel>
                        <Select
                          onValueChange={field.onChange}
                          value={field.value}
                        >
                          <FormControl>
                            <SelectTrigger>
                              <SelectValue placeholder="Select import data type" />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            <SelectItem value="generalBeneficiary">
                              General beneficiary data
                            </SelectItem>
                            <SelectItem value="beneficiaryWithMPCA">
                              Beneficiary data with MPCA
                            </SelectItem>
                            <SelectItem value="beneficiaryMinors">
                              Beneficiary minors
                            </SelectItem>
                          </SelectContent>
                        </Select>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <div className="grid sm:grid-cols-2 gap-4">
                    <AsyncSelect
                      requiredField
                      label="Receiving organisation"
                      name="organizationReferredTo"
                      control={control}
                      useInfiniteQueryFunction={useOrganizationsInfinite}
                      labelKey="name"
                      valueKey="id"
                      required
                    />
                    <FormField
                      control={control}
                      name="serviceCategory"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel requiredField>Service category</FormLabel>
                          <Select
                            onValueChange={(value) => {
                              setValue('subactivities', []);
                              field.onChange(value);
                            }}
                            value={field.value}
                          >
                            <FormControl>
                              <SelectTrigger
                                disabled={
                                  !currentFormSelectedOrganization ||
                                  !selectedOrganizationHasServices
                                }
                              >
                                <SelectValue placeholder="Select service category" />
                              </SelectTrigger>
                            </FormControl>
                            <SelectContent>
                              {resolvedServiceCategories &&
                                resolvedServiceCategories.length > 0 &&
                                resolvedServiceCategories.map((i) => (
                                  <SelectItem key={i.id} value={i.id}>
                                    {i.label}
                                  </SelectItem>
                                ))}
                            </SelectContent>
                          </Select>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>

                  {resolvedCurrentOrgSubactivities &&
                    resolvedCurrentOrgSubactivities.length > 0 && (
                      <FormField
                        control={control}
                        name="subactivities"
                        render={() => (
                          <FormItem>
                            <div className="mb-4">
                              <FormLabel>List of (sub)activities</FormLabel>
                            </div>
                            {resolvedCurrentOrgSubactivities.map((item) => (
                              <FormField
                                key={item.id}
                                control={control}
                                name="subactivities"
                                render={({ field }) => {
                                  return (
                                    <FormItem
                                      key={item.id}
                                      className="flex flex-row items-center space-x-3 space-y-0"
                                    >
                                      <FormControl>
                                        <Checkbox
                                          checked={
                                            !!field.value?.find(
                                              (i) => i.id === item.id
                                            )
                                          }
                                          onCheckedChange={(checked) => {
                                            return checked
                                              ? field.onChange([
                                                  ...field.value,
                                                  item,
                                                ])
                                              : field.onChange(
                                                  field.value?.filter(
                                                    (value: any) =>
                                                      value.id !== item.id
                                                  )
                                                );
                                          }}
                                        />
                                      </FormControl>
                                      <FormLabel className="text-sm font-normal">
                                        {item.title}
                                      </FormLabel>
                                    </FormItem>
                                  );
                                }}
                              />
                            ))}
                          </FormItem>
                        )}
                      />
                    )}
                </div>
                <SingleFileDropzone control={control} name="file" />
                <DialogFooter>
                  <Button
                    type="submit"
                    onClick={onSubmit}
                    isLoading={batchCreateReferrals.isLoading}
                    disabled={batchCreateReferrals.isLoading}
                  >
                    Import
                  </Button>
                </DialogFooter>
              </form>
            </Form>
          </>
        ) : (
          <div className="animate-opacity flex flex-col justify-center items-center py-10 px-12 text-center">
            <BadgeXIcon
              fill="#ff0000"
              stroke="white"
              strokeWidth={1}
              className="h-28 w-28"
            />
            <h4 className="text-2xl font-semibold md:text-3xl tracking-tight">
              Validation errors detected!
            </h4>

            <p className="text-muted-foreground">
              The file you uploaded contains validation errors.
            </p>
            <p className="text-muted-foreground pt-4 text-sm">
              We have created a copy of your file with cells that contain errors
              highlighted in red. Please download it below and after ensuring
              that there are no more errors, try again.
            </p>
            <div className="flex items-center mt-6">
              <Button
                onClick={() => handleDownloadBadFile(badFileResponse.file?.url)}
                variant="outline"
              >
                <Download className="size-5 mr-2" />
                Download file
              </Button>
            </div>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
};
