import { Loader2 } from 'lucide-react';

export const DrillLoading = () => (
  <div className="flex flex-1 items-center justify-center py-10">
    <Loader2 className="h-8 w-8 animate-spin" />
  </div>
);

export const DrillEmpty = ({ label }: { label: string }) => (
  <p className="py-10 text-center text-sm text-muted-foreground">{label}</p>
);
