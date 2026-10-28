import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate } from 'react-router-dom';

import { PageContainer } from '@/components/PageContainer';
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
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { APP_ROUTE } from '@/helpers/constants';
import { useReferral, useReferralMutation } from '@/services/referrals/api';
import { Separator } from '@/components/ui/separator';
import { AsyncSelect } from '@/components/AsyncSelect';
import { useOrganizationsInfinite } from '@/services/organizations/api';
import { Textarea } from '@/components/ui/textarea';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { FilesDropzone } from '@/components/FilesDropzone';
import { StatusTimeline } from '@/components/StatusTimeline';
import { ReferralStatus, ReferralTab } from '@/services/referrals/const';
import { DatePicker } from '@/components/DatePicker';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Checkbox } from '@/components/ui/checkbox';
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ReferralDiscussions } from '@/components/ReferralDiscussions';
import { useAuth } from '@/providers/GlobalProvider';

import { dataToSentReferralFormData } from './form-transformation';
import { SentReferralFormData, SentReferralSchema } from './validations';
import { defaultSentReferralFormFormValues } from './const';
import { MinorForm } from './components/MinorForm';
import { MpcaSpecificForm } from './components/MpcaSpecificForm';
import { SentReferralPageViewOnly } from './components/SentReferralPageViewOnly';
import { useSentReferralsProvider } from '../SentReferralsProvider';
import { StatusReasonModal } from '@/components/StatusReasonModal';
import { OrgActivity } from '@/services/organizations';
import {
  getAdministrativeRegionById,
  useAdminRegionsInfinite,
} from '@/services/administrativeRegions';
import { AdministrativeRegion } from '@/services/administrativeRegions/types';

