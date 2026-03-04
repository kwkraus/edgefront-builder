'use client'

import { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { signIn } from 'next-auth/react'
import { ErrorBanner } from '@/components/error-banner'

interface SeriesDetailErrorProps {
  error: Error & { digest?: string }
  reset: () => void
}

export default function SeriesDetailError({ error, reset }: SeriesDetailErrorProps) {
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
