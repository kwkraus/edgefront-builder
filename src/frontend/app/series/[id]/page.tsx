import { redirect } from 'next/navigation'
import { getServerSession } from '@/lib/auth'
import { getSeriesById } from '@/lib/api/series'
import { getSessionsBySeries } from '@/lib/api/sessions'
import { getSeriesMetrics } from '@/lib/api/metrics'
import { ApiError } from '@/lib/api/client'
import SeriesDetailView from '@/components/series-detail-view'

interface Props {
  params: Promise<{ id: string }>
}

export async function generateMetadata({ params }: Props) {
  const { id } = await params
  const session = await getServerSession()
  if (!session?.accessToken) return { title: 'Series — EdgeFront Builder' }
  try {
    const series = await getSeriesById(id, session.accessToken)
    return { title: `${series.title} — EdgeFront Builder` }
  } catch {
    return { title: 'Series — EdgeFront Builder' }
  }
}

export default async function SeriesDetailPage({ params }: Props) {
  const { id } = await params
  const session = await getServerSession()

  if (!session?.accessToken) {
    redirect('/api/auth/signin')
  }

  let series, sessions, metrics
  try {
    ;[series, sessions, metrics] = await Promise.all([
      getSeriesById(id, session.accessToken),
      getSessionsBySeries(id, session.accessToken),
      getSeriesMetrics(id, session.accessToken).catch((err) => {
        if (err instanceof ApiError && err.status === 404) return null
        throw err
      }),
    ])
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      redirect(`/api/auth/signin?callbackUrl=${encodeURIComponent(`/series/${id}`)}`)
    }
    throw err
  }

  return (
    <SeriesDetailView
      series={series}
      sessions={sessions}
      metrics={metrics}
    />
  )
}
