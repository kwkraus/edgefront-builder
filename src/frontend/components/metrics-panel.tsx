import { Text } from '@primer/react'
import { cn } from '@/lib/utils'

/**
 * Primer v38 removed `Box`; card styling uses Primer CSS custom properties
 * directly so that colours, borders, and spacing follow the active theme.
 *
 * Token mapping (mirrors the old `Box` sx-scale values):
 *   borderWidth={1}  → --borderWidth-thin  (1 px)
 *   borderRadius={2} → --borderRadius-medium (6 px)
 *   borderColor="border.default" → --borderColor-default
 *   p={3}            → --base-size-16 (16 px)
 */

const cardStyle: React.CSSProperties = {
  borderWidth: 'var(--borderWidth-thin, 1px)',
  borderStyle: 'solid',
  borderColor: 'var(--borderColor-default, var(--color-border-default))',
  borderRadius: 'var(--borderRadius-medium, 6px)',
  padding: 'var(--base-size-16, 16px)',
}

const labelStyle: React.CSSProperties = {
  color: 'var(--fgColor-muted, var(--color-fg-muted))',
  textTransform: 'uppercase',
  letterSpacing: '0.05em',
}

const valueStyle: React.CSSProperties = {
  color: 'var(--fgColor-default, var(--color-fg-default))',
  fontSize: 'var(--text-title-size-medium, 1.25rem)',
  fontVariantNumeric: 'tabular-nums',
  marginTop: 'var(--base-size-4, 4px)',
}

interface MetricCardProps {
  label: string
  value: string | number
  className?: string
}

export function MetricCard({ label, value, className }: MetricCardProps) {
  return (
    <div style={cardStyle} className={className}>
      <Text as="p" size="small" weight="semibold" style={labelStyle}>
        {label}
      </Text>
      <Text as="p" weight="semibold" style={valueStyle}>
        {value}
      </Text>
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
