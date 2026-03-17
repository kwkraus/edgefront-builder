'use client'

import { useCallback, useEffect, useMemo, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useSession } from 'next-auth/react'
import { ChevronLeftIcon, TrashIcon } from '@primer/octicons-react'
import { Button, FormControl, IconButton, Spinner, TextInput } from '@primer/react'
import { ErrorBanner } from '@/components/error-banner'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { SessionImportsSection } from '@/components/session-imports-section'
import { SessionMetricsSection } from '@/components/session-metrics-section'
import { SessionSchedulePicker } from '@/components/session-schedule-picker'
import { deleteSession, getSessionById, updateSession } from '@/lib/api/sessions'
import { getSessionMetrics } from '@/lib/api/metrics'
import type { SessionMetricsResponse, SessionResponse } from '@/lib/api/types'
import { formatDateTime, formatSessionSchedule, getImportSummary } from '@/lib/session-analytics'

interface SessionDetailViewProps {
  sessionId: string
}

function toDate(iso: string | null | undefined): Date | null {
  if (!iso) return null
  const date = new Date(iso)
  return Number.isNaN(date.getTime()) ? null : date
}

function SessionLoadingSkeleton() {
  return (
    <div className="space-y-6" aria-busy="true" aria-label="Loading session">
      <div className="h-4 w-28 animate-pulse rounded bg-[var(--bgColor-muted,var(--color-canvas-subtle))]" />
      <div className="h-8 w-72 animate-pulse rounded bg-[var(--bgColor-muted,var(--color-canvas-subtle))]" />
      <div className="space-y-4 rounded-lg border p-6">
        <div className="h-4 w-32 animate-pulse rounded bg-[var(--bgColor-muted,var(--color-canvas-subtle))]" />
        <div className="h-10 animate-pulse rounded bg-[var(--bgColor-muted,var(--color-canvas-subtle))]" />
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          <div className="h-40 animate-pulse rounded bg-[var(--bgColor-muted,var(--color-canvas-subtle))]" />
          <div className="h-40 animate-pulse rounded bg-[var(--bgColor-muted,var(--color-canvas-subtle))]" />
        </div>
      </div>
      <div className="grid grid-cols-1 gap-4 xl:grid-cols-3">
        {Array.from({ length: 3 }).map((_, index) => (
          <div key={index} className="h-80 animate-pulse rounded-lg border bg-[var(--bgColor-muted,var(--color-canvas-subtle))]" />
        ))}
      </div>
    </div>
  )
}

