'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { useSession } from 'next-auth/react'
import Link from 'next/link'
import { ChevronLeft } from 'lucide-react'
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
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ChevronLeft className="size-4" aria-hidden="true" />
          Back to Series
        </Link>
      </div>

      <h1 className="text-2xl font-bold tracking-tight">Create Series</h1>

      {error && <ErrorBanner message={error} onRetry={submitForm} />}

      <form onSubmit={handleSubmit} noValidate className="space-y-5">
        <div>
          <label htmlFor="title" className="block text-sm font-medium mb-1.5">
            Title <span className="text-destructive" aria-hidden="true">*</span>
          </label>
          <input
            id="title"
            type="text"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            onBlur={() => setTouched(true)}
            aria-invalid={titleError ? 'true' : undefined}
            aria-describedby={titleError ? 'title-error' : undefined}
            placeholder="e.g. Q1 Webinar Series"
            className="w-full rounded-md border px-3 py-2 text-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-ring aria-invalid:border-destructive"
            autoFocus
          />
          {titleError && (
            <p id="title-error" role="alert" className="mt-1 text-xs text-destructive">
              {titleError}
            </p>
          )}
        </div>

        <div className="flex items-center gap-3 pt-2">
          <button
            type="submit"
            disabled={loading}
            className="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            {loading && (
              <span
                className="size-3.5 rounded-full border-2 border-white/30 border-t-white animate-spin"
                aria-hidden="true"
              />
            )}
            {loading ? 'Creating…' : 'Create'}
          </button>
          <Link
            href="/series"
            className="rounded-md border px-4 py-2 text-sm hover:bg-muted transition-colors"
          >
            Cancel
          </Link>
        </div>
      </form>
    </div>
  )
}
