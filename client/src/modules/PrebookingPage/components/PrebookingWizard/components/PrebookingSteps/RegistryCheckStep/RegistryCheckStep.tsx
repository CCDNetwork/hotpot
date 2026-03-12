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
  onClose: () => void;
}

export const RegistryCheckStep: React.FC<Props> = ({
  isStepLoading,
  stepBookingResponse,
  onClose,
}) => {
  if (isStepLoading) {
    return (
      <StepLoadingComponent loadingStepText="The platform is checking the uploaded file against the registry for households that have already been booked..." />
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
      <p>
        All of your households are available for booking now. If you wish to
        book them, please use the Booking Wizard.
      </p>
    </div>
  ) : (
    <div className="flex flex-col text-sm items-center justify-center gap-2">
      <AlertTriangle className="w-16 h-16 text-yellow-500" />
      <p className="text-sm self-start">
        The platform has found that some or all of the households in your file
        have been booked by another organisation.
      </p>
      <p className="text-sm self-start">
        You should review your list, remove those households, and apply your
        selection criteria to create a final list.
      </p>
      <p className="text-sm self-start">
        Once you have your final list, you can return to the platform and run
        the Booking Wizard to book them.
      </p>
      <p className="text-sm self-start">
        We have created a version of your list with a column which shows the
        households which have already been booked.
      </p>
      <p className="text-sm self-start">
        You can <strong>download</strong> the list below. As soon as you
        download the file, this wizard will automatically close.
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
            onClick={() => {
              handleDownloadDuplicatesFile(
                stepBookingResponse?.fileUrl ?? '',
                duplicateFileName
              );
              onClose();
            }}
          >
            <Download className="size-5 mr-2" />
            Download
          </Button>
        </div>
      </div>
    </div>
  );
};
