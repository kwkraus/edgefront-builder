import { Suspense } from 'react'
import Link from 'next/link'
import { Plus } from 'lucide-react'
import { LoadingSkeleton } from '@/components/loading-skeleton'
import SeriesListContent from '@/components/series-list-content'

export const metadata = {
  title: 'Series — EdgeFront Builder',
}

export default function SeriesListPage() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold tracking-tight">Series</h1>
        <Link
          href="/series/new"
          className="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 transition-colors"
        >
          <Plus className="size-4" aria-hidden="true" />
          Create Series
        </Link>
      </div>

      <Suspense fallback={<LoadingSkeleton rows={5} />}>
        <SeriesListContent />
      </Suspense>
    </div>
  )
}
