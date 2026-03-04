import { redirect } from 'next/navigation'
import Link from 'next/link'
import { getServerSession } from '@/lib/auth'
import { getSeries } from '@/lib/api/series'
import { ApiError } from '@/lib/api/client'
import { StatusBadge } from '@/components/status-badge'
import { AlertTriangle } from 'lucide-react'
import type { SeriesListItem } from '@/lib/api/types'

function SeriesTableRow({ item }: { item: SeriesListItem }) {
  return (
    <tr className="group border-b last:border-b-0 hover:bg-muted/40 transition-colors">
      <td className="px-4 py-3">
        <Link
          href={`/series/${item.seriesId}`}
          className="font-medium text-foreground hover:underline focus:outline-none focus-visible:ring-2 focus-visible:ring-ring rounded"
        >
          {item.title}
        </Link>
      </td>
      <td className="px-4 py-3">
        <StatusBadge status={item.status} />
      </td>
      <td className="px-4 py-3 tabular-nums text-right">{item.sessionCount}</td>
      <td className="px-4 py-3 tabular-nums text-right">{item.totalRegistrations}</td>
      <td className="px-4 py-3 tabular-nums text-right">{item.totalAttendees}</td>
      <td className="px-4 py-3 tabular-nums text-right">{item.uniqueAccountsInfluenced}</td>
      <td className="px-4 py-3 text-center">
        {item.hasReconcileIssues && (
          <span
            className="inline-flex items-center gap-1 rounded-full border border-amber-200 bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-700"
            title="One or more sessions have reconcile issues"
          >
            <AlertTriangle className="size-3" aria-hidden="true" />
            Issues
          </span>
        )}
      </td>
    </tr>
  )
}

export default async function SeriesListContent() {
  const session = await getServerSession()

  if (!session?.accessToken) {
    redirect('/api/auth/signin')
  }

  let series: SeriesListItem[]
  try {
    series = await getSeries(session.accessToken)
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      redirect('/api/auth/signin?callbackUrl=%2Fseries')
    }
    throw err
  }

  if (series.length === 0) {
    return (
      <div className="rounded-lg border bg-card px-8 py-16 text-center text-muted-foreground">
        <p className="text-base">No series yet. Create your first series.</p>
      </div>
    )
  }

  return (
    <div className="rounded-lg border bg-card overflow-hidden">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b bg-muted/50 text-xs uppercase tracking-wide text-muted-foreground">
            <th className="px-4 py-3 text-left font-medium">Title</th>
            <th className="px-4 py-3 text-left font-medium">Status</th>
            <th className="px-4 py-3 text-right font-medium">Sessions</th>
            <th className="px-4 py-3 text-right font-medium">Registrations</th>
            <th className="px-4 py-3 text-right font-medium">Attendees</th>
            <th className="px-4 py-3 text-right font-medium">Accts Influenced</th>
            <th className="px-4 py-3 text-center font-medium">Reconcile</th>
          </tr>
        </thead>
        <tbody>
          {series.map((item) => (
            <SeriesTableRow key={item.seriesId} item={item} />
          ))}
        </tbody>
      </table>
    </div>
  )
}
