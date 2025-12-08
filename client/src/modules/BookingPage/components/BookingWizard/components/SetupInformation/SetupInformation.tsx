import { FileSpreadsheet } from 'lucide-react';

interface Props {
  bookingFileName?: string;
}

export const SetupInformation: React.FC<Props> = ({ bookingFileName }) => {
  return (
    <div className="flex w-full flex-col justify-center items-center mx-auto text-sm py-4 mb-6 border-b max-w-[500px]">
      <div className="flex flex-col w-full gap-1">
        <div className="flex justify-between">
          <span>Uploaded file:</span>
          <p className="flex items-center rounded-md gap-1.5 px-2 py-0.5 bg-muted">
            <FileSpreadsheet className="size-4" />
            {bookingFileName ?? 'N/A'}
          </p>
        </div>
      </div>
    </div>
  );
};
