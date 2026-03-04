'use client'

import { useState } from 'react'
import { useRouter, useParams } from 'next/navigation'
import { useSession } from 'next-auth/react'
import Link from 'next/link'
import { ChevronLeft } from 'lucide-react'
import { ErrorBanner } from '@/components/error-banner'
import { createSession } from '@/lib/api/sessions'

function fromDateTimeLocal(local: string): string {
  if (!local) return ''
  return new Date(local).toISOString()
}

export default function NewSessionPage() {
  const params = useParams()
  const seriesId = params.id as string
  const { data: authSession } = useSession()
  const router = useRouter()

  const [title, setTitle] = useState('')
  const [startsAt, setStartsAt] = useState('')
  const [endsAt, setEndsAt] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [touched, setTouched] = useState(false)

  const titleError = touched && !title.trim() ? 'Title is required' : null
  const endsAtError =
    touched && startsAt && endsAt && endsAt <= startsAt
      ? 'End time must be after start time'
      : null

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setTouched(true)
    if (!title.trim() || (startsAt && endsAt && endsAt <= startsAt)) return

    setLoading(true)
    setError(null)
    try {
      await createSession(
        seriesId,
        {
          title: title.trim(),
          startsAt: startsAt ? fromDateTimeLocal(startsAt) : '',
          endsAt: endsAt ? fromDateTimeLocal(endsAt) : '',
        },
        authSession?.accessToken ?? '',
      )
      router.push(`/series/${seriesId}`)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create session')
      setLoading(false)
    }
  }

  return (
    <div className="max-w-lg space-y-6">
      <div className="flex items-center gap-2">
        <Link
          href={`/series/${seriesId}`}
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
        >
          <ChevronLeft className="size-4" aria-hidden="true" />
          Back to Series
        </Link>
      </div>

      <h1 className="text-2xl font-bold tracking-tight">Add Session</h1>

      {error && <ErrorBanner message={error} />}

      {/* Loading overlay */}
      {loading && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" aria-live="polite" aria-busy="true">
          <div className="flex items-center gap-3 rounded-lg bg-background px-6 py-4 shadow-lg">
            <span className="size-5 rounded-full border-2 border-primary/30 border-t-primary animate-spin" aria-hidden="true" />
            <span className="text-sm font-medium">Creating session…</span>
          </div>
        </div>
      )}

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
            placeholder="e.g. Intro to EdgeFront"
            className="w-full rounded-md border px-3 py-2 text-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-ring aria-invalid:border-destructive"
            autoFocus
          />
          {titleError && (
            <p id="title-error" role="alert" className="mt-1 text-xs text-destructive">
              {titleError}
            </p>
          )}
        </div>

        <div>
          <label htmlFor="startsAt" className="block text-sm font-medium mb-1.5">
            Starts At
          </label>
          <input
            id="startsAt"
            type="datetime-local"
            value={startsAt}
            onChange={(e) => setStartsAt(e.target.value)}
            className="w-full rounded-md border px-3 py-2 text-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          />
        </div>

        <div>
          <label htmlFor="endsAt" className="block text-sm font-medium mb-1.5">
            Ends At
          </label>
          <input
            id="endsAt"
            type="datetime-local"
            value={endsAt}
            onChange={(e) => setEndsAt(e.target.value)}
            aria-invalid={endsAtError ? 'true' : undefined}
            aria-describedby={endsAtError ? 'endsAt-error' : undefined}
            className="w-full rounded-md border px-3 py-2 text-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-ring aria-invalid:border-destructive"
          />
          {endsAtError && (
            <p id="endsAt-error" role="alert" className="mt-1 text-xs text-destructive">
              {endsAtError}
            </p>
          )}
        </div>

        <div className="flex items-center gap-3 pt-2">
          <button
            type="submit"
            disabled={loading}
            className="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            {loading ? 'Saving…' : 'Save'}
          </button>
          <Link
            href={`/series/${seriesId}`}
            className="rounded-md border px-4 py-2 text-sm hover:bg-muted transition-colors"
          >
            Cancel
          </Link>
        </div>
      </form>
    </div>
  )
}
