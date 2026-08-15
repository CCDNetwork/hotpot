import { useId } from 'react';
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ComposedChart,
  LabelList,
  Line,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';

import { cn } from '@/helpers/utils';

import { formatCount, formatPercent } from '../helpers';

/**
 * Shared frame height so skeletons, empty states and rendered charts occupy
 * identical space — switching between the three never shifts the layout.
 */
export const CHART_FRAME_CLASS = 'h-44 sm:h-52 2xl:h-60';

// Recharts' default ~1.5s sweep makes filter changes feel sluggish.
const ANIMATION_MS = 300;

export const ChartSkeleton = () => (
  <div className={cn(CHART_FRAME_CLASS, 'animate-pulse rounded bg-muted')} />
);

const EmptyState = ({ label }: { label: string }) => (
  <div
    className={cn(
      CHART_FRAME_CLASS,
      'flex items-center justify-center text-sm text-muted-foreground'
    )}
  >
    {label}
  </div>
);

/** CSS-sized frame: charts scale with the viewport instead of a fixed 160px. */
const ChartFrame = ({ children }: { children: React.ReactElement }) => (
  <div className={CHART_FRAME_CLASS}>
    <ResponsiveContainer width="100%" height="100%">
      {children}
    </ResponsiveContainer>
  </div>
);

const AXIS_STYLE = {
  fontSize: 10,
  fill: 'hsl(var(--muted-foreground))',
} as const;

export type BarPoint = {
  label: string;
  value: number;
  secondary?: string;
};

const TrendTooltip = ({
  active,
  payload,
  label,
}: {
  active?: boolean;
  payload?: { payload: BarPoint }[];
  label?: string;
}) => {
  if (!active || !payload?.length) {
    return null;
  }

  const point = payload[0].payload;

  return (
    <div className="rounded-md border bg-background px-2.5 py-1.5 text-xs shadow-md">
      <p className="font-medium">{label}</p>
      <p className="text-muted-foreground">
        {formatCount(point.value)}
        {point.secondary ? ` · ${point.secondary}` : ''}
      </p>
    </div>
  );
};

/** Vertical bar chart for time-bucketed trends. */
export const BarTrendChart = ({
  data,
  emptyLabel = 'No data in this period',
}: {
  data: BarPoint[];
  emptyLabel?: string;
}) => {
  if (!data.length) {
    return <EmptyState label={emptyLabel} />;
  }

  return (
    <ChartFrame>
      <BarChart data={data} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
        <CartesianGrid vertical={false} strokeOpacity={0.2} />
        <XAxis
          dataKey="label"
          tickLine={false}
          axisLine={false}
          tick={AXIS_STYLE}
          interval="preserveStartEnd"
          minTickGap={24}
        />
        <YAxis
          allowDecimals={false}
          width={32}
          tickLine={false}
          axisLine={false}
          tick={AXIS_STYLE}
        />
        <Tooltip
          content={<TrendTooltip />}
          cursor={{ fill: 'hsl(var(--muted))', opacity: 0.4 }}
        />
        <Bar
          dataKey="value"
          fill="hsl(var(--primary))"
          fillOpacity={0.85}
          radius={[3, 3, 0, 0]}
          maxBarSize={40}
          animationDuration={ANIMATION_MS}
        />
      </BarChart>
    </ChartFrame>
  );
};

/**
 * Vertical bar chart for categorical distributions where each bar is
 * clickable — used by the consecutive-months histogram. Differs from
 * BarTrendChart in that: (a) every label is shown (interval=0), (b) the bar
 * has a pointer cursor and fires onBarClick with the bucket label, and
 * (c) the tooltip nudges the user that the bar is clickable.
 */
