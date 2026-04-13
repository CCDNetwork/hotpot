import { useMemo, useState } from 'react';
import { AnimatePresence } from 'framer-motion';

import { Dialog, DialogContent } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';

import {
  BookingDataset,
  usePrebookingMutation,
  useWizardFinishMutation,
} from '@/services/deduplication';

import { cn } from '@/helpers/utils';

import {
  AnimationWrapper,
  PrebookingWizardHeader,
  FileUploadStep,
  InternalFileStep,
  SetupInformation,
  PrebookingError,
  RegistryCheckStep,
} from './components';
import { WIZARD_STEP } from './const';
import { usePrebookingProvider } from '../../PrebookingPageProvider';

interface Props {
  isOpen: boolean;
  setIsOpen: React.Dispatch<React.SetStateAction<boolean>>;
}

export const PrebookingWizard = ({ isOpen, setIsOpen }: Props) => {
  const { prebookingWizardError, setPrebookingWizardError } =
    usePrebookingProvider();
  const [currentStep, setCurrentStep] = useState<number>(
    WIZARD_STEP.BOOKING_STEP_1
  );
  const [fileToUpload, setFileToUpload] = useState<File | undefined>(undefined);
  const [step1BookingResponse, setStep1BookingResponse] =
    useState<BookingDataset | null>(null);
  const [step2BookingResponse, setStep2BookingResponse] =
    useState<BookingDataset | null>(null);

  const { bookingStep1, bookingStep2 } = usePrebookingMutation(
    setPrebookingWizardError
  );
  const wizardFinish = useWizardFinishMutation();

  const onOpenChange = () => {
    const latestFileId =
      step2BookingResponse?.fileId || step1BookingResponse?.fileId;
    if (latestFileId) {
      wizardFinish.mutate({ fileId: latestFileId });
    }

    setIsOpen((old) => !old);

    setTimeout(() => {
      setFileToUpload(undefined);
      setStep1BookingResponse(null);
      setStep2BookingResponse(null);
      setCurrentStep(WIZARD_STEP.BOOKING_STEP_1);
      setPrebookingWizardError(undefined);
    }, 300);
  };

  const handleStep1Booking = async () => {
    if (!fileToUpload) return;

    try {
      const resp = await bookingStep1.mutateAsync({
        file: fileToUpload,
      });
      setStep1BookingResponse(resp);
    } catch (error: any) {
      setPrebookingWizardError(error);
    }
  };

  const handleStep2Booking = async () => {
    try {
      const resp = await bookingStep2.mutateAsync({
        fileId: step1BookingResponse?.fileId ?? '',
      });
      setStep2BookingResponse(resp);
    } catch (error: any) {
      setPrebookingWizardError(error);
    }
  };

  const handleWizardFinish = async () => {
    onOpenChange();
  };

  const handleContinueClick = async () => {
    if (currentStep !== WIZARD_STEP.BOOKING_STEP_3) {
      setCurrentStep((prev) => ++prev);
    }

    switch (currentStep) {
      case WIZARD_STEP.BOOKING_STEP_1:
        await handleStep1Booking();
        break;
      case WIZARD_STEP.BOOKING_STEP_2:
        await handleStep2Booking();
        break;
      case WIZARD_STEP.BOOKING_STEP_3:
        await handleWizardFinish();
        break;
    }
  };

  const isContinueButtonDisabled = useMemo(() => {
    if (currentStep === WIZARD_STEP.BOOKING_STEP_1 && !fileToUpload) {
      return true;
    }

    if (
      currentStep === WIZARD_STEP.BOOKING_STEP_2 &&
      !step1BookingResponse?.isValid
    ) {
      return true;
    }

    return false;
  }, [currentStep, fileToUpload, step1BookingResponse, step2BookingResponse]);

  const isWizardProcessing = bookingStep1.isLoading || bookingStep2.isLoading;

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent
        className="sm:max-w-4xl p-4"
        hideCloseButton
        disableBackdropClose
      >
        {prebookingWizardError ? (
          <PrebookingError
            onOpenChange={onOpenChange}
            errorMessage={
              prebookingWizardError.response?.data?.errorMessage ||
              prebookingWizardError.message
            }
          />
        ) : (
          <>
            <div className="border rounded-lg flex flex-col">
              <PrebookingWizardHeader currentStep={currentStep} />
              <div
                className={cn(
                  'flex flex-col items-center p-4 py-8 min-h-[410px]',
                  {
                    'pt-0': currentStep !== WIZARD_STEP.BOOKING_STEP_1,
                  }
                )}
              >
                {currentStep !== WIZARD_STEP.BOOKING_STEP_1 && (
                  <SetupInformation bookingFileName={fileToUpload?.name} />
                )}

                <AnimatePresence mode="wait">
                  <div className="flex items-center h-full">
                    {currentStep === WIZARD_STEP.BOOKING_STEP_1 && (
                      <AnimationWrapper key={WIZARD_STEP.BOOKING_STEP_1}>
                        <FileUploadStep
                          fileToUpload={fileToUpload}
                          setFileToUpload={setFileToUpload}
                        />
                      </AnimationWrapper>
                    )}

                    {currentStep === WIZARD_STEP.BOOKING_STEP_2 && (
                      <AnimationWrapper key={WIZARD_STEP.BOOKING_STEP_2}>
                        <InternalFileStep
                          isStepLoading={bookingStep1.isLoading}
                          stepBookingResponse={step1BookingResponse}
                        />
                      </AnimationWrapper>
                    )}

                    {currentStep === WIZARD_STEP.BOOKING_STEP_3 && (
                      <AnimationWrapper key={WIZARD_STEP.BOOKING_STEP_3}>
                        <RegistryCheckStep
                          isStepLoading={bookingStep2.isLoading}
                          stepBookingResponse={step2BookingResponse}
                          onClose={onOpenChange}
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
                  currentStep === WIZARD_STEP.BOOKING_STEP_1
                    ? bookingStep1.isLoading
                    : currentStep === WIZARD_STEP.BOOKING_STEP_2
                      ? bookingStep2.isLoading
                      : false
                }
                disabled={isContinueButtonDisabled || isWizardProcessing}
                onClick={handleContinueClick}
              >
                {currentStep === WIZARD_STEP.BOOKING_STEP_3
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
