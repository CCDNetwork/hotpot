// Per-currency locale conventions: USD/ILS (and most others) use en-US dot
// decimals; EUR uses de-DE comma decimals; JOD carries 3 decimal places (fils).
const CURRENCY_LOCALES: Record<string, string> = {
  EUR: 'de-DE',
};

const CURRENCY_DECIMALS: Record<string, number> = {
  JOD: 3,
};

export type FormatCurrencyOptions = {
  /** Numbers only — for table cells where the column header shows the code. */
  noCode?: boolean;
  /** Override the currency's default decimal places. */
  decimals?: number;
  /** "USD 500,000.00" instead of "500,000.00 USD". */
  codeFirst?: boolean;
};

export const formatCurrency = (
  amount: number,
  code: string,
  opts: FormatCurrencyOptions = {}
): string => {
  const currency = (code || '').toUpperCase();
  const locale = CURRENCY_LOCALES[currency] ?? 'en-US';
  const decimals = opts.decimals ?? CURRENCY_DECIMALS[currency] ?? 2;

  const formatted = new Intl.NumberFormat(locale, {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(amount);

  if (opts.noCode) {
    return formatted;
  }

  return opts.codeFirst
    ? `${currency} ${formatted}`
    : `${formatted} ${currency}`;
};

export const formatCount = (value: number): string =>
  new Intl.NumberFormat('en-US').format(value);

export const formatPercent = (share: number, decimals = 1): string =>
  `${(share * 100).toFixed(decimals)}%`;

export const formatBucket = (isoDate: string): string => {
  const date = new Date(isoDate);
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
};

export const formatDate = (isoDate: string | null): string => {
  if (!isoDate) {
    return '—';
  }
  return new Date(isoDate).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
};
