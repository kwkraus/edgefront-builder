'use client'

import { useRouter } from 'next/navigation'
import { ErrorBanner } from '@/components/error-banner'

interface SeriesErrorProps {
  error: Error & { digest?: string }
  reset: () => void
}

export default function SeriesError({ error, reset }: SeriesErrorProps) {
  const router = useRouter()

  return (
    <div className="space-y-4 py-8">
      <ErrorBanner
        message={error.message || 'Failed to load series. Please try again.'}
        onRetry={() => {
          reset()
          router.refresh()
        }}
      />
    </div>
  )
}
