import { cn } from '@/lib/utils'

interface MetricCardProps {
  label: string
  value: string | number
  className?: string
}

export function MetricCard({ label, value, className }: MetricCardProps) {
  return (
    <div
      className={cn(
        'rounded-lg border bg-card p-4',
        className,
      )}
    >
      <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
        {label}
      </p>
      <p className="mt-1 text-2xl font-semibold tabular-nums">{value}</p>
    </div>
  )
}

interface MetricsPanelProps {
  metrics: { label: string; value: string | number }[]
  className?: string
}

export function MetricsPanel({ metrics, className }: MetricsPanelProps) {
  return (
    <div className={cn('grid grid-cols-2 gap-3 sm:grid-cols-4', className)}>
      {metrics.map((m) => (
        <MetricCard key={m.label} label={m.label} value={m.value} />
      ))}
    </div>
  )
}
