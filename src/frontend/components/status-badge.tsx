import { Label } from '@primer/react'

interface StatusBadgeProps {
  status: string
  className?: string
}

const statusVariants: Record<string, React.ComponentProps<typeof Label>['variant']> = {
  Draft: 'secondary',
  Published: 'success',
  'Partially Published': 'attention',
}

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const variant = statusVariants[status] ?? 'secondary'

  return (
    <Label variant={variant} className={className}>
      {status}
    </Label>
  )
}
