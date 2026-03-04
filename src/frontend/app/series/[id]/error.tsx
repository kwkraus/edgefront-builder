'use client'

import { useRouter } from 'next/navigation'
import { ErrorBanner } from '@/components/error-banner'

interface SeriesDetailErrorProps {
  error: Error & { digest?: string }
  reset: () => void
}

export default function SeriesDetailError({ error, reset }: SeriesDetailErrorProps) {
  const router = useRouter()

  return (
    <div className="py-8">
      <ErrorBanner
        message={error.message || 'Failed to load series details. Please try again.'}
        onRetry={() => {
          reset()
          router.refresh()
        }}
      />
    </div>
  )
}
