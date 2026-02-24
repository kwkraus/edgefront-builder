import { redirect } from 'next/navigation'
import { getServerSession } from '@/lib/auth'
import { getSeriesById } from '@/lib/api/series'
import { getSessionsBySeries } from '@/lib/api/sessions'
import { getSeriesMetrics } from '@/lib/api/metrics'
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

  const [series, sessions, metrics] = await Promise.all([
    getSeriesById(id, session.accessToken),
    getSessionsBySeries(id, session.accessToken),
    getSeriesMetrics(id, session.accessToken),
  ])

  return (
    <SeriesDetailView
      series={series}
      sessions={sessions}
      metrics={metrics}
    />
  )
}
