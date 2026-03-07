import { Label, Spinner } from '@primer/react'

type ReconcileStatus = 'Synced' | 'Reconciling'

interface ReconcileBadgeProps {
  status: ReconcileStatus
  className?: string
}

const reconcileVariants: Record<ReconcileStatus, React.ComponentProps<typeof Label>['variant']> = {
  Synced: 'success',
  Reconciling: 'accent',
}

export function ReconcileBadge({ status, className }: ReconcileBadgeProps) {
  return (
    <Label variant={reconcileVariants[status]} className={className}>
      {status === 'Reconciling' && <Spinner size="small" />}
      {status}
    </Label>
  )
}
