'use client'

import { useState, useEffect, useCallback, useRef } from 'react'
import { useSession } from 'next-auth/react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { Pencil, Trash2, Rocket, Plus, AlertTriangle, RefreshCw, ExternalLink } from 'lucide-react'
import { StatusBadge } from '@/components/status-badge'
import { ErrorBanner } from '@/components/error-banner'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { MetricsPanel } from '@/components/metrics-panel'
import { updateSeries, deleteSeries, publishSeries, syncSeries } from '@/lib/api/series'
import { deleteSession, publishSession } from '@/lib/api/sessions'
import type { SeriesResponse, SessionListItem, SeriesMetricsResponse } from '@/lib/api/types'

function formatDateTime(iso: string | null | undefined) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

function formatRelativeTime(iso: string | null | undefined) {
  if (!iso) return null
  const date = new Date(iso)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  if (diffMins < 1) return 'just now'
  if (diffMins < 60) return `${diffMins}m ago`
  const diffHours = Math.floor(diffMins / 60)
  if (diffHours < 24) return `${diffHours}h ago`
  const diffDays = Math.floor(diffHours / 24)
  return `${diffDays}d ago`
}

interface Props {
  series: SeriesResponse
  sessions: SessionListItem[]
  metrics: SeriesMetricsResponse | null
}

