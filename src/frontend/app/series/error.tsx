'use client'

import { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { signIn } from 'next-auth/react'
import { ErrorBanner } from '@/components/error-banner'

interface SeriesErrorProps {
  error: Error & { digest?: string }
  reset: () => void
}

export default function SeriesError({ error, reset }: SeriesErrorProps) {
  const router = useRouter()

  useEffect(() => {
    if (error.message === 'UNAUTHORIZED') {
      signIn('azure-ad', { callbackUrl: window.location.href })
    }
  }, [error.message])

  if (error.message === 'UNAUTHORIZED') {
    return (
      <div className="py-8 text-sm text-muted-foreground">Redirecting to sign in…</div>
    )
  }

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
