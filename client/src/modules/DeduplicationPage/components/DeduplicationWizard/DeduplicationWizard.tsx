import { useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { AnimatePresence } from 'framer-motion';

import { Dialog, DialogContent } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { toast } from '@/components/ui/use-toast';
import {
  DeduplicationDataset,
  // SameOrgDedupeResponse,
  SystemOrgDedupeResponse,
  useDeduplicationMutation,
  useWizardFinishMutation,
} from '@/services/deduplication';

import { cn } from '@/helpers/utils';

import {
  DeduplicationUploadForm,
  DeduplicationUploadFormSchema,
} from './validation';
import {
  AnimationWrapper,
  DeduplicationError,
  DeduplicationWizardHeader,
  FileUploadStep,
  InternalFileDeduplicationStep,
  // OrganizationDeduplicationStep,
  SetupInformation,
  RegistryDeduplicationStep,
} from './components';
import { WIZARD_STEP } from './const';
import { useDeduplicationProvider } from '../../DeduplicationPageProvider';

interface Props {
  isOpen: boolean;
  setIsOpen: React.Dispatch<React.SetStateAction<boolean>>;
}

export const DeduplicationWizard = ({ isOpen, setIsOpen }: Props) => {
  const { deduplicationWizardError, setDeduplicationWizardError } =
    useDeduplicationProvider();
  const [currentStep, setCurrentStep] = useState<number>(
    WIZARD_STEP.FILE_UPLOAD
  );
  const [fileToUpload, setFileToUpload] = useState<File | undefined>(undefined);
  const [internalFileDedupResponse, setInternalFileDedupResponse] =
    useState<DeduplicationDataset | null>(null);
  // const [sameOrgDedupResponse, setSameOrgDedupResponse] =
  //   useState<SameOrgDedupeResponse | null>(null);
  const [systemOrgDedupResponse, setSystemOrgDedupResponse] =
    useState<SystemOrgDedupeResponse | null>(null);

  const {
    deduplicateFile,
    deduplicateSameOrganization,
    deduplicateSystemOrganizations,
    deduplicateFinish,
  } = useDeduplicationMutation();
  const wizardFinish = useWizardFinishMutation();

  const form = useForm<DeduplicationUploadForm>({
    defaultValues: {
      template: undefined,
    },
    mode: 'onChange',
    resolver: zodResolver(DeduplicationUploadFormSchema),
  });

  const { reset, watch } = form;
  const currentTemplate = watch('template');

  const onOpenChange = () => {
    const latestFileId = internalFileDedupResponse?.file?.id;
    if (latestFileId) {
      wizardFinish.mutate({ fileId: latestFileId });
    }

    setIsOpen((old) => !old);

    setTimeout(() => {
      reset();
      setFileToUpload(undefined);
      setInternalFileDedupResponse(null);
      setSystemOrgDedupResponse(null);
      setCurrentStep(WIZARD_STEP.FILE_UPLOAD);
      setDeduplicationWizardError(undefined);
    }, 300);
  };

  const handleInternalFileDeduplication = async () => {
    if (!fileToUpload) return;

    try {
      const resp = await deduplicateFile.mutateAsync({
        file: fileToUpload,
        templateId: currentTemplate.id,
      });
      setInternalFileDedupResponse(resp);
    } catch (error: any) {
      toast({
        title: 'An error has occured!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage ||
          'Something went wrong, please try again.',
      });
    }
  };

  const handleSameOrganizationDeduplication = async () => {
    try {
      await deduplicateSameOrganization.mutateAsync({
        fileId: internalFileDedupResponse?.file.id ?? '',
        templateId: currentTemplate.id,
      });
    } catch (error: any) {
      toast({
        title: 'An error has occured!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage ||
          'Something went wrong, please try again.',
      });
    }
  };

  const handleSystemOrganizationsDeduplication = async () => {
    try {
      await handleSameOrganizationDeduplication();
      const resp = await deduplicateSystemOrganizations.mutateAsync({
        fileId: internalFileDedupResponse?.file.id ?? '',
        templateId: currentTemplate.id,
      });
      setSystemOrgDedupResponse(resp);
    } catch (error: any) {
      toast({
        title: 'An error has occured!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage ||
          'Something went wrong, please try again.',
      });
    }
  };

  const handleWizardFinish = async () => {
    try {
      await deduplicateFinish.mutateAsync({
        fileId: internalFileDedupResponse?.file.id ?? '',
        templateId: currentTemplate.id,
      });
      onOpenChange();
    } catch (error: any) {
      toast({
        title: 'An error has occured!',
        variant: 'destructive',
        description:
          error.response?.data?.errorMessage ||
          'Something went wrong, please try again.',
      });
    }
  };

  const handleContinueClick = async () => {
    if (currentStep !== WIZARD_STEP.REGISTRY_DEDUPLICATION) {
      setCurrentStep((prev) => ++prev);
    }

    switch (currentStep) {
      case WIZARD_STEP.FILE_UPLOAD:
        await handleInternalFileDeduplication();
        break;
      case WIZARD_STEP.INTERNAL_FILE_DEDUPLICATION:
        //   await handleSameOrganizationDeduplication();
        //   break;
        // case WIZARD_STEP.ORGANIZATION_DEDUPLICATION:
        await handleSystemOrganizationsDeduplication();
        break;
      case WIZARD_STEP.REGISTRY_DEDUPLICATION:
        await handleWizardFinish();
        break;
    }
  };

  const isContinueButtonDisabled = useMemo(() => {
    if (
      currentStep === WIZARD_STEP.FILE_UPLOAD &&
      (!fileToUpload || !currentTemplate)
    ) {
      return true;
    }

    if (
      currentStep === WIZARD_STEP.INTERNAL_FILE_DEDUPLICATION &&
      internalFileDedupResponse?.duplicates
    ) {
      return true;
    }

    return false;
  }, [currentStep, fileToUpload, currentTemplate, internalFileDedupResponse]);

  // const sameOrgDedupUploadCount =
  //   (sameOrgDedupResponse?.totalRecords ?? 0) -
  //   (sameOrgDedupResponse?.potentialDuplicateRecords ?? 0) -
  //   (sameOrgDedupResponse?.identicalRecords ?? 0);

  const isWizardProcessing =
    deduplicateFile.isLoading ||
    deduplicateSameOrganization.isLoading ||
    deduplicateSystemOrganizations.isLoading ||
    deduplicateFinish.isLoading;

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent
        className="sm:max-w-4xl p-4"
        hideCloseButton
        disableBackdropClose
      >
        {deduplicationWizardError ? (
          <DeduplicationError
            onOpenChange={onOpenChange}
            errorMessage={deduplicationWizardError.message}
          />
        ) : (
          <>
            <div className="border rounded-lg flex flex-col">
              <DeduplicationWizardHeader currentStep={currentStep} />
              <div
                className={cn(
                  'flex flex-col items-center p-4 py-8 min-h-[410px]',
                  {
                    'pt-0': currentStep !== WIZARD_STEP.FILE_UPLOAD,
                  }
                )}
              >
                {currentStep !== WIZARD_STEP.FILE_UPLOAD && (
                  <SetupInformation
                    deduplicationFileName={fileToUpload?.name}
                    selectedTemplateName={currentTemplate?.name}
                  />
                )}

                <AnimatePresence mode="wait">
                  <div className="flex items-center h-full">
                    {currentStep === WIZARD_STEP.FILE_UPLOAD && (
                      <AnimationWrapper key={WIZARD_STEP.FILE_UPLOAD}>
                        <FileUploadStep
                          form={form}
                          fileToUpload={fileToUpload}
                          setFileToUpload={setFileToUpload}
                        />
                      </AnimationWrapper>
                    )}

                    {currentStep ===
                      WIZARD_STEP.INTERNAL_FILE_DEDUPLICATION && (
                      <AnimationWrapper
                        key={WIZARD_STEP.INTERNAL_FILE_DEDUPLICATION}
                      >
                        <InternalFileDeduplicationStep
                          isStepLoading={deduplicateFile.isLoading}
                          stepDeduplicationResponse={internalFileDedupResponse}
                        />
                      </AnimationWrapper>
                    )}

                    {/* {currentStep === WIZARD_STEP.ORGANIZATION_DEDUPLICATION && (
                      <AnimationWrapper
                        key={WIZARD_STEP.ORGANIZATION_DEDUPLICATION}
                      >
                        <OrganizationDeduplicationStep
                          isStepLoading={deduplicateSameOrganization.isLoading}
                          stepDeduplicationResponse={sameOrgDedupResponse}
                          duplicatesToUpload={sameOrgDedupUploadCount}
                        />
                      </AnimationWrapper>
                    )} */}

                    {currentStep === WIZARD_STEP.REGISTRY_DEDUPLICATION && (
                      <AnimationWrapper
                        key={WIZARD_STEP.REGISTRY_DEDUPLICATION}
                      >
                        <RegistryDeduplicationStep
                          isStepLoading={
                            deduplicateSystemOrganizations.isLoading ||
                            deduplicateSameOrganization.isLoading
                          }
                          stepDeduplicationResponse={systemOrgDedupResponse}
                        />
                      </AnimationWrapper>
                    )}
                  </div>
                </AnimatePresence>
              </div>
            </div>

            <div className="flex justify-between">
              <Button
                variant="outline"
                type="button"
                disabled={isWizardProcessing}
                onClick={onOpenChange}
              >
                Close
              </Button>
              <Button
                variant="default"
                type="button"
                isLoading={
                  currentStep === WIZARD_STEP.REGISTRY_DEDUPLICATION &&
                  deduplicateFinish.isLoading
                }
                disabled={isContinueButtonDisabled || isWizardProcessing}
                onClick={handleContinueClick}
              >
                {currentStep === WIZARD_STEP.REGISTRY_DEDUPLICATION
                  ? 'Finish'
                  : 'Continue'}
              </Button>
            </div>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
};
