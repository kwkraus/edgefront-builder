'use client'

import { Banner } from '@primer/react'
import { SyncIcon } from '@primer/octicons-react'

interface ErrorBannerProps {
  message: string
  onRetry?: () => void
  className?: string
}

export function ErrorBanner({ message, onRetry, className }: ErrorBannerProps) {
  return (
    <Banner
      variant="critical"
      title={message}
      className={className}
      primaryAction={
        onRetry ? (
          <Banner.PrimaryAction onClick={onRetry}>
            <SyncIcon size={16} />
            Retry
          </Banner.PrimaryAction>
        ) : undefined
      }
    />
  )
}
