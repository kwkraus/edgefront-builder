import { cn } from '@/lib/utils'

interface LoadingSkeletonProps {
  rows?: number
  className?: string
}

function SkeletonRow() {
  return (
    <tr className="border-b last:border-b-0" aria-hidden="true">
      <td className="px-4 py-3">
        <div className="h-4 w-48 rounded bg-stone-200 animate-pulse" />
      </td>
      <td className="px-4 py-3">
        <div className="h-5 w-16 rounded-full bg-stone-200 animate-pulse" />
      </td>
      <td className="px-4 py-3">
        <div className="h-4 w-10 rounded bg-stone-200 animate-pulse" />
      </td>
      <td className="px-4 py-3">
        <div className="h-4 w-12 rounded bg-stone-200 animate-pulse" />
      </td>
      <td className="px-4 py-3">
        <div className="h-4 w-12 rounded bg-stone-200 animate-pulse" />
      </td>
    </tr>
  )
}

export function LoadingSkeleton({ rows = 4, className }: LoadingSkeletonProps) {
  return (
    <div
      className={cn('rounded-lg border bg-card overflow-hidden', className)}
      aria-label="Loading content…"
      aria-busy="true"
    >
      <table className="w-full text-sm">
        <tbody>
          {Array.from({ length: rows }).map((_, i) => (
            <SkeletonRow key={i} />
          ))}
        </tbody>
      </table>
    </div>
  )
}
