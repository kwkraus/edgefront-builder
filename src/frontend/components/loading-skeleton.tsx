'use client'

import { SkeletonBox } from '@primer/react'
import { SkeletonText } from '@primer/react/experimental'
import { cn } from '@/lib/utils'

interface LoadingSkeletonProps {
  rows?: number
  className?: string
}

function SkeletonRow() {
  return (
    <tr aria-hidden="true">
      <td className="px-4 py-3">
        <SkeletonText size="bodySmall" maxWidth={192} />
      </td>
      <td className="px-4 py-3">
        <SkeletonBox height={20} width={64} className="rounded-full" />
      </td>
      <td className="px-4 py-3">
        <SkeletonText size="bodySmall" maxWidth={40} />
      </td>
      <td className="px-4 py-3">
        <SkeletonText size="bodySmall" maxWidth={48} />
      </td>
      <td className="px-4 py-3">
        <SkeletonText size="bodySmall" maxWidth={48} />
      </td>
    </tr>
  )
}

export function LoadingSkeleton({ rows = 4, className }: LoadingSkeletonProps) {
  return (
    <div
      className={cn('rounded-lg border border-[var(--borderColor-default,var(--color-border-default))] overflow-hidden', className)}
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
