'use client'

import { useState, useEffect, useCallback } from 'react'
import { useRouter, useParams } from 'next/navigation'
import { useSession } from 'next-auth/react'
import Link from 'next/link'
import {
  ChevronLeft,
  AlertTriangle,
  Save,
  Trash2,
  RefreshCw,
  ExternalLink,
  Rocket,
  Pencil,
} from 'lucide-react'
import { ErrorBanner } from '@/components/error-banner'
import { StatusBadge } from '@/components/status-badge'
import { MetricsPanel } from '@/components/metrics-panel'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { PeoplePicker } from '@/components/people-picker'
import { DateTimePicker } from '@/components/date-time-picker'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from '@/components/ui/dialog'

import { getSessionById } from '@/lib/api/sessions'
import {
  updateSession,
  deleteSession,
  syncSession,
  publishSession,
  setSessionPresenters,
  setSessionCoordinators,
} from '@/lib/api/sessions'
import { getSessionMetrics } from '@/lib/api/metrics'
import type { SessionResponse, SessionMetricsResponse, PersonInput } from '@/lib/api/types'

function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return '—'
  return new Date(iso).toLocaleString(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

function toDate(iso: string | null | undefined): Date | null {
  if (!iso) return null
  const d = new Date(iso)
  return isNaN(d.getTime()) ? null : d
}

export default function SessionDetailPage() {
  const params = useParams()
  const id = params.id as string
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
        getSessionMetrics(id, token).catch(() => null),
      ])
      setSession(s)
      setMetrics(m ?? null)
      // Sync form state with loaded data
      setTitle(s.title)
      setStartsAtDate(toDate(s.startsAt))
      setEndsAtDate(toDate(s.endsAt))
      setPresenters(
        s.presenters.map((p) => ({
          entraUserId: p.entraUserId,
          displayName: p.displayName,
          email: p.email,
        })),
      )
      setCoordinators(
        s.coordinators.map((c) => ({
          entraUserId: c.entraUserId,
          displayName: c.displayName,
          email: c.email,
        })),
      )
    } catch (err) {
      setLoadError(err instanceof Error ? err.message : 'Failed to load session')
    } finally {
      setLoadingData(false)
    }
  }, [id, token, authStatus])

  useEffect(() => {
    loadData()
  }, [loadData])

  // ── Auto-sync published sessions from Teams (stale after 15 min) ─────────
  const [syncing, setSyncing] = useState(false)
  const [syncDone, setSyncDone] = useState(false)
  const STALE_MS = 15 * 60 * 1000 // 15 minutes

  function isSyncStale(lastSyncAt: string | null | undefined): boolean {
    if (!lastSyncAt) return true
    return Date.now() - new Date(lastSyncAt).getTime() > STALE_MS
  }

  const doManualSync = useCallback(async () => {
    if (!session || session.status !== 'Published' || !session.teamsWebinarId || !token) return
    setSyncing(true)
    try {
      await syncSession(session.sessionId, token)
      setSyncDone(true)
      loadData()
    } catch {
      // Non-blocking — user still sees cached data
    } finally {
      setSyncing(false)
    }
  }, [session, token, loadData])

  useEffect(() => {
    if (!session || session.status !== 'Published' || !session.teamsWebinarId || !token || syncDone) return
    if (!isSyncStale(session.lastSyncAt)) { setSyncDone(true); return }
    let cancelled = false
    async function doSync() {
      setSyncing(true)
      try {
        await syncSession(session!.sessionId, token)
        if (!cancelled) {
          setSyncDone(true)
          loadData()
        }
      } catch {
        // Non-blocking — user still sees cached data
      } finally {
        if (!cancelled) setSyncing(false)
      }
    }
    doSync()
    return () => { cancelled = true }
  }, [session, token, syncDone, loadData])

  // ── Form state ───────────────────────────────────────────────────────────
  const [title, setTitle] = useState('')
  const [startsAtDate, setStartsAtDate] = useState<Date | null>(null)
  const [endsAtDate, setEndsAtDate] = useState<Date | null>(null)
  const [presenters, setPresenters] = useState<PersonInput[]>([])
  const [coordinators, setCoordinators] = useState<PersonInput[]>([])
  const [touched, setTouched] = useState(false)

  const titleError = touched && !title.trim() ? 'Title is required' : null
  const endsAtError =
    touched && startsAtDate && endsAtDate && endsAtDate.getTime() <= startsAtDate.getTime()
      ? 'End time must be after start time'
      : null

  // ── Title edit dialog ────────────────────────────────────────────────────
  const [editTitleOpen, setEditTitleOpen] = useState(false)
  const [editTitleDraft, setEditTitleDraft] = useState('')

  function openTitleEdit() {
    setEditTitleDraft(title)
    setEditTitleOpen(true)
  }

  function handleTitleSave() {
    if (!editTitleDraft.trim()) return
    setTitle(editTitleDraft.trim())
    setEditTitleOpen(false)
  }

  // ── Save ─────────────────────────────────────────────────────────────────
  const [saveLoading, setSaveLoading] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [teamsUpdateFailed, setTeamsUpdateFailed] = useState(false)

  async function handleSave(e: React.SyntheticEvent) {
    e.preventDefault()
    setTouched(true)
    if (!title.trim()) return
    if (startsAtDate && endsAtDate && endsAtDate.getTime() <= startsAtDate.getTime()) return

    setSaveLoading(true)
    setSaveError(null)
    setTeamsUpdateFailed(false)
    try {
      await updateSession(
        id,
        {
          title: title.trim(),
          startsAt: startsAtDate ? startsAtDate.toISOString() : '',
          endsAt: endsAtDate ? endsAtDate.toISOString() : '',
        },
        token,
      )
      await setSessionPresenters(id, presenters, token)
      await setSessionCoordinators(id, coordinators, token)
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

  // ── Publish individual session to Teams ──────────────────────────────────
  const [publishOpen, setPublishOpen] = useState(false)
  const [publishLoading, setPublishLoading] = useState(false)
  const [publishError, setPublishError] = useState<string | null>(null)
  const [publishLicenseError, setPublishLicenseError] = useState(false)

  async function handlePublishSession() {
    setPublishLoading(true)
    setPublishError(null)
    setPublishLicenseError(false)
    try {
      await publishSession(id, token)
      setPublishOpen(false)
      setPublishLoading(false)
      loadData()
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Session publish failed'
      if (msg.includes('TEAMS_LICENSE_REQUIRED')) {
        setPublishLicenseError(true)
      } else {
        setPublishError(msg)
      }
      setPublishLoading(false)
      setPublishOpen(false)
    }
  }

  // ── Derived state ────────────────────────────────────────────────────────
  const busy = saveLoading || deleteLoading || publishLoading

  // ── Loading skeleton ─────────────────────────────────────────────────────
  if (loadingData) {
    return (
      <div className="space-y-6" aria-label="Loading session…" aria-busy="true">
        {/* Back link */}
        <div className="h-4 w-28 rounded bg-stone-200 animate-pulse" />

        {/* Header: title + pencil + status badge */}
        <div className="flex items-center gap-3">
          <div className="h-8 w-64 rounded bg-stone-200 animate-pulse" />
          <div className="h-8 w-8 rounded bg-stone-200 animate-pulse" />
          <div className="h-6 w-20 rounded-full bg-stone-200 animate-pulse" />
        </div>

        {/* Two-column grid */}
        <div className="grid grid-cols-1 lg:grid-cols-[1fr_1fr] gap-6">
          {/* Schedule card */}
          <div className="space-y-4 rounded-lg border bg-card p-6">
            <div className="h-5 w-24 rounded bg-stone-200 animate-pulse" />
            <div className="space-y-1.5">
              <div className="h-4 w-20 rounded bg-stone-200 animate-pulse" />
              <div className="h-10 w-full rounded-md bg-stone-200 animate-pulse" />
            </div>
            <div className="space-y-1.5">
              <div className="h-4 w-16 rounded bg-stone-200 animate-pulse" />
              <div className="h-10 w-full rounded-md bg-stone-200 animate-pulse" />
            </div>
          </div>

          {/* Presenters + Coordinators */}
          <div className="space-y-6">
            <div className="space-y-3 rounded-lg border bg-card p-6">
              <div className="h-5 w-28 rounded bg-stone-200 animate-pulse" />
              <div className="h-10 w-full rounded-md bg-stone-200 animate-pulse" />
            </div>
            <div className="space-y-3 rounded-lg border bg-card p-6">
              <div className="h-5 w-32 rounded bg-stone-200 animate-pulse" />
              <div className="h-10 w-full rounded-md bg-stone-200 animate-pulse" />
            </div>
          </div>
        </div>

        {/* Button row */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="h-10 w-24 rounded-md bg-stone-200 animate-pulse" />
            <div className="h-10 w-32 rounded-md bg-stone-200 animate-pulse" />
            <div className="h-10 w-20 rounded-md bg-stone-200 animate-pulse" />
          </div>
          <div className="h-10 w-10 rounded-md bg-stone-200 animate-pulse" />
        </div>

        {/* Metrics section */}
        <div className="space-y-3">
          <div className="h-4 w-16 rounded bg-stone-200 animate-pulse" />
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="rounded-lg border bg-card p-4 space-y-2">
                <div className="h-3 w-20 rounded bg-stone-200 animate-pulse" />
                <div className="h-7 w-10 rounded bg-stone-200 animate-pulse" />
              </div>
            ))}
          </div>
        </div>
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
    <div className="space-y-6">
      {/* ── Back link ──────────────────────────────────────────────────────── */}
      <Link
        href={`/series/${session.seriesId}`}
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
      >
        <ChevronLeft className="size-4" aria-hidden="true" />
        Back to Series
      </Link>

      {/* ── Header: Title + Pencil + StatusBadge ───────────────────────────── */}
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight">{title}</h1>
            <button
              type="button"
              onClick={openTitleEdit}
              disabled={busy}
              className="p-1 text-muted-foreground hover:text-foreground rounded-md transition-colors disabled:opacity-50"
              aria-label="Edit session title"
            >
              <Pencil className="size-4" aria-hidden="true" />
            </button>
            <StatusBadge status={session.status} />
          </div>
          {titleError && (
            <p role="alert" className="text-xs text-destructive">
              {titleError}
            </p>
          )}
          <div className="flex items-center gap-3">
            {session.joinWebUrl && (
              <a
                href={session.joinWebUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="rounded p-1 text-muted-foreground hover:text-foreground focus:outline-none focus-visible:ring-2 focus-visible:ring-ring transition-colors"
                title="Open webinar in Teams"
                aria-label="Open webinar in Teams"
              >
                <ExternalLink className="size-4" aria-hidden="true" />
              </a>
            )}
            {syncing && (
              <span className="inline-flex items-center gap-1.5 text-xs text-muted-foreground">
                <RefreshCw className="size-3 animate-spin" aria-hidden="true" />
                Syncing…
              </span>
            )}
            {!syncing && session.lastSyncAt && (
              <span className="inline-flex items-center gap-1.5 text-xs text-muted-foreground" title={formatDateTime(session.lastSyncAt)}>
                Last synced: {formatDateTime(session.lastSyncAt)}
                {session.status === 'Published' && session.teamsWebinarId && (
                  <button
                    type="button"
                    onClick={doManualSync}
                    className="p-0.5 rounded hover:bg-muted transition-colors"
                    aria-label="Refresh from Teams"
                    title="Refresh from Teams"
                  >
                    <RefreshCw className="size-3.5" aria-hidden="true" />
                  </button>
                )}
              </span>
            )}
            {!syncing && session.status === 'Published' && !session.lastSyncAt && (
              <span className="inline-flex items-center gap-1.5 text-xs text-amber-600">
                Never synced
                {session.teamsWebinarId && (
                  <button
                    type="button"
                    onClick={doManualSync}
                    className="p-0.5 rounded hover:bg-muted transition-colors"
                    aria-label="Refresh from Teams"
                    title="Refresh from Teams"
                  >
                    <RefreshCw className="size-3.5" aria-hidden="true" />
                  </button>
                )}
              </span>
            )}
          </div>
        </div>
      </div>

      {/* ── Banners ──────────────────────────────────────────────────────────── */}
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
      {publishError && (
        <ErrorBanner
          message={publishError}
          onRetry={() => { setPublishError(null); setPublishOpen(true) }}
        />
      )}
      {publishLicenseError && (
        <div
          role="alert"
          className="rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800"
        >
          <strong>Teams webinar license required.</strong> Cannot publish session — assign a Teams
          webinar license, then retry.
        </div>
      )}
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

      {/* ── Save overlay ─────────────────────────────────────────────────────── */}
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

      {/* ── Form: Two-column layout ──────────────────────────────────────── */}
      <form onSubmit={handleSave} noValidate className="space-y-6">
        <div className="grid grid-cols-1 lg:grid-cols-[1fr_1fr] gap-6">
          {/* ── Left column: Schedule ──────────────────────────────────────── */}
          <section className="rounded-lg border bg-card p-6 space-y-4">
            <h2 className="text-base font-semibold">Schedule</h2>

            <DateTimePicker
              label="Start Time"
              value={startsAtDate}
              onChange={(d) => setStartsAtDate(d)}
              disabled={saveLoading}
            />

            <div>
              <DateTimePicker
                label="End Time"
                value={endsAtDate}
                onChange={(d) => setEndsAtDate(d)}
                disabled={saveLoading}
              />
              {endsAtError && (
                <p role="alert" className="mt-1 text-xs text-destructive">
                  {endsAtError}
                </p>
              )}
            </div>
          </section>

          {/* ── Right column: Presenters & Coordinators ────────────────────── */}
          <div className="space-y-6">
            <section className="rounded-lg border bg-card p-6 space-y-3">
              <h2 className="text-base font-semibold">Presenters</h2>
              <PeoplePicker
                label="Presenters"
                hideLabel
                value={presenters}
                onChange={setPresenters}
                disabled={saveLoading}
              />
            </section>

            <section className="rounded-lg border bg-card p-6 space-y-3">
              <h2 className="text-base font-semibold">Coordinators</h2>
              <PeoplePicker
                label="Coordinators"
                hideLabel
                value={coordinators}
                onChange={setCoordinators}
                disabled={saveLoading}
              />
            </section>
          </div>
        </div>

        {/* ── Action buttons ───────────────────────────────────────────────────── */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <button
              type="submit"
              disabled={saveLoading}
              className="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
              <Save className="size-4" aria-hidden="true" />
              {saveLabel}
            </button>
            {!isPublished && (
              <button
                type="button"
                onClick={() => { setPublishError(null); setPublishLicenseError(false); setPublishOpen(true) }}
                disabled={publishLoading}
                className="inline-flex items-center gap-2 rounded-md border border-amber-300 bg-amber-50 px-4 py-2 text-sm font-medium text-amber-800 hover:bg-amber-100 disabled:opacity-50"
              >
                <Rocket className="size-4" aria-hidden="true" />
                Publish to Teams
              </button>
            )}
            <Link
              href={`/series/${session.seriesId}`}
              className="rounded-md border px-4 py-2 text-sm hover:bg-muted transition-colors"
            >
              Cancel
            </Link>
          </div>
          <button
            type="button"
            onClick={() => { setDeleteError(null); setDeleteOpen(true) }}
            className="p-2 text-destructive hover:bg-destructive/10 rounded-md transition-colors"
            aria-label="Delete session"
          >
            <Trash2 className="size-5" aria-hidden="true" />
          </button>
        </div>
      </form>

      {/* ── Metrics card ───────────────────────────────────────────────────── */}
      {metrics && (
        <section className="rounded-lg border bg-card p-6 space-y-4" aria-label="Session metrics">
          <h2 className="text-base font-semibold">Metrics</h2>
          <MetricsPanel
            metrics={[
              { label: 'Registrations', value: metrics.totalRegistrations },
              { label: 'Attendees', value: metrics.totalAttendees },
              { label: 'Registrant Domains', value: metrics.uniqueRegistrantAccountDomains },
              { label: 'Attendee Domains', value: metrics.uniqueAttendeeAccountDomains },
            ]}
          />
          {metrics.warmAccountsTriggered.length > 0 && (
            <div>
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

      {/* ── Title edit dialog ──────────────────────────────────────────────── */}
      <Dialog open={editTitleOpen} onOpenChange={setEditTitleOpen}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Edit Session Title</DialogTitle>
          </DialogHeader>
          <div className="py-2">
            <label htmlFor="edit-session-title" className="block text-sm font-medium mb-1.5">
              Title <span className="text-destructive" aria-hidden="true">*</span>
            </label>
            <input
              id="edit-session-title"
              type="text"
              value={editTitleDraft}
              onChange={(e) => setEditTitleDraft(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault()
                  handleTitleSave()
                }
              }}
              className="w-full rounded-md border px-3 py-2 text-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              autoFocus
            />
          </div>
          <DialogFooter>
            <button
              type="button"
              onClick={() => setEditTitleOpen(false)}
              className="rounded-md border px-4 py-2 text-sm hover:bg-muted transition-colors"
            >
              Cancel
            </button>
            <button
              type="button"
              onClick={handleTitleSave}
              disabled={!editTitleDraft.trim()}
              className="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
              Save
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

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

      {/* ── Publish Session Confirm ──────────────────────────────────────── */}
      <ConfirmDialog
        open={publishOpen}
        title="Publish Session to Teams"
        description="This will create a Teams webinar for this session. Continue?"
        confirmLabel="Publish"
        loading={publishLoading}
        onConfirm={handlePublishSession}
        onCancel={() => setPublishOpen(false)}
      />
    </div>
  )
}
