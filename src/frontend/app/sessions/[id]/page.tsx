import SessionDetailView from '@/components/session-detail-view'

interface Props {
  params: Promise<{ id: string }>
}

export const metadata = {
  title: 'Session — EdgeFront Builder',
}

export default async function SessionDetailPage({ params }: Props) {
  const { id } = await params

  return <SessionDetailView sessionId={id} />
}
