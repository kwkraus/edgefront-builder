'use client'

import { useState } from 'react'
import { useRouter, useParams } from 'next/navigation'
import { useSession } from 'next-auth/react'
import Link from 'next/link'
import { ChevronLeftIcon } from '@primer/octicons-react'
import { Button, FormControl, TextInput, Spinner } from '@primer/react'
import { ErrorBanner } from '@/components/error-banner'
import { SessionSchedulePicker } from '@/components/session-schedule-picker'
import { createSession } from '@/lib/api/sessions'

const cardStyle: React.CSSProperties = {
  borderWidth: 'var(--borderWidth-thin, 1px)',
  borderStyle: 'solid',
  borderColor: 'var(--borderColor-default, var(--color-border-default))',
  borderRadius: 'var(--borderRadius-medium, 6px)',
  padding: 'var(--base-size-24, 24px)',
}

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
          className="inline-flex items-center gap-1 text-sm"
          style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
        >
          <ChevronLeftIcon size={16} />
          Back to Series
        </Link>
      </div>

      <h1 className="text-2xl font-bold tracking-tight">Add Session</h1>

      {error && <ErrorBanner message={error} />}

      {loading && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center"
          style={{ backgroundColor: 'var(--overlay-backdrop, rgba(0,0,0,0.3))' }}
          aria-live="polite"
          aria-busy="true"
        >
          <div
            className="flex items-center gap-3 px-6 py-4 shadow-lg"
            style={{
              backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))',
              borderRadius: 'var(--borderRadius-medium, 6px)',
            }}
          >
            <Spinner size="small" />
            <span className="text-sm font-medium">Creating session…</span>
          </div>
        </div>
      )}

      <form onSubmit={handleSubmit} noValidate className="space-y-5">
        <FormControl required>
          <FormControl.Label>Title</FormControl.Label>
          <TextInput
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            onBlur={() => setTouched(true)}
            placeholder="e.g. Intro to EdgeFront"
            block
            autoFocus
            validationStatus={titleError ? 'error' : undefined}
          />
          {titleError && (
            <FormControl.Validation variant="error">{titleError}</FormControl.Validation>
          )}
        </FormControl>

        <div className="space-y-4" style={cardStyle}>
          <h2 className="text-base font-semibold">Schedule</h2>
          <SessionSchedulePicker
            startsAt={startsAtDate}
            endsAt={endsAtDate}
            onStartsAtChange={setStartsAtDate}
            onEndsAtChange={setEndsAtDate}
            disabled={loading}
          />
          {endsAtError && (
            <FormControl.Validation variant="error">{endsAtError}</FormControl.Validation>
          )}
        </div>

        <div className="flex items-center gap-3 pt-2">
          <Button type="submit" variant="primary" disabled={loading}>
            {loading ? 'Saving…' : 'Save'}
          </Button>
          <Button as={Link} href={`/series/${seriesId}`} variant="default">
            Cancel
          </Button>
        </div>
      </form>
    </div>
  )
}
