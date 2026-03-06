'use client'

import { useState, useEffect, useCallback, useRef } from 'react'
import { useSession } from 'next-auth/react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { PencilIcon, TrashIcon, RocketIcon, PlusIcon, AlertIcon, SyncIcon, LinkExternalIcon } from '@primer/octicons-react'
import { Button, IconButton, Dialog, Banner, Spinner, TextInput, Token } from '@primer/react'
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
            <IconButton
              icon={PencilIcon}
              aria-label="Edit series title"
              variant="invisible"
              size="small"
              onClick={openEdit}
              disabled={busy}
            />
            <StatusBadge status={seriesDisplayStatus} />
          </div>
          <p className="text-xs" style={{ color: 'var(--fgColor-muted)' }}>
            Created {formatDateTime(series.createdAt)} · Updated {formatDateTime(series.updatedAt)}
          </p>
        </div>

        <div className="flex items-center gap-2">
          {series.status === 'Draft' && (
            <Button
              variant="primary"
              leadingVisual={RocketIcon}
              onClick={() => { setPublishError(null); setPublishLicenseError(false); setPublishOpen(true) }}
              disabled={busy}
            >
              Publish Series
            </Button>
          )}
          <IconButton
            icon={TrashIcon}
            aria-label="Delete series"
            variant="danger"
            onClick={() => { setDeleteError(null); setDeleteOpen(true) }}
            disabled={busy}
          />
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
        <Banner
          variant="warning"
          title="Teams webinar license required."
          description="Assign a Teams webinar license to the service account, then retry publishing."
        />
      )}
      {publishSessionError && (
        <ErrorBanner
          message={publishSessionError}
          onRetry={() => { setPublishSessionError(null) }}
        />
      )}
      {publishSessionLicenseError && (
        <Banner
          variant="warning"
          title="Teams webinar license required."
          description="Cannot publish session — assign a Teams webinar license, then retry."
        />
      )}

      {/* ── Metrics ──────────────────────────────────────────────────────── */}
      <section aria-label="Series metrics">
        <h2
          className="mb-3 text-sm font-semibold uppercase tracking-wide"
          style={{ color: 'var(--fgColor-muted)' }}
        >
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
            <p
              className="mb-2 text-xs font-medium"
              style={{ color: 'var(--fgColor-muted)' }}
            >
              Warm accounts:
            </p>
            <div className="flex flex-wrap gap-2">
              {metrics.warmAccounts.map((wa) => (
                <Token
                  key={`${wa.accountDomain}-${wa.warmRule}`}
                  text={
                    <>
                      {wa.accountDomain}{' '}
                      <span className="font-semibold" style={{ color: 'var(--fgColor-accent)' }}>
                        {wa.warmRule}
                      </span>
                    </>
                  }
                  size="medium"
                />
              ))}
            </div>
          </div>
        )}
      </section>

      {/* ── Sessions table ───────────────────────────────────────────────── */}
      <section aria-label="Sessions">
        <div className="mb-3 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <h2
              className="text-sm font-semibold uppercase tracking-wide"
              style={{ color: 'var(--fgColor-muted)' }}
            >
              Sessions ({sessions.length})
            </h2>
            {series.status === 'Published' && series.draftSessionCount > 0 && (
              <span
                className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium"
                style={{
                  color: 'var(--fgColor-attention)',
                  backgroundColor: 'var(--bgColor-attention-muted)',
                  border: '1px solid var(--borderColor-attention-muted)',
                }}
              >
                {series.draftSessionCount} unpublished
              </span>
            )}
            {syncing && (
              <span
                className="inline-flex items-center gap-1.5 text-xs"
                style={{ color: 'var(--fgColor-muted)' }}
              >
                <Spinner size="small" />
                Syncing from Teams…
              </span>
            )}
            {!syncing && series.status === 'Published' && sessions.some(s => s.status === 'Published') && (
              <IconButton
                icon={SyncIcon}
                aria-label="Refresh from Teams"
                variant="invisible"
                size="small"
                onClick={doSync}
              />
            )}
          </div>
          <Button
            as={Link}
            href={`/series/${series.seriesId}/sessions/new`}
            variant="primary"
            size="small"
            leadingVisual={PlusIcon}
          >
            Add Session
          </Button>
        </div>

        {sessions.length === 0 ? (
          <div
            className="rounded-lg px-8 py-12 text-center"
            style={{
              color: 'var(--fgColor-muted)',
              backgroundColor: 'var(--bgColor-default)',
              border: '1px solid var(--borderColor-default)',
            }}
          >
            <p>No sessions in this series. Add a session.</p>
          </div>
        ) : (
          <div
            className="rounded-lg overflow-hidden"
            style={{
              backgroundColor: 'var(--bgColor-default)',
              border: '1px solid var(--borderColor-default)',
            }}
          >
            <table className="w-full text-sm">
              <thead>
                <tr
                  className="text-xs uppercase tracking-wide"
                  style={{
                    color: 'var(--fgColor-muted)',
                    backgroundColor: 'var(--bgColor-muted)',
                    borderBottom: '1px solid var(--borderColor-default)',
                  }}
                >
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
                {sessions.map((s, idx) => (
                  <tr
                    key={s.sessionId}
                    onClick={() => router.push(`/sessions/${s.sessionId}`)}
                    className="cursor-pointer transition-colors"
                    style={{
                      borderBottom: idx < sessions.length - 1 ? '1px solid var(--borderColor-default)' : undefined,
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.backgroundColor = 'var(--bgColor-muted)'
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.backgroundColor = ''
                    }}
                  >
                    <td className="px-4 py-3 font-medium">
                      {s.title}
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap" style={{ color: 'var(--fgColor-muted)' }}>
                      {formatDateTime(s.startsAt)}
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap" style={{ color: 'var(--fgColor-muted)' }}>
                      {formatDateTime(s.endsAt)}
                    </td>
                    <td className="px-4 py-3">
                      <StatusBadge status={s.status} />
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-xs" style={{ color: 'var(--fgColor-muted)' }}>
                      {s.lastSyncAt
                        ? <span title={formatDateTime(s.lastSyncAt)}>{formatRelativeTime(s.lastSyncAt)}</span>
                        : s.status === 'Published' ? <span style={{ color: 'var(--fgColor-attention)' }}>Never</span> : '—'}
                    </td>
                    <td className="px-4 py-3">
                      {s.driftStatus === 'DriftDetected' && (
                        <span
                          className="inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium"
                          style={{
                            color: 'var(--fgColor-attention)',
                            backgroundColor: 'var(--bgColor-attention-muted)',
                            border: '1px solid var(--borderColor-attention-muted)',
                          }}
                        >
                          <AlertIcon size={12} />
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
                          <IconButton
                            icon={RocketIcon}
                            aria-label={`Publish ${s.title} to Teams`}
                            variant="invisible"
                            size="small"
                            onClick={() => setPublishSessionId(s.sessionId)}
                            disabled={publishSessionLoading}
                            unsafeDisableTooltip={false}
                            style={{ color: 'var(--fgColor-attention)' }}
                          />
                        )}
                        {s.status === 'Published' && s.joinWebUrl && (
                          <IconButton
                            as="a"
                            href={s.joinWebUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            icon={LinkExternalIcon}
                            aria-label={`Open ${s.title} in Teams`}
                            variant="invisible"
                            size="small"
                            onClick={(e: React.MouseEvent) => e.stopPropagation()}
                          />
                        )}
                        <IconButton
                          icon={PencilIcon}
                          aria-label={`Edit ${s.title}`}
                          variant="invisible"
                          size="small"
                          onClick={() => router.push(`/sessions/${s.sessionId}`)}
                        />
                        <IconButton
                          icon={TrashIcon}
                          aria-label={`Delete ${s.title}`}
                          variant="danger"
                          size="small"
                          onClick={() => setDeleteSessionId(s.sessionId)}
                        />
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
        <Dialog
          title="Edit Series"
          width="medium"
          onClose={() => setEditOpen(false)}
          footerButtons={[
            {
              buttonType: 'default',
              content: 'Cancel',
              onClick: () => setEditOpen(false),
              disabled: editLoading,
            },
            {
              buttonType: 'primary',
              content: (
                <span className="inline-flex items-center gap-2">
                  {editLoading && <Spinner size="small" />}
                  Save
                </span>
              ),
              onClick: (e) => handleEditSubmit(e),
              disabled: editLoading || !editTitle.trim(),
            },
          ]}
        >
          {editError && <ErrorBanner message={editError} className="mb-4" />}
          <form
            onSubmit={handleEditSubmit}
            id="edit-series-form"
            className="space-y-4"
          >
            <div>
              <label
                htmlFor="series-title"
                className="block text-sm font-medium mb-1"
              >
                Title{' '}
                <span style={{ color: 'var(--fgColor-danger)' }} aria-hidden="true">
                  *
                </span>
              </label>
              <TextInput
                id="series-title"
                value={editTitle}
                onChange={(e) => setEditTitle(e.target.value)}
                required
                block
                autoFocus
              />
            </div>
          </form>
        </Dialog>
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
