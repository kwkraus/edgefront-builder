import { cn } from '@/lib/utils'

interface StatusBadgeProps {
  status: string
  className?: string
}

const statusStyles: Record<string, string> = {
  Draft: 'bg-stone-100 text-stone-700 border-stone-200',
  Published: 'bg-green-50 text-green-700 border-green-200',
  'Partially Published': 'bg-amber-50 text-amber-700 border-amber-200',
  // Fallback for unknown values
  default: 'bg-gray-100 text-gray-700 border-gray-200',
}

export function StatusBadge({ status, className }: StatusBadgeProps) {
  const style = statusStyles[status] ?? statusStyles.default

  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium',
        style,
        className,
      )}
    >
      {status}
    </span>
  )
}
