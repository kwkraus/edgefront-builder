'use client'

import { useState } from 'react'
import { useRouter, useParams } from 'next/navigation'
import { useSession } from 'next-auth/react'
import Link from 'next/link'
import { ChevronLeft } from 'lucide-react'
import { ErrorBanner } from '@/components/error-banner'
import { DateTimePicker } from '@/components/date-time-picker'
import { createSession } from '@/lib/api/sessions'

export default function NewSessionPage() {
  const params = useParams()
  const seriesId = params.id as string
  const { data: authSession } = useSession()
  const router = useRouter()

  const [title, setTitle] = useState('')
  const [startsAtDate, setStartsAtDate] = useState<Date | null>(null)
  const [endsAtDate, setEndsAtDate] = useState<Date | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [touched, setTouched] = useState(false)

  const titleError = touched && !title.trim() ? 'Title is required' : null
  const endsAtError =
    touched && startsAtDate && endsAtDate && endsAtDate.getTime() <= startsAtDate.getTime()
      ? 'End time must be after start time'
      : null

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setTouched(true)
    if (!title.trim()) return
    if (startsAtDate && endsAtDate && endsAtDate.getTime() <= startsAtDate.getTime()) return

    setLoading(true)
    setError(null)
    try {
      await createSession(
        seriesId,
        {
          title: title.trim(),
          startsAt: startsAtDate ? startsAtDate.toISOString() : '',
          endsAt: endsAtDate ? endsAtDate.toISOString() : '',
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

        <div className="rounded-lg border bg-card p-6 space-y-4">
          <h2 className="text-base font-semibold">Schedule</h2>
          <DateTimePicker
            label="Start Time"
            value={startsAtDate}
            onChange={setStartsAtDate}
            disabled={loading}
          />
          <div>
            <DateTimePicker
              label="End Time"
              value={endsAtDate}
              onChange={setEndsAtDate}
              disabled={loading}
            />
            {endsAtError && (
              <p id="endsAt-error" role="alert" className="mt-1 text-xs text-destructive">
                {endsAtError}
              </p>
            )}
          </div>
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
