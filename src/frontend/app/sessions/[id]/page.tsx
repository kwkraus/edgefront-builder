'use client'

import { use, useState, useEffect, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import { useSession } from 'next-auth/react'
import Link from 'next/link'
import { ChevronLeft, AlertTriangle, Save, Trash2 } from 'lucide-react'
import { ErrorBanner } from '@/components/error-banner'
import { StatusBadge } from '@/components/status-badge'
import { ReconcileBadge } from '@/components/reconcile-badge'
import { MetricsPanel } from '@/components/metrics-panel'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { LoadingSkeleton } from '@/components/loading-skeleton'
import { getSessionById } from '@/lib/api/sessions'
import { updateSession, deleteSession } from '@/lib/api/sessions'
import { getSessionMetrics } from '@/lib/api/metrics'
import type { SessionResponse, SessionMetricsResponse } from '@/lib/api/types'

interface Props {
  params: Promise<{ id: string }>
}

function toDateTimeLocal(iso: string | null | undefined): string {
  if (!iso) return ''
  // Use the ISO string up to minutes for datetime-local
  return iso.slice(0, 16)
}

function fromDateTimeLocal(local: string): string {
  if (!local) return ''
  return new Date(local).toISOString()
}

function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleString(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

export default function SessionDetailPage({ params }: Props) {
  const { id } = use(params)
  const { data: authSession, status: authStatus } = useSession()
  const router = useRouter()
  const token = authSession?.accessToken ?? ''

  // ── Data loading ─────────────────────────────────────────────────────────
  const [session, setSession] = useState<SessionResponse | null>(null)
  const [metrics, setMetrics] = useState<SessionMetricsResponse | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [loadingData, setLoadingData] = useState(true)

  const loadData = useCallback(async () => {
    if (authStatus === 'loading' || !token) return
    setLoadingData(true)
    setLoadError(null)
    try {
      const [s, m] = await Promise.all([
        getSessionById(id, token),
        getSessionMetrics(id, token),
      ])
      setSession(s)
      setMetrics(m)
      // Sync form state with loaded data
      setTitle(s.title)
      setStartsAt(toDateTimeLocal(s.startsAt))
      setEndsAt(toDateTimeLocal(s.endsAt))
    } catch (err) {
      setLoadError(err instanceof Error ? err.message : 'Failed to load session')
    } finally {
      setLoadingData(false)
    }
  }, [id, token, authStatus])

  useEffect(() => {
    loadData()
  }, [loadData])

  // ── Form state ───────────────────────────────────────────────────────────
  const [title, setTitle] = useState('')
  const [startsAt, setStartsAt] = useState('')
  const [endsAt, setEndsAt] = useState('')
  const [touched, setTouched] = useState(false)

  const titleError = touched && !title.trim() ? 'Title is required' : null
  const endsAtError =
    touched && startsAt && endsAt && endsAt <= startsAt
      ? 'End time must be after start time'
      : null

  // ── Save ─────────────────────────────────────────────────────────────────
  const [saveLoading, setSaveLoading] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [teamsUpdateFailed, setTeamsUpdateFailed] = useState(false)

  async function handleSave(e: React.FormEvent) {
    e.preventDefault()
    setTouched(true)
    if (!title.trim() || (startsAt && endsAt && endsAt <= startsAt)) return

    setSaveLoading(true)
    setSaveError(null)
    setTeamsUpdateFailed(false)
    try {
      await updateSession(
        id,
        {
          title: title.trim(),
          startsAt: startsAt ? fromDateTimeLocal(startsAt) : '',
          endsAt: endsAt ? fromDateTimeLocal(endsAt) : '',
        },
        token,
      )
      router.push(`/series/${session?.seriesId}`)
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Save failed'
      if (msg.includes('TEAMS_UPDATE_FAILED')) {
        setTeamsUpdateFailed(true)
      } else {
        setSaveError(msg)
      }
      setSaveLoading(false)
    }
  }

  // ── Delete ───────────────────────────────────────────────────────────────
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [deleteLoading, setDeleteLoading] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  async function handleDelete() {
    setDeleteLoading(true)
    setDeleteError(null)
    try {
      await deleteSession(id, token)
      router.push(`/series/${session?.seriesId}`)
    } catch (err) {
      setDeleteError(err instanceof Error ? err.message : 'Failed to delete session')
      setDeleteLoading(false)
      setDeleteOpen(false)
    }
  }

  // ── Loading state ────────────────────────────────────────────────────────
  if (loadingData) {
    return (
      <div className="space-y-6">
        <div className="h-4 w-24 rounded bg-stone-200 animate-pulse" />
        <div className="h-8 w-64 rounded bg-stone-200 animate-pulse" />
        <LoadingSkeleton rows={3} />
      </div>
    )
  }

  if (loadError || !session) {
    return (
      <div className="space-y-4 py-8">
        <ErrorBanner
          message={loadError ?? 'Session not found'}
          onRetry={loadData}
        />
      </div>
    )
  }

  const isPublished = session.status === 'Published'
  const saveLabel = isPublished ? 'Save & Publish to Teams' : 'Save'

  return (
    <div className="max-w-2xl space-y-6">
      {/* ── Back link ──────────────────────────────────────────────────────── */}
      <Link
        href={`/series/${session.seriesId}`}
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
      >
        <ChevronLeft className="size-4" aria-hidden="true" />
        Back to Series
      </Link>

      {/* ── Header ──────────────────────────────────────────────────────────── */}
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight">{session.title}</h1>
            <StatusBadge status={session.status} />
          </div>
          <div className="flex items-center gap-3">
            <ReconcileBadge status={session.reconcileStatus} />
            {session.teamsWebinarId && (
              <span className="text-xs text-muted-foreground">
                Teams ID: {session.teamsWebinarId}
              </span>
            )}
          </div>
        </div>
        <button
          type="button"
          onClick={() => { setDeleteError(null); setDeleteOpen(true) }}
          className="inline-flex items-center gap-2 rounded-md border border-destructive/40 px-3 py-2 text-sm text-destructive hover:bg-destructive/5"
          aria-label="Delete session"
        >
          <Trash2 className="size-4" aria-hidden="true" />
          Delete
        </button>
      </div>

      {/* ── Banners ──────────────────────────────────────────────────────────── */}
      {session.reconcileStatus === 'Disabled' && (
        <div
          role="alert"
          className="flex items-start gap-3 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"
        >
          <AlertTriangle className="mt-0.5 size-4 shrink-0" aria-hidden="true" />
          <span>Webhook disabled — manual intervention required.</span>
        </div>
      )}

      {session.driftStatus === 'DriftDetected' && (
        <div
          role="alert"
          className="rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800"
        >
          <div className="flex items-center gap-2 mb-2 font-medium">
            <AlertTriangle className="size-4 shrink-0" aria-hidden="true" />
            Drift detected — Builder values differ from Teams
          </div>
          <dl className="grid grid-cols-3 gap-x-4 gap-y-1 text-xs">
            <dt className="font-medium text-amber-700">Field</dt>
            <dt className="font-medium text-amber-700">Builder</dt>
            <dt className="font-medium text-amber-700">Stored</dt>
            <dd>Title</dd>
            <dd>{session.title}</dd>
            <dd className="text-amber-600">—</dd>
            <dd>Starts At</dd>
            <dd>{formatDateTime(session.startsAt)}</dd>
            <dd className="text-amber-600">—</dd>
            <dd>Ends At</dd>
            <dd>{formatDateTime(session.endsAt)}</dd>
            <dd className="text-amber-600">—</dd>
          </dl>
        </div>
      )}

      {deleteError && <ErrorBanner message={deleteError} />}
      {saveError && <ErrorBanner message={saveError} />}
      {teamsUpdateFailed && (
        <div
          role="alert"
          className="flex items-start justify-between gap-3 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800"
        >
          <span>Publish failed — Teams webinar could not be updated.</span>
          <button
            type="button"
            onClick={handleSave}
            className="shrink-0 font-medium underline underline-offset-2 hover:no-underline"
          >
            Retry
          </button>
        </div>
      )}

      {/* ── Edit form ───────────────────────────────────────────────────────── */}
      {saveLoading && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30" aria-live="polite" aria-busy="true">
          <div className="flex items-center gap-3 rounded-lg bg-background px-6 py-4 shadow-lg">
            <span className="size-5 rounded-full border-2 border-primary/30 border-t-primary animate-spin" aria-hidden="true" />
            <span className="text-sm font-medium">
              {isPublished ? 'Publishing to Teams…' : 'Saving…'}
            </span>
          </div>
        </div>
      )}

      <form onSubmit={handleSave} noValidate className="space-y-5 rounded-lg border bg-card p-6">
        <h2 className="text-base font-semibold">Session Details</h2>

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
            className="w-full rounded-md border px-3 py-2 text-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-ring aria-invalid:border-destructive"
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

        {session.lastSyncAt && (
          <p className="text-xs text-muted-foreground">
            Last synced: {formatDateTime(session.lastSyncAt)}
          </p>
        )}
        {session.lastError && (
          <p className="text-xs text-destructive">
            Last error: {session.lastError}
          </p>
        )}

        <div className="flex items-center gap-3 pt-1">
          <button
            type="submit"
            disabled={saveLoading}
            className="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
          >
            <Save className="size-4" aria-hidden="true" />
            {saveLabel}
          </button>
          <Link
            href={`/series/${session.seriesId}`}
            className="rounded-md border px-4 py-2 text-sm hover:bg-muted transition-colors"
          >
            Cancel
          </Link>
        </div>
      </form>

      {/* ── Session Metrics ───────────────────────────────────────────────── */}
      {metrics && (
        <section aria-label="Session metrics">
          <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
            Metrics
          </h2>
          <MetricsPanel
            metrics={[
              { label: 'Registrations', value: metrics.totalRegistrations },
              { label: 'Attendees', value: metrics.totalAttendees },
              { label: 'Registrant Domains', value: metrics.uniqueRegistrantAccountDomains },
              { label: 'Attendee Domains', value: metrics.uniqueAttendeeAccountDomains },
            ]}
          />
          {metrics.warmAccountsTriggered.length > 0 && (
            <div className="mt-3">
              <p className="mb-2 text-xs font-medium text-muted-foreground">
                Warm accounts triggered:
              </p>
              <div className="flex flex-wrap gap-2">
                {metrics.warmAccountsTriggered.map((domain) => (
                  <span
                    key={domain}
                    className="rounded-full border bg-muted px-2.5 py-0.5 text-xs"
                  >
                    {domain}
                  </span>
                ))}
              </div>
            </div>
          )}
        </section>
      )}

      {/* ── Delete Confirm ────────────────────────────────────────────────── */}
      <ConfirmDialog
        open={deleteOpen}
        title="Delete Session"
        description="This will permanently delete the session and its Teams webinar. Continue?"
        confirmLabel="Delete Session"
        dangerous
        loading={deleteLoading}
        onConfirm={handleDelete}
        onCancel={() => setDeleteOpen(false)}
      />
    </div>
  )
}