export const HistogramBars = ({
  data,
  onBarClick,
  emptyLabel = 'No data',
  unitLabel = 'episode',
}: {
  data: BarPoint[];
  onBarClick?: (bucket: string) => void;
  emptyLabel?: string;
  unitLabel?: string;
}) => {
  if (!data.length) return <EmptyState label={emptyLabel} />;

  return (
    <ChartFrame>
      <BarChart data={data} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
        <CartesianGrid vertical={false} strokeOpacity={0.2} />
        <XAxis
          dataKey="label"
          tickLine={false}
          axisLine={false}
          tick={AXIS_STYLE}
          interval={0}
        />
        <YAxis
          allowDecimals={false}
          width={32}
          tickLine={false}
          axisLine={false}
          tick={AXIS_STYLE}
        />
        <Tooltip
          content={({ active, payload, label }) => {
            if (!active || !payload?.length) return null;
            const point = payload[0].payload as BarPoint;
            const count = point.value;
            return (
              <div className="rounded-md border bg-background px-2.5 py-1.5 text-xs shadow-md">
                <p className="font-medium">{label}</p>
                <p className="text-muted-foreground">
                  {formatCount(count)} {unitLabel}
                  {count === 1 ? '' : 's'}
                  {onBarClick ? ' · click to inspect' : ''}
                </p>
              </div>
            );
          }}
          cursor={{ fill: 'hsl(var(--muted))', opacity: 0.4 }}
        />
        <Bar
          dataKey="value"
          fill="hsl(var(--primary))"
          fillOpacity={0.85}
          radius={[3, 3, 0, 0]}
          maxBarSize={48}
          animationDuration={ANIMATION_MS}
          cursor={onBarClick ? 'pointer' : undefined}
          onClick={(entry: unknown) => {
            const point = entry as { label?: string } | null;
            if (onBarClick && point?.label) {
              onBarClick(point.label);
            }
          }}
        />
      </BarChart>
    </ChartFrame>
  );
};

/** Gradient area chart for time-bucketed trends. */
export const AreaTrendChart = ({
  data,
  emptyLabel = 'No data in this period',
}: {
  data: BarPoint[];
  emptyLabel?: string;
}) => {
  // useId's ":" delimiters are invalid inside url(#…) SVG references.
  const gradientId = `trend-${useId().replace(/:/g, '')}`;

  if (!data.length) {
    return <EmptyState label={emptyLabel} />;
  }

  return (
    <ChartFrame>
      <AreaChart data={data} margin={{ top: 4, right: 8, bottom: 0, left: 0 }}>
        <defs>
          <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
            <stop
              offset="0%"
              stopColor="hsl(var(--primary))"
              stopOpacity={0.25}
            />
            <stop
              offset="100%"
              stopColor="hsl(var(--primary))"
              stopOpacity={0}
            />
          </linearGradient>
        </defs>
        <CartesianGrid vertical={false} strokeOpacity={0.2} />
        <XAxis
          dataKey="label"
          tickLine={false}
          axisLine={false}
          tick={AXIS_STYLE}
          interval="preserveStartEnd"
          minTickGap={24}
        />
        <YAxis
          allowDecimals={false}
          width={32}
          tickLine={false}
          axisLine={false}
          tick={AXIS_STYLE}
        />
        <Tooltip content={<TrendTooltip />} cursor={{ strokeOpacity: 0.2 }} />
        <Area
          type="monotone"
          dataKey="value"
          stroke="hsl(var(--primary))"
          strokeWidth={2}
          fill={`url(#${gradientId})`}
          dot={{ r: 2, fill: 'hsl(var(--primary))', strokeWidth: 0 }}
          activeDot={{ r: 4 }}
          animationDuration={ANIMATION_MS}
        />
      </AreaChart>
    </ChartFrame>
  );
};

export type RatePoint = {
  label: string;
  count: number;
  checked: number;
  /** count ÷ checked; null when nothing was checked in the bucket. */
  rate: number | null;
};

const RATE_COLOR = '#f43f5e'; // rose-500

const RateTooltip = ({
  active,
  payload,
  label,
}: {
  active?: boolean;
  payload?: { payload: RatePoint }[];
  label?: string;
}) => {
  if (!active || !payload?.length) {
    return null;
  }

  const point = payload[0].payload;

  return (
    <div className="rounded-md border bg-background px-2.5 py-1.5 text-xs shadow-md">
      <p className="font-medium">{label}</p>
      <p className="text-muted-foreground">
        {formatCount(point.count)} overlaps · {formatCount(point.checked)}{' '}
        checked
      </p>
      <p style={{ color: RATE_COLOR }}>
        {point.rate != null
          ? `${formatPercent(point.rate)} overlap rate`
          : 'no records checked'}
      </p>
    </div>
  );
};

