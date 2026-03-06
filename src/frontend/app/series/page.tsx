import { Suspense } from 'react'
import Link from 'next/link'
import { PlusIcon } from '@primer/octicons-react'
import { LoadingSkeleton } from '@/components/loading-skeleton'
import SeriesListContent from '@/components/series-list-content'

export const metadata = {
  title: 'Series — EdgeFront Builder',
}

/**
 * Primer CSS custom-property styles for the page heading.
 * Mirrors the token scale used in metrics-panel.tsx.
 */
const headingStyle: React.CSSProperties = {
  color: 'var(--fgColor-default, var(--color-fg-default))',
  fontSize: 'var(--text-title-size-large, 1.5rem)',
  fontWeight: 'var(--base-text-weight-semibold, 600)',
  lineHeight: 'var(--text-title-lineHeight-large, 1.5)',
  letterSpacing: '-0.01em',
}

/**
 * Primary CTA link styled as a button using Primer tokens.
 * Uses accent/emphasis colours so the button follows the active theme.
 */
const primaryButtonStyle: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  gap: 'var(--base-size-8, 8px)',
  borderRadius: 'var(--borderRadius-medium, 6px)',
  backgroundColor: 'var(--bgColor-accent-emphasis, var(--color-accent-emphasis))',
  color: 'var(--fgColor-onEmphasis, var(--color-fg-on-emphasis))',
  padding: 'var(--base-size-8, 6px) var(--base-size-16, 16px)',
  fontSize: 'var(--text-body-size-medium, 0.875rem)',
  fontWeight: 'var(--base-text-weight-medium, 500)',
  textDecoration: 'none',
  transition: 'background-color 0.15s ease',
  outlineColor: 'var(--borderColor-focus, var(--color-accent-fg))',
}

export default function SeriesListPage() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 style={headingStyle}>Series</h1>
        <Link
          href="/series/new"
          style={primaryButtonStyle}
          className="focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
        >
          <PlusIcon size={16} aria-hidden="true" />
          Create Series
        </Link>
      </div>

      <Suspense fallback={<LoadingSkeleton rows={5} />}>
        <SeriesListContent />
      </Suspense>
    </div>
  )
}
