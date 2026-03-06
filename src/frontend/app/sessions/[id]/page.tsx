'use client'

import { useState, useEffect, useCallback } from 'react'
import { useRouter, useParams } from 'next/navigation'
import { useSession } from 'next-auth/react'
import Link from 'next/link'
import {
  ChevronLeftIcon,
  CheckIcon,
  TrashIcon,
  SyncIcon,
  LinkExternalIcon,
  RocketIcon,
  PencilIcon,
} from '@primer/octicons-react'
import {
  Button,
  IconButton,
  Dialog,
  TextInput,
  FormControl,
  Banner,
  Spinner,
  Token,
  SkeletonBox,
} from '@primer/react'
import { SkeletonText } from '@primer/react/experimental'
import { ErrorBanner } from '@/components/error-banner'
import { StatusBadge } from '@/components/status-badge'
import { MetricsPanel } from '@/components/metrics-panel'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { PeoplePicker } from '@/components/people-picker'
import { DateTimePicker } from '@/components/date-time-picker'

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
        <SkeletonText size="bodySmall" maxWidth={112} />

        {/* Header: title + pencil + status badge */}
        <div className="flex items-center gap-3">
          <SkeletonBox height={32} width={256} />
          <SkeletonBox height={32} width={32} />
          <SkeletonBox height={24} width={80} className="rounded-full" />
        </div>

        {/* Two-column grid */}
        <div className="grid grid-cols-1 lg:grid-cols-[1fr_1fr] gap-6">
          {/* Schedule card */}
          <div className="space-y-4 rounded-lg border p-6" style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}>
            <SkeletonText size="bodySmall" maxWidth={96} />
            <div className="space-y-1.5">
              <SkeletonText size="bodySmall" maxWidth={80} />
              <SkeletonBox height={40} />
            </div>
            <div className="space-y-1.5">
              <SkeletonText size="bodySmall" maxWidth={64} />
              <SkeletonBox height={40} />
            </div>
          </div>

          {/* Presenters + Coordinators */}
          <div className="space-y-6">
            <div className="space-y-3 rounded-lg border p-6" style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}>
              <SkeletonBox height={40} />
            </div>
            <div className="space-y-3 rounded-lg border p-6" style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}>
              <SkeletonBox height={40} />
            </div>
          </div>
        </div>

        {/* Button row */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <SkeletonBox height={40} width={96} />
            <SkeletonBox height={40} width={128} />
            <SkeletonBox height={40} width={80} />
          </div>
          <SkeletonBox height={40} width={40} />
        </div>

        {/* Metrics section */}
        <div className="space-y-3">
          <SkeletonText size="bodySmall" maxWidth={64} />
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="rounded-lg border p-4 space-y-2" style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}>
                <SkeletonText size="bodySmall" maxWidth={80} />
                <SkeletonBox height={28} width={40} />
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
        className="inline-flex items-center gap-1 text-sm transition-colors"
        style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
        onMouseEnter={(e) => { (e.currentTarget as HTMLAnchorElement).style.color = 'var(--fgColor-default, var(--color-fg-default))' }}
        onMouseLeave={(e) => { (e.currentTarget as HTMLAnchorElement).style.color = 'var(--fgColor-muted, var(--color-fg-muted))' }}
      >
        <ChevronLeftIcon size={16} aria-hidden="true" />
        Back to Series
      </Link>

      {/* ── Header: Title + Pencil + StatusBadge ───────────────────────────── */}
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight">{title}</h1>
            <IconButton
              icon={PencilIcon}
              aria-label="Edit session title"
              variant="invisible"
              size="small"
              onClick={openTitleEdit}
              disabled={busy}
            />
            <StatusBadge status={session.status} />
          </div>
          {titleError && (
            <p role="alert" className="text-xs" style={{ color: 'var(--fgColor-danger, var(--color-danger-fg))' }}>
              {titleError}
            </p>
          )}
          <div className="flex items-center gap-3">
            {session.joinWebUrl && (
              <a
                href={session.joinWebUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="rounded p-1 transition-colors focus:outline-none focus-visible:ring-2"
                style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
                onMouseEnter={(e) => { (e.currentTarget as HTMLAnchorElement).style.color = 'var(--fgColor-default, var(--color-fg-default))' }}
                onMouseLeave={(e) => { (e.currentTarget as HTMLAnchorElement).style.color = 'var(--fgColor-muted, var(--color-fg-muted))' }}
                title="Open webinar in Teams"
                aria-label="Open webinar in Teams"
              >
                <LinkExternalIcon size={16} aria-hidden="true" />
              </a>
            )}
            {syncing && (
              <span className="inline-flex items-center gap-1.5 text-xs" style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}>
                <Spinner size="small" />
                Syncing…
              </span>
            )}
            {!syncing && session.lastSyncAt && (
              <span className="inline-flex items-center gap-1.5 text-xs" style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }} title={formatDateTime(session.lastSyncAt)}>
                Last synced: {formatDateTime(session.lastSyncAt)}
                {session.status === 'Published' && session.teamsWebinarId && (
                  <IconButton
                    icon={SyncIcon}
                    aria-label="Refresh from Teams"
                    variant="invisible"
                    size="small"
                    onClick={doManualSync}
                  />
                )}
              </span>
            )}
            {!syncing && session.status === 'Published' && !session.lastSyncAt && (
              <span className="inline-flex items-center gap-1.5 text-xs text-amber-600">
                Never synced
                {session.teamsWebinarId && (
                  <IconButton
                    icon={SyncIcon}
                    aria-label="Refresh from Teams"
                    variant="invisible"
                    size="small"
                    onClick={doManualSync}
                  />
                )}
              </span>
            )}
          </div>
        </div>
      </div>

      {/* ── Banners ──────────────────────────────────────────────────────────── */}
      {session.driftStatus === 'DriftDetected' && (
        <Banner variant="warning" title="Drift detected — Builder values differ from Teams">
          <dl className="grid grid-cols-3 gap-x-4 gap-y-1 text-xs mt-2">
            <dt className="font-medium">Field</dt>
            <dt className="font-medium">Builder</dt>
            <dt className="font-medium">Stored</dt>
            <dd>Title</dd>
            <dd>{session.title}</dd>
            <dd>—</dd>
            <dd>Starts At</dd>
            <dd>{formatDateTime(session.startsAt)}</dd>
            <dd>—</dd>
            <dd>Ends At</dd>
            <dd>{formatDateTime(session.endsAt)}</dd>
            <dd>—</dd>
          </dl>
        </Banner>
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
        <Banner variant="warning" title="Teams webinar license required.">
          Cannot publish session — assign a Teams webinar license, then retry.
        </Banner>
      )}
      {teamsUpdateFailed && (
        <Banner
          variant="critical"
          title="Publish failed — Teams webinar could not be updated."
          primaryAction={
            <Banner.PrimaryAction onClick={handleSave}>
              Retry
            </Banner.PrimaryAction>
          }
        />
      )}

      {/* ── Save overlay ─────────────────────────────────────────────────────── */}
      {saveLoading && (
        <div className="fixed inset-0 z-50 flex items-center justify-center" style={{ backgroundColor: 'rgba(0,0,0,0.3)' }} aria-live="polite" aria-busy="true">
          <div className="flex items-center gap-3 rounded-lg px-6 py-4 shadow-lg" style={{ backgroundColor: 'var(--bgColor-default)' }}>
            <Spinner size="medium" />
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
          <section className="rounded-lg border p-6 space-y-4" style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}>
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
                <p role="alert" className="mt-1 text-xs" style={{ color: 'var(--fgColor-danger, var(--color-danger-fg))' }}>
                  {endsAtError}
                </p>
              )}
            </div>
          </section>

          {/* ── Right column: Presenters & Coordinators ────────────────────── */}
          <div className="space-y-6">
            <section className="rounded-lg border p-6 space-y-3" style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}>
              <h2 className="text-base font-semibold">Presenters</h2>
              <PeoplePicker
                label="Presenters"
                hideLabel
                value={presenters}
                onChange={setPresenters}
                disabled={saveLoading}
              />
            </section>

            <section className="rounded-lg border p-6 space-y-3" style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}>
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
            <Button
              type="submit"
              variant="primary"
              leadingVisual={CheckIcon}
              disabled={saveLoading}
            >
              {saveLabel}
            </Button>
            {!isPublished && (
              <Button
                leadingVisual={RocketIcon}
                onClick={() => { setPublishError(null); setPublishLicenseError(false); setPublishOpen(true) }}
                disabled={publishLoading}
                className="!border-amber-300 !bg-amber-50 !text-amber-800 hover:!bg-amber-100"
              >
                Publish to Teams
              </Button>
            )}
            <Button as={Link} href={`/series/${session.seriesId}`} variant="default">
              Cancel
            </Button>
          </div>
          <IconButton
            icon={TrashIcon}
            aria-label="Delete session"
            variant="danger"
            onClick={() => { setDeleteError(null); setDeleteOpen(true) }}
          />
        </div>
      </form>

      {/* ── Metrics card ───────────────────────────────────────────────────── */}
      {metrics && (
        <section className="rounded-lg border p-6 space-y-4" style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }} aria-label="Session metrics">
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
              <p className="mb-2 text-xs font-medium" style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}>
                Warm accounts triggered:
              </p>
              <div className="flex flex-wrap gap-2">
                {metrics.warmAccountsTriggered.map((domain) => (
                  <Token key={domain} text={domain} />
                ))}
              </div>
            </div>
          )}
        </section>
      )}

      {/* ── Title edit dialog ──────────────────────────────────────────────── */}
      {editTitleOpen && (
        <Dialog
          title="Edit Session Title"
          onClose={() => setEditTitleOpen(false)}
          footerButtons={[
            { buttonType: 'default', content: 'Cancel', onClick: () => setEditTitleOpen(false) },
            { buttonType: 'primary', content: 'Save', onClick: handleTitleSave, disabled: !editTitleDraft.trim() },
          ]}
        >
          <FormControl required>
            <FormControl.Label>Title</FormControl.Label>
            <TextInput
              value={editTitleDraft}
              onChange={(e) => setEditTitleDraft(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault()
                  handleTitleSave()
                }
              }}
              block
              autoFocus
            />
          </FormControl>
        </Dialog>
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