/** Bars (counts, left axis) combined with a rate line (%, right axis). */
export const RateTrendChart = ({
  data,
  emptyLabel = 'No data in this period',
}: {
  data: RatePoint[];
  emptyLabel?: string;
}) => {
  if (!data.length) {
    return <EmptyState label={emptyLabel} />;
  }

  return (
    <ChartFrame>
      <ComposedChart
        data={data}
        margin={{ top: 4, right: 0, bottom: 0, left: 0 }}
      >
        <CartesianGrid vertical={false} strokeOpacity={0.2} />
        <XAxis
          dataKey="label"
          tickLine={false}
          axisLine={false}
          tick={AXIS_STYLE}
          interval="preserveStartEnd"
          minTickGap={24}
        />
        <YAxis
          yAxisId="count"
          allowDecimals={false}
          width={32}
          tickLine={false}
          axisLine={false}
          tick={AXIS_STYLE}
        />
        <YAxis
          yAxisId="rate"
          orientation="right"
          domain={[0, 'auto']}
          width={36}
          tickLine={false}
          axisLine={false}
          tick={{ ...AXIS_STYLE, fill: RATE_COLOR }}
          tickFormatter={(value) => formatPercent(Number(value), 0)}
        />
        <Tooltip
          content={<RateTooltip />}
          cursor={{ fill: 'hsl(var(--muted))', opacity: 0.4 }}
        />
        <Bar
          yAxisId="count"
          dataKey="count"
          fill="hsl(var(--primary))"
          fillOpacity={0.85}
          radius={[3, 3, 0, 0]}
          maxBarSize={40}
          animationDuration={ANIMATION_MS}
        />
        <Line
          yAxisId="rate"
          type="monotone"
          dataKey="rate"
          stroke={RATE_COLOR}
          strokeWidth={2}
          connectNulls={false}
          dot={{ r: 2.5, fill: RATE_COLOR, strokeWidth: 0 }}
          activeDot={{ r: 4 }}
          animationDuration={ANIMATION_MS}
        />
      </ComposedChart>
    </ChartFrame>
  );
};

export type HorizontalBarPoint = {
  key: string;
  label: string;
  value: number;
  valueLabel?: string;
};

const HorizontalBarTooltip = ({
  active,
  payload,
}: {
  active?: boolean;
  payload?: { payload: HorizontalBarPoint }[];
}) => {
  if (!active || !payload?.length) {
    return null;
  }

  const point = payload[0].payload;

  return (
    <div className="rounded-md border bg-background px-2.5 py-1.5 text-xs shadow-md">
      <p className="font-medium">{point.label}</p>
      <p className="text-muted-foreground">
        {point.valueLabel ?? formatCount(point.value)}
      </p>
    </div>
  );
};

const truncateLabel = (label: string, max = 18): string =>
  label.length > max ? `${label.slice(0, max - 1)}…` : label;

