import { formatCurrency } from '../../helpers';
import { MissingRateBadge } from '../KpiTile';

/**
 * Renders "native (→ converted)" inline. Skips the arrow when the row is
 * already in the display currency; shows a "no rate" badge when the FX
 * lookup came back empty.
 */
export const ConvertedAmount = ({
  amount,
  currency,
  converted,
  displayCurrency,
}: {
  amount: number;
  currency: string;
  converted: number | null;
  displayCurrency: string;
}) => (
  <span className="whitespace-nowrap tabular-nums">
    {formatCurrency(amount, currency)}
    {currency.toUpperCase() !== displayCurrency.toUpperCase() && (
      <span className="text-muted-foreground">
        {' '}
        {converted != null ? (
          <>→ {formatCurrency(converted, displayCurrency)}</>
        ) : (
          <MissingRateBadge />
        )}
      </span>
    )}
  </span>
);