export default function SeriesDetailView({ series, sessions, metrics }: Props) {
  const { data: authSession, status: sessionStatus } = useSession()
  const router = useRouter()
  const token = authSession?.accessToken ?? ''
  const busy = sessionStatus === 'loading'

  // ── Auto-sync published sessions (stale after 15 min) ────────────────────
  const [syncing, setSyncing] = useState(false)
  const hasSynced = useRef(false)
  const STALE_MS = 15 * 60 * 1000 // 15 minutes

  function isSyncStale(sessionList: SessionListItem[]): boolean {
    const published = sessionList.filter(s => s.status === 'Published')
    if (published.length === 0) return false
    // Stale if any published session has never synced or oldest sync > 15 min
    return published.some(s => {
      if (!s.lastSyncAt) return true
      return Date.now() - new Date(s.lastSyncAt).getTime() > STALE_MS
    })
  }

  const doSync = useCallback(async () => {
    if (!token || series.status !== 'Published') return
    const hasPublished = sessions.some(s => s.status === 'Published')
    if (!hasPublished) return
    setSyncing(true)
    try {
      await syncSeries(series.seriesId, token)
      router.refresh()
    } catch {
      // Sync failure is non-blocking — user still sees cached data
    } finally {
      setSyncing(false)
    }
  }, [token, series.seriesId, series.status, sessions, router])

  const doAutoSync = useCallback(async () => {
    if (hasSynced.current) return
    if (!isSyncStale(sessions)) { hasSynced.current = true; return }
    hasSynced.current = true
    await doSync()
  }, [sessions, doSync])

  useEffect(() => {
    doAutoSync()
  }, [doAutoSync])

  // ── Edit Series ──────────────────────────────────────────────────────────
  const [editOpen, setEditOpen] = useState(false)
  const [editTitle, setEditTitle] = useState(series.title)
  const [editLoading, setEditLoading] = useState(false)
  const [editError, setEditError] = useState<string | null>(null)

  function openEdit() {
    setEditTitle(series.title)
    setEditError(null)
    setEditOpen(true)
  }

  async function handleEditSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!editTitle.trim()) return
    setEditLoading(true)
    setEditError(null)
    try {
      await updateSeries(series.seriesId, { title: editTitle.trim() }, token)
      setEditOpen(false)
      router.refresh()
    } catch (err) {
      setEditError(err instanceof Error ? err.message : 'Failed to update series')
    } finally {
      setEditLoading(false)
    }
  }

  // ── Delete Series ─────────────────────────────────────────────────────────
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [deleteLoading, setDeleteLoading] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  async function handleDeleteSeries() {
    setDeleteLoading(true)
    setDeleteError(null)
    try {
      await deleteSeries(series.seriesId, token)
      router.push('/series')
    } catch (err) {
      setDeleteError(err instanceof Error ? err.message : 'Failed to delete series')
      setDeleteLoading(false)
      setDeleteOpen(false)
    }
  }

  // ── Publish Series ────────────────────────────────────────────────────────
  const [publishOpen, setPublishOpen] = useState(false)
  const [publishLoading, setPublishLoading] = useState(false)
  const [publishError, setPublishError] = useState<string | null>(null)
  const [publishLicenseError, setPublishLicenseError] = useState(false)

  async function handlePublishSeries() {
    setPublishLoading(true)
    setPublishError(null)
    setPublishLicenseError(false)
    try {
      await publishSeries(series.seriesId, token)
      setPublishOpen(false)
      router.refresh()
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Publish failed'
      if (msg.includes('TEAMS_LICENSE_REQUIRED')) {
        setPublishLicenseError(true)
      } else {
        setPublishError(msg)
      }
      setPublishLoading(false)
      setPublishOpen(false)
    }
  }

  // ── Delete Session ────────────────────────────────────────────────────────
  const [deleteSessionId, setDeleteSessionId] = useState<string | null>(null)
  const [deleteSessionLoading, setDeleteSessionLoading] = useState(false)
  const [deleteSessionError, setDeleteSessionError] = useState<string | null>(null)

  async function handleDeleteSession() {
    if (!deleteSessionId) return
    setDeleteSessionLoading(true)
    setDeleteSessionError(null)
    try {
      await deleteSession(deleteSessionId, token)
      setDeleteSessionId(null)
      router.refresh()
    } catch (err) {
      setDeleteSessionError(err instanceof Error ? err.message : 'Failed to delete session')
      setDeleteSessionLoading(false)
      setDeleteSessionId(null)
    }
  }

  // ── Publish Session (individual) ──────────────────────────────────────────
  const [publishSessionId, setPublishSessionId] = useState<string | null>(null)
  const [publishSessionLoading, setPublishSessionLoading] = useState(false)
  const [publishSessionError, setPublishSessionError] = useState<string | null>(null)
  const [publishSessionLicenseError, setPublishSessionLicenseError] = useState(false)

  async function handlePublishSession() {
    if (!publishSessionId) return
    setPublishSessionLoading(true)
    setPublishSessionError(null)
    setPublishSessionLicenseError(false)
    try {
      await publishSession(publishSessionId, token)
      setPublishSessionId(null)
      router.refresh()
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Session publish failed'
      if (msg.includes('TEAMS_LICENSE_REQUIRED')) {
        setPublishSessionLicenseError(true)
      } else {
        setPublishSessionError(msg)
      }
      setPublishSessionLoading(false)
      setPublishSessionId(null)
    }
  }

  const seriesDisplayStatus =
    series.status === 'Published' && series.draftSessionCount > 0
      ? 'Partially Published'
      : series.status

  return (
    <div className="space-y-6">
      {/* ── Header ──────────────────────────────────────────────────────── */}
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight">{series.title}</h1>
            <button
              type="button"
              onClick={openEdit}
              disabled={busy}
              className="p-1 text-muted-foreground hover:text-foreground rounded-md transition-colors disabled:opacity-50"
              aria-label="Edit series title"
            >
              <Pencil className="size-4" aria-hidden="true" />
            </button>
            <StatusBadge status={seriesDisplayStatus} />
          </div>
          <p className="text-xs text-muted-foreground">
            Created {formatDateTime(series.createdAt)} · Updated {formatDateTime(series.updatedAt)}
          </p>
        </div>

        <div className="flex items-center gap-2">
          {series.status === 'Draft' && (
            <button
              type="button"
              onClick={() => { setPublishError(null); setPublishLicenseError(false); setPublishOpen(true) }}
              disabled={busy}
              className="inline-flex items-center gap-2 rounded-md bg-primary px-3 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
              <Rocket className="size-4" aria-hidden="true" />
              Publish Series
            </button>
          )}
          <button
            type="button"
            onClick={() => { setDeleteError(null); setDeleteOpen(true) }}
            disabled={busy}
            className="p-2 text-destructive hover:bg-destructive/10 rounded-md transition-colors disabled:opacity-50"
            aria-label="Delete series"
          >
            <Trash2 className="size-4" aria-hidden="true" />
          </button>
        </div>
      </div>

      {/* ── Error banners ────────────────────────────────────────────────── */}
      {deleteError && (
        <ErrorBanner message={deleteError} onRetry={() => setDeleteOpen(true)} />
      )}
      {deleteSessionError && (
        <ErrorBanner message={deleteSessionError} />
      )}
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
          <strong>Teams webinar license required.</strong> Assign a Teams webinar license to the
          service account, then retry publishing.
        </div>
      )}
      {publishSessionError && (
        <ErrorBanner
          message={publishSessionError}
          onRetry={() => { setPublishSessionError(null) }}
        />
      )}
      {publishSessionLicenseError && (
        <div
          role="alert"
          className="rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800"
        >
          <strong>Teams webinar license required.</strong> Cannot publish session — assign a Teams
          webinar license, then retry.
        </div>
      )}

      {/* ── Metrics ──────────────────────────────────────────────────────── */}
      <section aria-label="Series metrics">
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-muted-foreground">
          Metrics
        </h2>
        <MetricsPanel
          metrics={[
            { label: 'Registrations', value: metrics?.totalRegistrations ?? 0 },
            { label: 'Attendees', value: metrics?.totalAttendees ?? 0 },
            { label: 'Accts Influenced', value: metrics?.uniqueAccountsInfluenced ?? 0 },
            { label: 'Warm Accounts', value: metrics?.warmAccounts.length ?? 0 },
          ]}
        />
        {metrics && metrics.warmAccounts.length > 0 && (
          <div className="mt-3">
            <p className="mb-2 text-xs font-medium text-muted-foreground">Warm accounts:</p>
            <div className="flex flex-wrap gap-2">
              {metrics.warmAccounts.map((wa) => (
                <span
                  key={`${wa.accountDomain}-${wa.warmRule}`}
                  className="rounded-full border bg-muted px-2.5 py-0.5 text-xs"
                >
                  {wa.accountDomain}{' '}
                  <span className="font-semibold text-primary">{wa.warmRule}</span>
                </span>
              ))}
            </div>
          </div>
        )}
      </section>

      {/* ── Sessions table ───────────────────────────────────────────────── */}
      <section aria-label="Sessions">
        <div className="mb-3 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
              Sessions ({sessions.length})
            </h2>
            {series.status === 'Published' && series.draftSessionCount > 0 && (
              <span className="inline-flex items-center gap-1 rounded-full border border-amber-200 bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-700">
                {series.draftSessionCount} unpublished
              </span>
            )}
            {syncing && (
              <span className="inline-flex items-center gap-1.5 text-xs text-muted-foreground">
                <span className="size-3 rounded-full border-2 border-primary/30 border-t-primary animate-spin" aria-hidden="true" />
                Syncing from Teams…
              </span>
            )}
            {!syncing && series.status === 'Published' && sessions.some(s => s.status === 'Published') && (
              <button
                type="button"
                onClick={doSync}
                className="inline-flex items-center gap-1.5 p-1 rounded text-xs text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
                aria-label="Refresh from Teams"
                title="Refresh from Teams"
              >
                <RefreshCw className="size-3.5" aria-hidden="true" />
              </button>
            )}
          </div>
          <Link
            href={`/series/${series.seriesId}/sessions/new`}
            className="inline-flex items-center gap-1.5 rounded-md bg-primary px-3 py-1.5 text-xs font-medium text-primary-foreground hover:bg-primary/90"
          >
            <Plus className="size-3.5" aria-hidden="true" />
            Add Session
          </Link>
        </div>

        {sessions.length === 0 ? (
          <div className="rounded-lg border bg-card px-8 py-12 text-center text-muted-foreground">
            <p>No sessions in this series. Add a session.</p>
          </div>
        ) : (
          <div className="rounded-lg border bg-card overflow-hidden">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50 text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-4 py-3 text-left font-medium">Title</th>
                  <th className="px-4 py-3 text-left font-medium">Starts</th>
                  <th className="px-4 py-3 text-left font-medium">Ends</th>
                  <th className="px-4 py-3 text-left font-medium">Status</th>
                  <th className="px-4 py-3 text-left font-medium">Last Synced</th>
                  <th className="px-4 py-3 text-left font-medium">Drift</th>
                  <th className="px-4 py-3 text-right font-medium">Reg.</th>
                  <th className="px-4 py-3 text-right font-medium">Att.</th>
                  <th className="px-4 py-3 text-right font-medium w-20">Actions</th>
                </tr>
              </thead>
              <tbody>
                {sessions.map((s) => (
                  <tr
                    key={s.sessionId}
                    onClick={() => router.push(`/sessions/${s.sessionId}`)}
                    className="border-b last:border-b-0 cursor-pointer hover:bg-muted/40 transition-colors"
                  >
                    <td className="px-4 py-3 font-medium">
                      {s.title}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground whitespace-nowrap">
                      {formatDateTime(s.startsAt)}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground whitespace-nowrap">
                      {formatDateTime(s.endsAt)}
                    </td>
                    <td className="px-4 py-3">
                      <StatusBadge status={s.status} />
                    </td>
                    <td className="px-4 py-3 text-muted-foreground whitespace-nowrap text-xs">
                      {s.lastSyncAt
                        ? <span title={formatDateTime(s.lastSyncAt)}>{formatRelativeTime(s.lastSyncAt)}</span>
                        : s.status === 'Published' ? <span className="text-amber-600">Never</span> : '—'}
                    </td>
                    <td className="px-4 py-3">
                      {s.driftStatus === 'DriftDetected' && (
                        <span className="inline-flex items-center gap-1 rounded-full border border-amber-200 bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-700">
                          <AlertTriangle className="size-3" aria-hidden="true" />
                          Drift
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-3 tabular-nums text-right">{s.totalRegistrations}</td>
                    <td className="px-4 py-3 tabular-nums text-right">{s.totalAttendees}</td>
                    <td
                      className="px-4 py-3 text-right"
                      onClick={(e) => e.stopPropagation()}
                    >
                      <div className="flex items-center justify-end gap-1">
                        {series.status === 'Published' && s.status === 'Draft' && (
                          <button
                            type="button"
                            onClick={() => setPublishSessionId(s.sessionId)}
                            disabled={publishSessionLoading}
                            className="rounded p-1 text-amber-600 hover:text-amber-800 hover:bg-amber-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50"
                            aria-label={`Publish ${s.title} to Teams`}
                            title="Publish to Teams"
                          >
                            <Rocket className="size-3.5" />
                          </button>
                        )}
                        {s.status === 'Published' && s.teamsWebinarId && (
                          <a
                            href={`https://teams.microsoft.com/l/virtual-event/${s.teamsWebinarId}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="rounded p-1 text-muted-foreground hover:text-foreground focus:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                            aria-label={`Open ${s.title} in Teams`}
                            onClick={(e) => e.stopPropagation()}
                          >
                            <ExternalLink className="size-3.5" />
                          </a>
                        )}
                        <button
                          type="button"
                          onClick={() => router.push(`/sessions/${s.sessionId}`)}
                          className="rounded p-1 text-muted-foreground hover:text-foreground focus:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                          aria-label={`Edit ${s.title}`}
                        >
                          <Pencil className="size-3.5" />
                        </button>
                        <button
                          type="button"
                          onClick={() => setDeleteSessionId(s.sessionId)}
                          className="rounded p-1 text-muted-foreground hover:text-destructive focus:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                          aria-label={`Delete ${s.title}`}
                        >
                          <Trash2 className="size-3.5" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {/* ── Edit Series Modal ─────────────────────────────────────────────── */}
      {editOpen && (
        <div
          role="dialog"
          aria-modal="true"
          aria-labelledby="edit-dialog-title"
          className="fixed inset-0 z-50 flex items-center justify-center"
        >
          <div
            className="fixed inset-0 bg-black/50"
            onClick={() => setEditOpen(false)}
          />
          <div className="relative z-10 w-full max-w-md rounded-lg border bg-background p-6 shadow-lg">
            <h2 id="edit-dialog-title" className="text-base font-semibold mb-4">
              Edit Series
            </h2>
            {editError && <ErrorBanner message={editError} className="mb-4" />}
            <form onSubmit={handleEditSubmit} className="space-y-4">
              <div>
                <label htmlFor="series-title" className="block text-sm font-medium mb-1">
                  Title <span className="text-destructive" aria-hidden="true">*</span>
                </label>
                <input
                  id="series-title"
                  type="text"
                  value={editTitle}
                  onChange={(e) => setEditTitle(e.target.value)}
                  required
                  className="w-full rounded-md border px-3 py-2 text-sm focus:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  autoFocus
                />
              </div>
              <div className="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setEditOpen(false)}
                  disabled={editLoading}
                  className="rounded-md border px-4 py-2 text-sm hover:bg-muted disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={editLoading || !editTitle.trim()}
                  className="inline-flex items-center gap-2 rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                >
                  {editLoading && (
                    <span className="size-3.5 rounded-full border-2 border-white/30 border-t-white animate-spin" aria-hidden="true" />
                  )}
                  Save
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* ── Delete Series Confirm ─────────────────────────────────────────── */}
      <ConfirmDialog
        open={deleteOpen}
        title="Delete Series"
        description="This will delete all sessions and their Teams webinars. Continue?"
        confirmLabel="Delete Series"
        dangerous
        loading={deleteLoading}
        onConfirm={handleDeleteSeries}
        onCancel={() => setDeleteOpen(false)}
      />

      {/* ── Publish Series Confirm ────────────────────────────────────────── */}
      <ConfirmDialog
        open={publishOpen}
        title="Publish Series"
        description="This will create Teams webinars for all sessions. Continue?"
        confirmLabel="Publish"
        loading={publishLoading}
        onConfirm={handlePublishSeries}
        onCancel={() => setPublishOpen(false)}
      />

      {/* ── Delete Session Confirm ────────────────────────────────────────── */}
      <ConfirmDialog
        open={deleteSessionId !== null}
        title="Delete Session"
        description="This will permanently delete the session and its Teams webinar. Continue?"
        confirmLabel="Delete Session"
        dangerous
        loading={deleteSessionLoading}
        onConfirm={handleDeleteSession}
        onCancel={() => setDeleteSessionId(null)}
      />

      {/* ── Publish Session Confirm ───────────────────────────────────────── */}
      <ConfirmDialog
        open={publishSessionId !== null}
        title="Publish Session to Teams"
        description="This will create a Teams webinar for this session. Continue?"
        confirmLabel="Publish"
        loading={publishSessionLoading}
        onConfirm={handlePublishSession}
        onCancel={() => setPublishSessionId(null)}
      />
    </div>
  )
}
