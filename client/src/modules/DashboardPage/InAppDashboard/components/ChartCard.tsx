import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';

// The shared Card primitives only apply horizontal padding from the sm:
// breakpoint up; dashboard cards need it on mobile too.
export const ChartCard = ({
  title,
  description,
  children,
}: {
  title: string;
  description?: string;
  children: React.ReactNode;
}) => (
  <Card>
    <CardHeader className="px-4 py-4 sm:px-6 sm:py-6">
      <CardTitle className="text-base">{title}</CardTitle>
      {description && <CardDescription>{description}</CardDescription>}
    </CardHeader>
    <CardContent className="px-4 pb-4 pt-0 sm:px-6 sm:pb-6">
      {children}
    </CardContent>
  </Card>
);
