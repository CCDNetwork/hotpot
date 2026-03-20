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
        Your file and its contents are in the right format. Now we&apos;ll check
        your file against the registry, and let you know if any of your
        households have been booked by another organisation.
      </p>
    </div>
  ) : (
    <div className="flex flex-col items-center justify-center gap-2">
      <AlertTriangle className="w-16 h-16 text-yellow-500" />
      <p className="text-sm">
        Some records in your file contain formatting issues or incorrect values.
        Please download the file, review the highlighted errors, make the
        necessary corrections, and upload the updated file again.
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