/** Horizontal bar list (partners, blocking partners). */
export const HorizontalBars = ({
  data,
  emptyLabel = 'No data in this period',
}: {
  data: HorizontalBarPoint[];
  emptyLabel?: string;
}) => {
  if (!data.length) {
    return <EmptyState label={emptyLabel} />;
  }

  // Size the label gutter and value margin to the content so short labels
  // give their space back to the bars (matters most on narrow screens).
  const longestLabel = Math.max(
    ...data.map((d) => truncateLabel(d.label).length)
  );
  const labelWidth = Math.min(140, Math.max(72, longestLabel * 6.5));
  const longestValue = Math.max(
    ...data.map((d) => (d.valueLabel ?? formatCount(d.value)).length)
  );
  const valueMargin = Math.min(96, Math.max(40, longestValue * 6 + 10));

  return (
    <ResponsiveContainer width="100%" height={Math.max(data.length * 32, 96)}>
      <BarChart
        data={data}
        layout="vertical"
        margin={{ top: 0, right: valueMargin, bottom: 0, left: 0 }}
      >
        <XAxis type="number" hide />
        <YAxis
          type="category"
          dataKey="label"
          width={labelWidth}
          tickLine={false}
          axisLine={false}
          tick={{ ...AXIS_STYLE, fontSize: 11 }}
          tickFormatter={(label: string) => truncateLabel(label)}
        />
        <Tooltip
          content={<HorizontalBarTooltip />}
          cursor={{ fill: 'hsl(var(--muted))', opacity: 0.4 }}
        />
        <Bar
          dataKey="value"
          fill="hsl(var(--primary))"
          fillOpacity={0.85}
          radius={[0, 3, 3, 0]}
          maxBarSize={18}
          animationDuration={ANIMATION_MS}
        >
          <LabelList
            dataKey={(p: HorizontalBarPoint) =>
              p.valueLabel ?? formatCount(p.value)
            }
            position="right"
            fontSize={10}
            fill="hsl(var(--muted-foreground))"
          />
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
};

export type DonutSegment = {
  key: string;
  label: string;
  value: number;
  color: string;
};

const DonutTooltip = ({
  active,
  payload,
  total,
}: {
  active?: boolean;
  payload?: { payload: DonutSegment }[];
  total: number;
}) => {
  if (!active || !payload?.length) {
    return null;
  }

  const segment = payload[0].payload;

  return (
    <div className="rounded-md border bg-background px-2.5 py-1.5 text-xs shadow-md">
      <p className="font-medium">{segment.label}</p>
      <p className="text-muted-foreground">
        {formatCount(segment.value)} (
        {((segment.value / total) * 100).toFixed(1)}%)
      </p>
    </div>
  );
};

/** Donut with legend; stacks vertically below the sm breakpoint. */
export const DonutChart = ({
  segments,
  centerLabel,
  emptyLabel = 'No data in this period',
}: {
  segments: DonutSegment[];
  centerLabel?: string;
  emptyLabel?: string;
}) => {
  const total = segments.reduce((sum, s) => sum + s.value, 0);

  if (total === 0) {
    return <EmptyState label={emptyLabel} />;
  }

  return (
    <div className="flex flex-col items-center gap-4 sm:flex-row sm:gap-6">
      <div className="relative h-32 w-32 shrink-0 sm:h-36 sm:w-36">
        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <Pie
              data={segments}
              dataKey="value"
              nameKey="label"
              innerRadius="65%"
              outerRadius="100%"
              startAngle={90}
              endAngle={-270}
              strokeWidth={0}
              animationDuration={ANIMATION_MS}
            >
              {segments.map((segment) => (
                <Cell key={segment.key} fill={segment.color} />
              ))}
            </Pie>
            <Tooltip content={<DonutTooltip total={total} />} />
          </PieChart>
        </ResponsiveContainer>
        {centerLabel && (
          <div className="pointer-events-none absolute inset-0 flex items-center justify-center text-sm font-semibold">
            {centerLabel}
          </div>
        )}
      </div>
      <div className="w-full min-w-0 space-y-1.5 sm:w-auto">
        {segments.map((segment) => (
          <div
            key={segment.key}
            className="flex items-center gap-2 text-sm"
            title={`${segment.label}: ${formatCount(segment.value)}`}
          >
            <span
              className="h-2.5 w-2.5 shrink-0 rounded-full"
              style={{ backgroundColor: segment.color }}
            />
            <span className="truncate">{segment.label}</span>
            <span className="shrink-0 tabular-nums text-muted-foreground">
              {formatCount(segment.value)} (
              {((segment.value / total) * 100).toFixed(1)}%)
            </span>
          </div>
        ))}
      </div>
    </div>
  );
};

export const DONUT_COLORS = [
  '#0ea5e9', // sky-500
  '#10b981', // emerald-500
  '#f59e0b', // amber-500
  '#8b5cf6', // violet-500
  '#f43f5e', // rose-500
  '#06b6d4', // cyan-500
  '#84cc16', // lime-500
  '#d946ef', // fuchsia-500
];
