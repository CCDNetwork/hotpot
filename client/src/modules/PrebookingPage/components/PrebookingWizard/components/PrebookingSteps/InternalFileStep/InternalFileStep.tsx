import {
  AlertTriangle,
  CheckCircleIcon,
  Download,
  FileSpreadsheetIcon,
} from 'lucide-react';

import { Button } from '@/components/ui/button';
import { appendStringToFilename, createDownloadLink } from '@/helpers/common';
import { StepLoadingComponent } from '../../StepLoadingComponent/StepLoadingComponent';
import { BookingDataset } from '@/services/deduplication';

interface Props {
  isStepLoading: boolean;
  stepBookingResponse: BookingDataset | null;
}

export const InternalFileStep: React.FC<Props> = ({
  isStepLoading,
  stepBookingResponse,
}) => {
  const duplicateFileName = appendStringToFilename(
    stepBookingResponse?.fileId ?? '-',
    '-duplicates'
  );

  const handleDownloadDuplicatesFile = (url: string, filename: string) => {
    createDownloadLink(url, filename);
  };

  if (isStepLoading) {
    return (
      <StepLoadingComponent loadingStepText="The platform is checking the uploaded file for double entries..." />
    );
  }
  return stepBookingResponse?.isValid ? (
    <div className="flex flex-col items-center justify-center gap-4 text-sm">
      <CheckCircleIcon className="w-16 h-16 text-green-600" />
      <p>
        Your file and its contents are in the right format. Now let&apos;s check
        if your households are available for booking.
      </p>
      <p>
        We will compare your file to the registry and check if another
        organisation has already booked them.
      </p>
    </div>
  ) : (
    <div className="flex flex-col items-center justify-center gap-2">
      <AlertTriangle className="w-16 h-16 text-yellow-500" />
      <p className="text-sm">
        The platform has found duplicate records within this file. Please make
        sure there are no duplicate records and start the wizard again.
      </p>
      <p className="text-sm">
        To assist you, we created a version of your file with a
        &quot;Duplicate&quot; column. You can download it below.
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
