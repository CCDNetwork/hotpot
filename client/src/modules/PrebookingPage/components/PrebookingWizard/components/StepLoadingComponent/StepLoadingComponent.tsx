import { Loader2 } from 'lucide-react';

interface Props {
  loadingStepText: string;
}

export const StepLoadingComponent: React.FC<Props> = ({ loadingStepText }) => {
  return (
    <div className="flex flex-col items-center justify-center gap-4 max-w-[400px] mx-auto">
      <Loader2 className="w-16 h-16 animate-spin" />
      <p className="text-center text-sm">{loadingStepText}</p>
      <span className="flex flex-col gap-1">
        <p className="text-sm text-center text-red-500 font-medium">
          Warning: Do Not Refresh the Page
        </p>
        <p className="text-xs text-center text-muted-foreground">
          Please do not refresh the wizard until the check process of the file
          is complete. Refreshing the page will interrupt the process and you
          will need to start over from the beginning. Thank you for your
          patience.
        </p>
      </span>
    </div>
  );
};
