import { cn } from '@/lib/utils'

type ReconcileStatus = 'Synced' | 'Reconciling' | 'Retrying' | 'Disabled'

interface ReconcileBadgeProps {
  status: ReconcileStatus
  className?: string
}

const reconcileStyles: Record<ReconcileStatus, string> = {
  Synced: 'bg-green-50 text-green-700 border-green-200',
  Reconciling: 'bg-blue-50 text-blue-700 border-blue-200',
  Retrying: 'bg-orange-50 text-orange-700 border-orange-200',
  Disabled: 'bg-red-50 text-red-700 border-red-200',
}

export function ReconcileBadge({ status, className }: ReconcileBadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full border px-2.5 py-0.5 text-xs font-medium',
        reconcileStyles[status],
        className,
      )}
    >
      {status === 'Reconciling' && (
        <span className="size-1.5 rounded-full bg-blue-500 animate-pulse" />
      )}
      {status}
    </span>
  )
}