export const SentReferralPage = () => {
  const navigate = useNavigate();
  const { deploymentSettings } = useAuth();
  const { id: sentReferralId, isCreate } = useIdFromParams();
  const { viewOnlyEnabled, setViewOnlyEnabled } = useSentReferralsProvider();

  const [activeTab, setActiveTab] = useState<
    ReferralTab.Discussion | ReferralTab.Referral
  >(ReferralTab.Referral);
  const [isStatusReasonModal, setOpenStatusReasonModal] =
    useState<boolean>(false);
  const [adminLevelsPopulating, setAdminLevelsPopulating] =
    useState<boolean>(false);

  useEffect(() => {
    if (isCreate) {
      setViewOnlyEnabled(false);
    }
  }, [isCreate, setViewOnlyEnabled]);

  const { data: sentReferralData, isLoading: queryLoading } = useReferral({
    id: sentReferralId,
    isCreate,
  });

  const { createReferral, patchReferral, updateReferralReason } =
    useReferralMutation();

  const form = useForm<SentReferralFormData>({
    defaultValues: defaultSentReferralFormFormValues,
    resolver: zodResolver(SentReferralSchema),
  });

  const {
    control,
    formState,
    handleSubmit,
    getValues,
    reset,
    watch,
    setValue,
  } = form;

  const currentFormAssignedFocalPoint = watch('focalPoint');
  const currentFormIsMinor = watch('isMinor');
  const currentFormSelectedOrganization = watch('organizationReferredTo');
  const currentFormServiceCategory = watch('serviceCategory');
  const currentFormIsCaregiverInformed = watch('isCaregiverInformed');
  const currentFormNoTaxId = watch('noTaxId');
  const currentFormContactPreference = watch('contactPreference');
  const currentFormCaregiverContactPreference = watch(
    'caregiverContactPreference'
  );
  const currentFormAdministrativeRegion1 = watch('administrativeRegion1');
  const currentFormAdministrativeRegion2 = watch('administrativeRegion2');
  const currentFormAdministrativeRegion3 = watch('administrativeRegion3');
  const currentFormAdministrativeRegion4 = watch('administrativeRegion4');

  useEffect(() => {
    if (sentReferralData) {
      reset(dataToSentReferralFormData(sentReferralData));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [sentReferralData]);

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

  const onSubmit = async ({
    values,
    isDraft,
  }: {
    values: SentReferralFormData;
    isDraft?: boolean;
  }) => {
    if (isCreate) {
      try {
        await createReferral.mutateAsync({ ...values, isDraft: !!isDraft });
        toast({
          title: 'Success!',
          variant: 'default',
          description: 'Referral successfully created.',
        });
        navigate(APP_ROUTE.SentReferrals);
      } catch (error: any) {
        toast({
          title: 'Something went wrong!',
          variant: 'destructive',
          description:
            error.response?.data?.errorMessage || 'Error creating referral.',
        });
      }
      return;
    }

    const changedFields: Partial<any> = Object.keys(
      formState.dirtyFields
    ).reduce((acc: Partial<any>, key: string) => {
      const value = values[key as keyof SentReferralFormData];
      if (value !== null && value !== undefined) {
        acc[key as keyof SentReferralFormData] = value;
      }
      return acc;
    }, {});

    try {
      await patchReferral.mutateAsync({
        data: { ...changedFields, isDraft },
        referralId: sentReferralId,
      });
      toast({
        title: 'Success!',
        variant: 'default',
        description: 'Referral successfully edited.',
      });
      window.location.reload();
    } catch (error: any) {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage || 'Error editing referral.',
      });
    }
  };

  const onWithdrawConfirmClick = async (reasonText: string) => {
    try {
      await updateReferralReason.mutateAsync({
        referralId: sentReferralId,
        referralType: 'withdraw',
        text: reasonText,
      });

      toast({
        title: 'Success!',
        variant: 'default',
        description: 'Referral successfully withdrawn.',
      });
      navigate(APP_ROUTE.SentReferrals);
    } catch (error: any) {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage || 'Error withdrawing referral.',
      });
    }
    setOpenStatusReasonModal(false);
  };

  const onWithdrawActionClick = () => {
    setOpenStatusReasonModal(true);
  };

  const onCancelWithdrawActionClick = () => {
    setOpenStatusReasonModal(false);
  };

  const propagateParentRegion = async (adminLevel: AdministrativeRegion) => {
    if (!adminLevel.parentId) return;
    if (adminLevel.level === 1 || adminLevel.level > 4) return;
    if (
      getValues(
        `administrativeRegion${adminLevel.level - 1}.id` as keyof SentReferralFormData
      ) === adminLevel.parentId
    )
      return;
    setAdminLevelsPopulating(true);

    const parentEntity = await getAdministrativeRegionById(adminLevel.parentId);

    setValue(
      `administrativeRegion${parentEntity.level}` as keyof SentReferralFormData,
      parentEntity
    );

    setAdminLevelsPopulating(false);
    propagateParentRegion(parentEntity);
  };

  const headerNodeButtons = () => {
    if (
      sentReferralData?.status !== ReferralStatus.Delivered &&
      !isCreate &&
      !sentReferralData?.isDraft
    ) {
      if (viewOnlyEnabled) {
        return (
          <div className="flex sm:flex-row flex-col gap-2 sm:gap-4">
            <Button type="submit" onClick={() => setViewOnlyEnabled(false)}>
              Edit
            </Button>
            {!sentReferralData?.isRejected && (
              <Button
                variant="destructive"
                onClick={onWithdrawActionClick}
                isLoading={patchReferral.isLoading}
                disabled={formState.isSubmitting || patchReferral.isLoading}
              >
                Withdraw referral
              </Button>
            )}
          </div>
        );
      }

      return (
        <div className="flex sm:flex-row flex-col gap-2 sm:gap-4">
          <Button
            type="submit"
            onClick={handleSubmit((values) =>
              onSubmit({ values, isDraft: undefined })
            )}
            isLoading={patchReferral.isLoading}
            disabled={
              formState.isSubmitting ||
              patchReferral.isLoading ||
              !formState.isDirty
            }
          >
            Submit
          </Button>
          <Button
            variant="destructive"
            onClick={() => window.location.reload()}
          >
            Cancel edits
          </Button>
        </div>
      );
    }
  };

  return (
    <PageContainer
      pageTitle={
        isCreate
          ? 'Make Referral'
          : `Case  ${sentReferralData?.caseNumber ?? '-'}`
      }
      isLoading={queryLoading && !isCreate}
      breadcrumbs={[
        { href: `${APP_ROUTE.SentReferrals}`, name: 'Sent Referrals' },
        {
          name: isCreate
            ? 'New case'
            : `Case ${sentReferralData?.caseNumber ?? '-'}`,
        },
      ]}
      headerNode={headerNodeButtons()}
    >
      {!isCreate && (
        <Tabs defaultValue="referral">
          <TabsList>
            <TabsTrigger
              value="referral"
              onClick={() => setActiveTab(ReferralTab.Referral)}
            >
              Referral
            </TabsTrigger>
            <TabsTrigger
              value="discussion"
              onClick={() => {
                setActiveTab(ReferralTab.Discussion);
              }}
            >
              Discussion
            </TabsTrigger>
          </TabsList>
        </Tabs>
      )}

      {activeTab === ReferralTab.Discussion && (
        <ReferralDiscussions referralId={sentReferralId} />
      )}

      {sentReferralData &&
        viewOnlyEnabled &&
        activeTab === ReferralTab.Referral && (
          <SentReferralPageViewOnly sentReferralData={sentReferralData} />
        )}

      {!viewOnlyEnabled && activeTab === ReferralTab.Referral && (
        <Form {...form}>
          <form onSubmit={handleSubmit((values) => onSubmit({ values }))}>
            <div className="space-y-8 max-w-2xl">
              <Card className="sm:bg-secondary/10 border-0 sm:border sm:dark:bg-secondary/10 shadow-none">
                {!isCreate &&
                  (sentReferralData?.isDraft ? (
                    <div className="w-full text-center bg-primary text-white rounded-tl-lg rounded-tr-lg text-xs leading-6 font-semibold">
                      Draft Referral
                    </div>
                  ) : (
                    <>
                      <div className="px-6 pt-2 pb-6 flex items-center justify-center">
                        <StatusTimeline
                          currentStatus={
                            sentReferralData?.status ??
                            ReferralStatus.UnderReview
                          }
                          isRejected={sentReferralData?.isRejected ?? false}
                        />
                      </div>
                      <Separator />
                    </>
                  ))}

                <CardContent className="mt-6">
                  <div className="grid grid-cols-1 gap-4">
                    <FormField
                      control={control}
                      name="isUrgent"
                      render={({ field }) => (
                        <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm">
                          <div className="space-y-0.5">
                            <FormLabel>Urgent Referral</FormLabel>
                            <FormDescription>
                              Mark referral as urgent (within 24 hours)
                            </FormDescription>
                          </div>
                          <FormControl>
                            <Switch
                              checked={field.value}
                              onCheckedChange={field.onChange}
                              disabled={viewOnlyEnabled}
                            />
                          </FormControl>
                        </FormItem>
                      )}
                    />
                  </div>
                </CardContent>

                <Separator />

                <CardHeader>
                  <CardTitle>Recipient Organization</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-1 gap-4">
                    {!isCreate && !sentReferralData?.isDraft && (
                      <div className="sm:col-span-1">
                        <dt className="text-sm font-bold tracking-tight leading-6">
                          Focal point
                        </dt>
                        <dd className="mt-1 text-sm leading sm:mt-2">
                          {currentFormAssignedFocalPoint ? (
                            `${currentFormAssignedFocalPoint?.firstName ?? ''} ${currentFormAssignedFocalPoint.lastName ?? ''}`
                          ) : (
                            <p className="italic text-primary/80">Unassigned</p>
                          )}
                        </dd>
                      </div>
                    )}
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
                        disabled={viewOnlyEnabled}
                      />
                      <FormField
                        control={control}
                        name="fundingSource"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel requiredField>Funding Source</FormLabel>
                            <Select
                              onValueChange={field.onChange}
                              value={field.value}
                            >
                              <FormControl>
                                <SelectTrigger disabled={viewOnlyEnabled}>
                                  <SelectValue placeholder="Select funding source" />
                                </SelectTrigger>
                              </FormControl>
                              <SelectContent>
                                {deploymentSettings?.fundingSources &&
                                deploymentSettings.fundingSources.length > 0 ? (
                                  deploymentSettings?.fundingSources.map(
                                    (fSource) => (
                                      <SelectItem key={fSource} value={fSource}>
                                        {fSource}
                                      </SelectItem>
                                    )
                                  )
                                ) : (
                                  <p>No funding sources available...</p>
                                )}
                              </SelectContent>
                            </Select>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                    </div>
                    <div className="grid sm:grid-cols-2 gap-4">
                      {currentFormSelectedOrganization &&
                        (currentFormSelectedOrganization.isMpcaActive ||
                          currentFormSelectedOrganization.isShelterActive ||
                          currentFormSelectedOrganization.isWashActive ||
                          currentFormSelectedOrganization.isFoodAssistanceActive ||
                          currentFormSelectedOrganization.isProtectionActive ||
                          currentFormSelectedOrganization.isIncentivePaymentsActive ||
                          currentFormSelectedOrganization.isLivelihoodsActive) && (
                          <FormField
                            control={control}
                            name="serviceCategory"
                            render={({ field }) => (
                              <FormItem>
                                <FormLabel requiredField>
                                  Service category
                                </FormLabel>
                                <Select
                                  onValueChange={(value) => {
                                    setValue('subactivities', []);
                                    setValue('subactivitiesIds', []);
                                    field.onChange(value);
                                  }}
                                  value={field.value}
                                >
                                  <FormControl>
                                    <SelectTrigger disabled={viewOnlyEnabled}>
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
                        )}
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
                                            disabled={viewOnlyEnabled}
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
                  {currentFormServiceCategory === 'mpca' && (
                    <div>
                      <Separator className="my-4" />
                      <MpcaSpecificForm
                        control={control}
                        disabled={viewOnlyEnabled}
                      />
                    </div>
                  )}
                </CardContent>
                <Separator />
                <CardHeader>
                  <CardTitle>Beneficiary Details</CardTitle>
                </CardHeader>
                <CardContent className="space-y-6">
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
                              required
                              className=""
                              placeholder="First name"
                              {...field}
                              disabled={viewOnlyEnabled}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={control}
                      name="surname"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel requiredField>Surname</FormLabel>
                          <FormControl>
                            <Input
                              id="surname"
                              placeholder="Surname"
                              {...field}
                              disabled={viewOnlyEnabled}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={control}
                      name="patronymicName"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel requiredField>Patronymic name</FormLabel>
                          <FormControl>
                            <Input
                              id="patronymicName"
                              placeholder="Patronymic name"
                              {...field}
                              disabled={viewOnlyEnabled}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    <FormField
                      control={control}
                      name="gender"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel requiredField>Gender</FormLabel>
                          <Select
                            onValueChange={field.onChange}
                            value={field.value}
                            disabled={viewOnlyEnabled}
                          >
                            <FormControl>
                              <SelectTrigger>
                                <SelectValue placeholder="Select gender" />
                              </SelectTrigger>
                            </FormControl>
                            <SelectContent>
                              <SelectItem value="male">Male</SelectItem>
                              <SelectItem value="female">Female</SelectItem>
                              <SelectItem value="other">Other</SelectItem>
                              <SelectItem value="nonDisclosed">
                                Prefer not to say
                              </SelectItem>
                            </SelectContent>
                          </Select>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>

                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <FormField
                      control={control}
                      name="dateOfBirth"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel requiredField>Date of birth</FormLabel>
                          <FormControl>
                            <DatePicker
                              field={field}
                              btnclass="w-full"
                              disabled={viewOnlyEnabled}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>
                  <div className="flex items-center gap-4">
                    <div className="w-full">
                      <FormField
                        control={control}
                        name="taxId"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel requiredField={!currentFormNoTaxId}>
                              Tax ID
                            </FormLabel>
                            <FormControl>
                              <Input
                                disabled={currentFormNoTaxId || viewOnlyEnabled}
                                id="taxId"
                                placeholder="Tax ID"
                                {...field}
                              />
                            </FormControl>
                            <FormDescription className="line-clamp-1 overflow-visible">
                              Note: It is not possible to do MPCA referrals
                              without Tax ID
                            </FormDescription>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                    </div>
                    <FormField
                      control={form.control}
                      name="noTaxId"
                      render={({ field }) => (
                        <FormItem className="flex flex-row items-center space-x-2 space-y-0">
                          <FormControl>
                            <Checkbox
                              disabled={
                                viewOnlyEnabled ||
                                currentFormServiceCategory === OrgActivity.Mpca
                              }
                              checked={field.value}
                              onCheckedChange={(value) => {
                                if (value) {
                                  setValue('taxId', '');
                                }
                                field.onChange(value);
                              }}
                            />
                          </FormControl>
                          <FormLabel className="font-normal text-sm whitespace-nowrap">
                            No Tax ID
                          </FormLabel>
                        </FormItem>
                      )}
                    />
                  </div>

                  <Separator />

                  <FormField
                    control={control}
                    name="address"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Address</FormLabel>
                        <FormControl>
                          <Input
                            id="address"
                            placeholder="Address"
                            {...field}
                            disabled={viewOnlyEnabled}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />

                  <div className="relative grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <AsyncSelect
                      label={
                        deploymentSettings?.adminLevel1Name ?? 'Admin Level 1'
                      }
                      name="administrativeRegion1"
                      control={control}
                      useInfiniteQueryFunction={useAdminRegionsInfinite}
                      useSearchTextQueryFilter
                      queryFilters={{
                        level: '1',
                        id: currentFormAdministrativeRegion2?.parentId ?? '',
                      }}
                      labelKey="name"
                      valueKey="id"
                      disabled={
                        !!currentFormAdministrativeRegion2 ||
                        viewOnlyEnabled ||
                        adminLevelsPopulating
                      }
                    />
                    <AsyncSelect
                      label={
                        deploymentSettings?.adminLevel2Name ?? 'Admin Level 2'
                      }
                      name="administrativeRegion2"
                      control={control}
                      useInfiniteQueryFunction={useAdminRegionsInfinite}
                      useSearchTextQueryFilter
                      queryFilters={{
                        level: '2',
                        id: currentFormAdministrativeRegion3?.parentId ?? '',
                        parentId: currentFormAdministrativeRegion1?.id ?? '',
                      }}
                      labelKey="name"
                      valueKey="id"
                      onChangeCallback={propagateParentRegion}
                      disabled={
                        !!currentFormAdministrativeRegion3 ||
                        viewOnlyEnabled ||
                        adminLevelsPopulating
                      }
                    />
                    <AsyncSelect
                      label={
                        deploymentSettings?.adminLevel3Name ?? 'Admin Level 3'
                      }
                      name="administrativeRegion3"
                      control={control}
                      useInfiniteQueryFunction={useAdminRegionsInfinite}
                      useSearchTextQueryFilter
                      queryFilters={{
                        level: '3',
                        id: currentFormAdministrativeRegion4?.parentId ?? '',
                        parentId: currentFormAdministrativeRegion2?.id ?? '',
                      }}
                      labelKey="name"
                      valueKey="id"
                      onChangeCallback={propagateParentRegion}
                      disabled={
                        !!currentFormAdministrativeRegion4 ||
                        viewOnlyEnabled ||
                        adminLevelsPopulating
                      }
                    />
                    <AsyncSelect
                      label={
                        deploymentSettings?.adminLevel4Name ?? 'Admin Level 4'
                      }
                      name="administrativeRegion4"
                      control={control}
                      useInfiniteQueryFunction={useAdminRegionsInfinite}
                      useSearchTextQueryFilter
                      queryFilters={{
                        level: '4',
                        parentId: currentFormAdministrativeRegion3?.id ?? '',
                      }}
                      labelKey="name"
                      valueKey="id"
                      onChangeCallback={propagateParentRegion}
                      disabled={viewOnlyEnabled || adminLevelsPopulating}
                    />
                  </div>

                  <FormField
                    control={form.control}
                    name="contactPreference"
                    render={({ field }) => (
                      <FormItem className="space-y-3">
                        <FormLabel>Contact preference</FormLabel>
                        <FormControl>
                          <RadioGroup
                            onValueChange={field.onChange}
                            defaultValue="email"
                            value={field.value}
                            className="flex flex-col space-y-1"
                            disabled={viewOnlyEnabled}
                          >
                            <FormItem className="flex items-center space-x-3 space-y-0">
                              <FormControl>
                                <RadioGroupItem value="email" />
                              </FormControl>
                              <FormLabel className="font-normal">
                                Email
                              </FormLabel>
                            </FormItem>
                            <FormItem className="flex items-center space-x-3 space-y-0">
                              <FormControl>
                                <RadioGroupItem value="phone" />
                              </FormControl>
                              <FormLabel className="font-normal">
                                Phone
                              </FormLabel>
                            </FormItem>
                            <FormItem className="flex items-center space-x-3 space-y-0">
                              <FormControl>
                                <RadioGroupItem value="visit" />
                              </FormControl>
                              <FormLabel className="font-normal">
                                Visit
                              </FormLabel>
                            </FormItem>
                          </RadioGroup>
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <FormField
                      control={control}
                      name="email"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel
                            requiredField={
                              currentFormContactPreference === 'email'
                            }
                          >
                            Email
                          </FormLabel>
                          <FormControl>
                            <Input
                              id="email"
                              placeholder="email@example.com"
                              type="email"
                              {...field}
                              disabled={viewOnlyEnabled}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={control}
                      name="phone"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel
                            requiredField={
                              currentFormContactPreference === 'phone'
                            }
                          >
                            Phone
                          </FormLabel>
                          <FormControl>
                            <Input
                              id="phone"
                              placeholder="Phone"
                              type="tel"
                              {...field}
                              disabled={viewOnlyEnabled}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </div>

                  <FormField
                    control={control}
                    name="restrictions"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>
                          Are there any contact or referral restrictions to be
                          aware of?
                        </FormLabel>
                        <FormControl>
                          <Textarea
                            id="restrictions"
                            placeholder="Restrictions"
                            maxLength={300}
                            limitCounterEnabled
                            {...field}
                            disabled={viewOnlyEnabled}
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />

                  <Separator />

                  <FormField
                    control={control}
                    name="isMinor"
                    render={({ field }) => (
                      <FormItem className="flex dark:border-orange-400 dark:bg-card border-orange-200 bg-orange-50 flex-row items-center justify-between rounded-lg border p-3 shadow-sm">
                        <div className="space-y-0.5">
                          <FormLabel>Child under 18 years old?</FormLabel>
                          <FormDescription>
                            Enable if minor and fill out necessary information
                          </FormDescription>
                        </div>
                        <FormControl>
                          <Switch
                            disabled={viewOnlyEnabled}
                            checked={field.value}
                            onCheckedChange={(value) => {
                              setValue('isSeparated', false);
                              setValue('caregiver', '');
                              setValue('relationshipToChild', '');
                              setValue('caregiverEmail', '');
                              setValue('caregiverPhone', '');
                              setValue('needForService', '');
                              field.onChange(value);
                            }}
                          />
                        </FormControl>
                      </FormItem>
                    )}
                  />

                  {currentFormIsMinor && (
                    <MinorForm
                      control={control}
                      currentFormCaregiverContactPreference={
                        currentFormCaregiverContactPreference
                      }
                      isCaregiverInformed={currentFormIsCaregiverInformed}
                      disabled={viewOnlyEnabled}
                    />
                  )}
                </CardContent>
                <Separator />
                <CardHeader>
                  <CardDescription>
                    Describe the minimum information required by the receiving
                    agency to be able to respond to the referral. This can
                    include problem description, whether they receive other
                    assistance, number in the household, etc. For referrals to
                    GBV, CP and Protection case management, do not provide
                    details of the case or incident.
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-1 gap-4">
                    <FormField
                      control={control}
                      name="required"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel requiredField>Reason</FormLabel>
                          <FormControl>
                            <Textarea
                              id="required"
                              placeholder="Enter reason"
                              maxLength={300}
                              limitCounterEnabled
                              {...field}
                              disabled={viewOnlyEnabled}
                            />
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={control}
                      name="needForService"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Service explanation</FormLabel>
                          <FormControl>
                            <Textarea
                              disabled={viewOnlyEnabled}
                              id="required"
                              placeholder="Please explain any need for service, and any already provided"
                              maxLength={300}
                              limitCounterEnabled
                              {...field}
                            />
                          </FormControl>
                        </FormItem>
                      )}
                    />
                    <div className="flex flex-col gap-3">
                      <FormLabel>Upload additional information</FormLabel>
                      <FilesDropzone
                        control={control}
                        name="files"
                        disabled={viewOnlyEnabled}
                      />
                    </div>
                  </div>
                </CardContent>

                <Separator />

                <CardContent className="pt-6">
                  <FormField
                    control={control}
                    name="consent"
                    render={({ field }) => (
                      <>
                        <FormItem className="flex flex-row items-center justify-between rounded-lg border p-3 shadow-sm">
                          <div className="space-y-0.5">
                            <FormLabel requiredField>Consent</FormLabel>
                            <FormDescription>
                              Has the Beneficiary given you consent to share
                              their data?
                            </FormDescription>
                          </div>
                          <FormControl>
                            <Switch
                              checked={field.value}
                              onCheckedChange={field.onChange}
                              disabled={viewOnlyEnabled}
                            />
                          </FormControl>
                        </FormItem>
                        <FormMessage className="mt-1" />
                      </>
                    )}
                  />
                </CardContent>
                {(isCreate || sentReferralData?.isDraft) && (
                  <>
                    <Separator className="mb-6" />
                    <CardFooter className="gap-4 justify-between p-0 sm:px-6 sm:pb-6">
                      <div className="flex gap-4">
                        <Button
                          type="button"
                          variant="default"
                          onClick={() =>
                            onSubmit({
                              values: form.getValues(),
                              isDraft: true,
                            })
                          }
                          disabled={createReferral.isLoading}
                        >
                          Save draft
                        </Button>
                        <Button
                          type="submit"
                          variant="default"
                          onClick={handleSubmit((values) =>
                            onSubmit({ values, isDraft: false })
                          )}
                          disabled={createReferral.isLoading}
                        >
                          Send Referral
                        </Button>
                      </div>
                      <Button
                        type="button"
                        variant="outline"
                        onClick={() => navigate(APP_ROUTE.SentReferrals)}
                      >
                        Cancel
                      </Button>
                    </CardFooter>
                  </>
                )}
              </Card>
            </div>
          </form>
        </Form>
      )}

      <StatusReasonModal
        open={!!isStatusReasonModal}
        title="Withdraw Referral"
        body={`Please provide a reason for withdrawing referral "${sentReferralData?.caseNumber}".`}
        confirmButtonLoading={patchReferral.isLoading}
        actionButtonVariant="destructive"
        onCancel={onCancelWithdrawActionClick}
        onAction={onWithdrawConfirmClick}
      />
    </PageContainer>
  );
};
