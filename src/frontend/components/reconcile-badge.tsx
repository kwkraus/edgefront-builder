import { cn } from '@/lib/utils'

type ReconcileStatus = 'Synced' | 'Reconciling'

interface ReconcileBadgeProps {
  status: ReconcileStatus
  className?: string
}

const reconcileStyles: Record<ReconcileStatus, string> = {
  Synced: 'bg-green-50 text-green-700 border-green-200',
  Reconciling: 'bg-blue-50 text-blue-700 border-blue-200',
}

export function ReconcileBadge({ status, className }: ReconcileBadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 rounded-full border px-2.5 py-0.5 text-xs font-medium',
        reconcileStyles[status] ?? 'bg-gray-50 text-gray-700 border-gray-200',
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