export default function SessionDetailView({ sessionId }: SessionDetailViewProps) {
  const { data: authSession, status: authStatus } = useSession()
  const accessToken = authSession?.accessToken ?? ''
  const router = useRouter()

  const [session, setSession] = useState<SessionResponse | null>(null)
  const [metrics, setMetrics] = useState<SessionMetricsResponse | null>(null)
  const [loadingData, setLoadingData] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)

  const [title, setTitle] = useState('')
  const [startsAtDate, setStartsAtDate] = useState<Date | null>(null)
  const [endsAtDate, setEndsAtDate] = useState<Date | null>(null)
  const [touched, setTouched] = useState(false)

  const [saveLoading, setSaveLoading] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [deleteLoading, setDeleteLoading] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const [statusMessage, setStatusMessage] = useState<string | null>(null)

  const syncFormState = useCallback((nextSession: SessionResponse) => {
    setTitle(nextSession.title)
    setStartsAtDate(toDate(nextSession.startsAt))
    setEndsAtDate(toDate(nextSession.endsAt))
  }, [])

  const loadData = useCallback(
    async (options?: { resetForm?: boolean }) => {
      if (authStatus === 'loading' || !accessToken) {
        return
      }

      setLoadingData(true)
      setLoadError(null)

      try {
        const [nextSession, nextMetrics] = await Promise.all([
          getSessionById(sessionId, accessToken),
          getSessionMetrics(sessionId, accessToken).catch(() => null),
        ])

        setSession(nextSession)
        setMetrics(nextMetrics ?? null)

        if (options?.resetForm !== false) {
          syncFormState(nextSession)
        }
      } catch (error) {
        setLoadError(error instanceof Error ? error.message : 'Failed to load session.')
      } finally {
        setLoadingData(false)
      }
    },
    [accessToken, authStatus, sessionId, syncFormState],
  )

  useEffect(() => {
    void loadData()
  }, [loadData])

  const titleError = touched && !title.trim() ? 'Title is required.' : null
  const endsAtError =
    touched && startsAtDate && endsAtDate && endsAtDate.getTime() <= startsAtDate.getTime()
      ? 'End time must be after the start time.'
      : null

  const busy = saveLoading || deleteLoading
  const schedule = formatSessionSchedule(session?.startsAt, session?.endsAt)
  const importCoverage = {
    registrations: Boolean(getImportSummary(session?.imports, 'registrations')),
    attendance: Boolean(getImportSummary(session?.imports, 'attendance')),
    qa: Boolean(getImportSummary(session?.imports, 'qa')),
  }

  const hasUnsavedChanges = useMemo(() => {
    if (!session) return false

    return (
      title !== session.title ||
      startsAtDate?.toISOString() !== session.startsAt ||
      endsAtDate?.toISOString() !== session.endsAt
    )
  }, [endsAtDate, session, startsAtDate, title])

  async function handleSave(event: React.FormEvent) {
    event.preventDefault()
    setTouched(true)
    setStatusMessage(null)

    if (!title.trim()) return
    if (startsAtDate && endsAtDate && endsAtDate.getTime() <= startsAtDate.getTime()) return

    setSaveLoading(true)
    setSaveError(null)

    try {
      await updateSession(
        sessionId,
        {
          title: title.trim(),
          startsAt: startsAtDate?.toISOString() ?? '',
          endsAt: endsAtDate?.toISOString() ?? '',
        },
        accessToken,
      )

      await loadData({ resetForm: true })
      setStatusMessage('Session definition updated.')
    } catch (error) {
      setSaveError(error instanceof Error ? error.message : 'Failed to save session.')
    } finally {
      setSaveLoading(false)
    }
  }

  async function handleDelete() {
    if (!session) return

    setDeleteLoading(true)
    setDeleteError(null)

    try {
      await deleteSession(sessionId, accessToken)
      router.push(`/series/${session.seriesId}`)
    } catch (error) {
      setDeleteError(error instanceof Error ? error.message : 'Failed to delete session.')
      setDeleteOpen(false)
      setDeleteLoading(false)
    }
  }

  function handleReset() {
    if (!session) return
    syncFormState(session)
    setTouched(false)
    setSaveError(null)
    setStatusMessage(null)
  }

  if ((loadingData && !session) || authStatus === 'loading') {
    return <SessionLoadingSkeleton />
  }

  if (loadError || !session) {
    return (
      <div className="space-y-4 py-8">
        <ErrorBanner message={loadError ?? 'Session not found.'} onRetry={() => void loadData()} />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <Link
        href={`/series/${session.seriesId}`}
        className="inline-flex items-center gap-1 text-sm transition-colors"
        style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
      >
        <ChevronLeftIcon size={16} aria-hidden="true" />
        Back to series
      </Link>

      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1">
          <h1 className="text-2xl font-bold tracking-tight">{session.title}</h1>
          <p
            className="text-sm"
            style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
          >
            Local session definition with session-scoped imports and analytics.
          </p>
          <p
            className="text-xs"
            style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
          >
            Scheduled {schedule.date}
            {schedule.time ? ` · ${schedule.time}` : ''}
            {schedule.duration ? ` · ${schedule.duration}` : ''}
          </p>
        </div>

        <IconButton
          icon={TrashIcon}
          aria-label="Delete session"
          variant="danger"
          onClick={() => {
            setDeleteError(null)
            setDeleteOpen(true)
          }}
          disabled={busy}
        />
      </header>

      {statusMessage && (
        <div
          role="status"
          className="rounded-lg border px-4 py-3 text-sm"
          style={{
            borderColor: 'var(--borderColor-success-emphasis, var(--color-success-emphasis))',
            backgroundColor: 'var(--bgColor-success-muted, var(--color-success-subtle))',
            color: 'var(--fgColor-success, var(--color-success-fg))',
          }}
        >
          {statusMessage}
        </div>
      )}

      {loadError && <ErrorBanner message={loadError} onRetry={() => void loadData({ resetForm: false })} />}
      {saveError && <ErrorBanner message={saveError} />}
      {deleteError && <ErrorBanner message={deleteError} />}

      <form onSubmit={handleSave} noValidate className="space-y-6">
        <section
          className="space-y-4 rounded-lg border p-6"
          style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}
        >
          <div className="space-y-1">
            <h2 className="text-base font-semibold">Session definition</h2>
            <p
              className="text-sm"
              style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
            >
              Maintain the local title and schedule used for analytics and reporting.
            </p>
          </div>

          <dl className="grid grid-cols-1 gap-3 text-sm sm:grid-cols-3">
            <div>
              <dt
                className="text-xs font-medium uppercase tracking-wide"
                style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
              >
                Registrations CSV
              </dt>
              <dd>{importCoverage.registrations ? 'Imported' : 'Pending'}</dd>
            </div>
            <div>
              <dt
                className="text-xs font-medium uppercase tracking-wide"
                style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
              >
                Attendance CSV
              </dt>
              <dd>{importCoverage.attendance ? 'Imported' : 'Pending'}</dd>
            </div>
            <div>
              <dt
                className="text-xs font-medium uppercase tracking-wide"
                style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
              >
                Q&amp;A CSV
              </dt>
              <dd>{importCoverage.qa ? 'Imported' : 'Pending'}</dd>
            </div>
          </dl>

          <FormControl required>
            <FormControl.Label>Title</FormControl.Label>
            <TextInput
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              onBlur={() => setTouched(true)}
              validationStatus={titleError ? 'error' : undefined}
              placeholder="e.g. Session 1 — Intro to EdgeFront"
              disabled={busy}
              block
            />
            {titleError && (
              <FormControl.Validation variant="error">{titleError}</FormControl.Validation>
            )}
          </FormControl>

          <div className="space-y-3">
            <h3 className="text-sm font-semibold">Schedule</h3>
            <SessionSchedulePicker
              startsAt={startsAtDate}
              endsAt={endsAtDate}
              onStartsAtChange={setStartsAtDate}
              onEndsAtChange={setEndsAtDate}
              disabled={busy}
            />
            {endsAtError && (
              <p
                role="alert"
                className="text-sm"
                style={{ color: 'var(--fgColor-danger, var(--color-danger-fg))' }}
              >
                {endsAtError}
              </p>
            )}
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <Button type="submit" variant="primary" disabled={busy || !hasUnsavedChanges}>
              {saveLoading ? (
                <span className="inline-flex items-center gap-2">
                  <Spinner size="small" />
                  Saving…
                </span>
              ) : (
                'Save changes'
              )}
            </Button>
            <Button type="button" variant="default" onClick={handleReset} disabled={busy || !hasUnsavedChanges}>
              Reset
            </Button>
            <Button as={Link} href={`/series/${session.seriesId}`} variant="default" disabled={busy}>
              Back to series
            </Button>
          </div>
        </section>
      </form>

      <SessionImportsSection
        sessionId={sessionId}
        imports={session.imports}
        accessToken={accessToken}
        onUploadComplete={async (result) => {
          setStatusMessage(
            `${result.fileName} uploaded for ${result.importType}. ${result.rowCount.toLocaleString()} rows imported on ${formatDateTime(result.importedAt)}.`,
          )
          await loadData({ resetForm: false })
        }}
      />

      <SessionMetricsSection metrics={metrics} />

      <ConfirmDialog
        open={deleteOpen}
        title="Delete session"
        description="This will permanently delete the session definition and any imported analytics attached to it. Continue?"
        confirmLabel="Delete session"
        dangerous
        loading={deleteLoading}
        onConfirm={handleDelete}
        onCancel={() => setDeleteOpen(false)}
      />
    </div>
  )
}



