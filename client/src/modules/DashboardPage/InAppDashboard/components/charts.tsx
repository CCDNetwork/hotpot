import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';

import { formatCount } from '../helpers';

const EmptyState = ({ label }: { label: string }) => (
  <div className="flex h-40 items-center justify-center text-sm text-muted-foreground">
    {label}
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
    <ResponsiveContainer width="100%" height={160}>
      <BarChart data={data} margin={{ top: 4, right: 4, bottom: 0, left: 0 }}>
        <CartesianGrid vertical={false} strokeOpacity={0.2} />
        <XAxis
          dataKey="label"
          tickLine={false}
          axisLine={false}
          tick={AXIS_STYLE}
          interval="preserveStartEnd"
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
        />
      </BarChart>
    </ResponsiveContainer>
  );
};

/** Line chart for time-bucketed trends. */
export const LineTrendChart = ({
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
    <ResponsiveContainer width="100%" height={160}>
      <LineChart data={data} margin={{ top: 4, right: 8, bottom: 0, left: 0 }}>
        <CartesianGrid vertical={false} strokeOpacity={0.2} />
        <XAxis
          dataKey="label"
          tickLine={false}
          axisLine={false}
          tick={AXIS_STYLE}
          interval="preserveStartEnd"
        />
        <YAxis
          allowDecimals={false}
          width={32}
          tickLine={false}
          axisLine={false}
          tick={AXIS_STYLE}
        />
        <Tooltip content={<TrendTooltip />} cursor={{ strokeOpacity: 0.2 }} />
        <Line
          type="monotone"
          dataKey="value"
          stroke="hsl(var(--primary))"
          strokeWidth={2}
          dot={{ r: 2.5, fill: 'hsl(var(--primary))', strokeWidth: 0 }}
          activeDot={{ r: 4 }}
        />
      </LineChart>
    </ResponsiveContainer>
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

  return (
    <ResponsiveContainer width="100%" height={Math.max(data.length * 34, 80)}>
      <BarChart
        data={data}
        layout="vertical"
        margin={{ top: 0, right: 40, bottom: 0, left: 0 }}
      >
        <XAxis type="number" hide />
        <YAxis
          type="category"
          dataKey="label"
          width={140}
          tickLine={false}
          axisLine={false}
          tick={{ ...AXIS_STYLE, fontSize: 11 }}
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
          label={{
            position: 'right',
            fontSize: 10,
            fill: 'hsl(var(--muted-foreground))',
            formatter: (value: unknown) => formatCount(Number(value ?? 0)),
          }}
        />
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

/** Donut with legend. */
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
    <div className="flex items-center gap-6">
      <div className="relative h-36 w-36 shrink-0">
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
              isAnimationActive={false}
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
      <div className="min-w-0 space-y-1.5">
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
