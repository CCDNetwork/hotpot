import {
  AlertTriangle,
  CheckCircleIcon,
  Download,
  FileSpreadsheetIcon,
} from 'lucide-react';

import { BookingDataset } from '@/services/deduplication';
import { StepLoadingComponent } from '../../StepLoadingComponent/StepLoadingComponent';
import { Button } from '@/components/ui/button';
import { appendStringToFilename, createDownloadLink } from '@/helpers/common';

interface Props {
  isStepLoading: boolean;
  stepBookingResponse: BookingDataset | null;
}

export const RegistryBookingStep: React.FC<Props> = ({
  isStepLoading,
  stepBookingResponse,
}) => {
  if (isStepLoading) {
    return (
      <StepLoadingComponent loadingStepText="The platform is adding the uploaded data to the registry and checking for duplicates..." />
    );
  }

  const duplicateFileName = appendStringToFilename(
    stepBookingResponse?.fileId ?? '-',
    '-duplicates'
  );

  const handleDownloadDuplicatesFile = (url: string, filename: string) => {
    createDownloadLink(url, filename);
  };

  return stepBookingResponse?.isValid ? (
    <div className="flex flex-col items-center justify-center gap-4 text-sm">
      <CheckCircleIcon className="w-16 h-16 text-green-600" />
      <p className="pb-4">
        The platform has found no duplicates in the registry.
      </p>
      <p>Your booking data has been successfully added to the registry.</p>
    </div>
  ) : (
    <div className="flex flex-col text-sm items-center justify-center gap-2">
      <AlertTriangle className="w-16 h-16 text-yellow-500" />
      <p className="text-sm self-start">
        The platform has found duplicates between your upload and the registry.
      </p>
      <p className="text-sm self-start">
        To assist you, we created a version of your file with a “Duplicate”
        column. You can download it below.
      </p>

      <div className="flex flex-col gap-4">
        <div className="flex gap-2">
          <span className="px-2 py-1 rounded-md bg-muted w-full flex items-center justify-center border border-border">
            <FileSpreadsheetIcon className="w-4 h-4 mr-1.5" />
            <p className="max-w-[250px] truncate text-sm">
              {duplicateFileName}
            </p>
          </span>
          <Button
            variant="destructive"
            onClick={() =>
              handleDownloadDuplicatesFile(
                stepBookingResponse?.fileUrl ?? '',
                duplicateFileName
              )
            }
          >
            <Download className="size-5 mr-2" />
            Download
          </Button>
        </div>
        <p className="text-xs text-muted-foreground">
          For privacy reasons, the platform will not keep any information about
          the file you uploaded once you exit the wizard.
        </p>
      </div>
    </div>
  );
};
