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
      <p>The platform has found no duplicate records within this file.</p>
      <p className="text-center">
        You have booked all of the households that you have uploaded for
        assistance. Good work!
      </p>
    </div>
  ) : (
    <div className="flex flex-col text-sm items-center justify-center gap-2">
      <AlertTriangle className="w-16 h-16 text-yellow-500" />
      <p className="text-sm self-start">
        The platform has found duplicate records from your uploaded file in the
        registry. Some or all of the households in your file have been booked by
        another organization.
      </p>
      <p className="text-sm self-start">
        To assist you, we have created a version of your file with an “Already
        Booked” column which shows the duplicates. You can download it below and
        update your internal records.
      </p>
      <p className="text-sm self-start">
        You have booked all the households for assistance which are not
        duplicates. Congratulations!
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
      </div>
    </div>
  );
};
