import { FileSpreadsheetIcon, PaperclipIcon } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { toast } from '@/components/ui/use-toast';
import React from 'react';

interface Props {
  fileToUpload: File | undefined;
  setFileToUpload: React.Dispatch<React.SetStateAction<File | undefined>>;
}

export const FileUploadStep: React.FC<Props> = ({
  fileToUpload,
  setFileToUpload,
}) => {
  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    setFileToUpload(file);
    e.target.value = '';
  };

  const handleChooseFileClick = async () => {
    try {
      const fileInput = document.getElementById(
        'prebooking-file-input'
      ) as HTMLInputElement;
      fileInput.click();
    } catch {
      toast({
        title: 'Something went wrong!',
        variant: 'destructive',
        description: 'Please try again.',
      });
    }
  };
  return (
    <>
      <form>
        <div className="grid grid-cols-1 place-items-center mx-auto max-w-[400px] divide-y">
          <div className="pb-4 flex flex-col gap-3 text-center">
            <h1 className="font-semibold tracking-tight text-2xl">
              Welcome to the CWG pre-booking wizard.
            </h1>
            <p className="text-sm">
              Let&apos;s cross-checking if the households on your list have
              already been booked. To start, upload your data in an Excel File.
            </p>
            <p className="text-sm text-muted-foreground">
              Your spreadsheet should be in the format agreed by the CWG, a
              single page with a maximum of 4000 rows. If you do not have the
              template, you can download a copy{' '}
              <a
                href="/booking-empty-upload-template-1.xlsx"
                download
                className="text-primary font-semibold"
              >
                here
              </a>
              .
            </p>
          </div>
          <div className="pt-4 w-full">
            {fileToUpload ? (
              <div className="flex flex-col items-center justify-center border bg-muted py-3.5 rounded-lg transition-all duration-150 animate-appear">
                <p className="text-xs text-muted-foreground font-medium">
                  Selected file
                </p>
                <div className="flex gap-2 items-center">
                  <FileSpreadsheetIcon className="size-5" />
                  <p className="font-medium">{fileToUpload.name}</p>
                </div>
                <Button
                  onClick={() => setFileToUpload(undefined)}
                  type="button"
                  variant="link"
                  size="sm"
                  className="text-red-500 h-fit pt-1"
                >
                  Remove
                </Button>
              </div>
            ) : (
              <>
                <Button
                  type="button"
                  variant="destructive"
                  onClick={handleChooseFileClick}
                  className="w-full"
                >
                  <PaperclipIcon className="w-4 h-4 mr-2" />
                  Choose a file
                </Button>
              </>
            )}
          </div>
        </div>
      </form>
      <input
        type="file"
        className="hidden"
        id="prebooking-file-input"
        accept=".xlsx,.xls"
        onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
          handleFileChange(e)
        }
      />
    </>
  );
};
