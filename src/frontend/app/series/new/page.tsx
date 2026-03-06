'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { useSession } from 'next-auth/react'
import Link from 'next/link'
import { ChevronLeftIcon } from '@primer/octicons-react'
import { Button, FormControl, TextInput, Spinner } from '@primer/react'
import { ErrorBanner } from '@/components/error-banner'
import { createSeries } from '@/lib/api/series'

export default function NewSeriesPage() {
  const { data: authSession } = useSession()
  const router = useRouter()

  const [title, setTitle] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [touched, setTouched] = useState(false)

  const titleError = touched && !title.trim() ? 'Title is required' : null

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    await submitForm()
  }

  async function submitForm() {
    setTouched(true)
    if (!title.trim()) return

    setLoading(true)
    setError(null)
    try {
      const series = await createSeries(
        { title: title.trim() },
        authSession?.accessToken ?? '',
      )
      router.push(`/series/${series.seriesId}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create series')
      setLoading(false)
    }
  }

  return (
    <div className="max-w-lg space-y-6">
      <div className="flex items-center gap-2">
        <Link
          href="/series"
          className="inline-flex items-center gap-1 text-sm transition-opacity hover:opacity-80"
          style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
        >
          <ChevronLeftIcon size={16} aria-hidden="true" />
          Back to Series
        </Link>
      </div>

      <h1 className="text-2xl font-bold tracking-tight">Create Series</h1>

      {error && <ErrorBanner message={error} onRetry={submitForm} />}

      <form onSubmit={handleSubmit} noValidate className="space-y-5">
        <FormControl required>
          <FormControl.Label>Title</FormControl.Label>
          <TextInput
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            onBlur={() => setTouched(true)}
            validationStatus={titleError ? 'error' : undefined}
            placeholder="e.g. Q1 Webinar Series"
            block
            autoFocus
          />
          {titleError && (
            <FormControl.Validation variant="error">
              {titleError}
            </FormControl.Validation>
          )}
        </FormControl>

        <div className="flex items-center gap-3 pt-2">
          <Button
            type="submit"
            variant="primary"
            disabled={loading}
            leadingVisual={loading ? () => <Spinner size="small" /> : undefined}
          >
            {loading ? 'Creating…' : 'Create'}
          </Button>
          <Button as={Link} href="/series" variant="default">
            Cancel
          </Button>
        </div>
      </form>
    </div>
  )
}
