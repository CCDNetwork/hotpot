import { Button } from '@/components/ui/button';

export const DrillPager = ({
  page,
  totalPages,
  onChange,
}: {
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
}) => (
  <div className="flex items-center justify-between border-t pt-3">
    <Button
      variant="outline"
      size="sm"
      className="focus-visible:ring-0"
      disabled={page <= 1}
      onClick={() => onChange(page - 1)}
    >
      Previous
    </Button>
    <span className="text-sm text-muted-foreground">
      Page {page} of {totalPages}
    </span>
    <Button
      variant="outline"
      size="sm"
      className="focus-visible:ring-0"
      disabled={page >= totalPages}
      onClick={() => onChange(page + 1)}
    >
      Next
    </Button>
  </div>
);
