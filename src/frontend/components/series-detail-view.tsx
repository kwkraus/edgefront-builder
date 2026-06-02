'use client'

import { useState, useEffect, useMemo } from 'react'
import { useSession } from 'next-auth/react'
import { useRouter } from 'next/navigation'
import Link from 'next/link'
import { TrashIcon, PlusIcon, PeopleIcon, OrganizationIcon, DownloadIcon } from '@primer/octicons-react'
import { Button, IconButton, Tooltip, Token } from '@primer/react'
import { StatusBadge } from '@/components/status-badge'
import { ErrorBanner } from '@/components/error-banner'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { InlineEditableTitle } from '@/components/inline-editable-title'
import { MetricsPanel } from '@/components/metrics-panel'
import { updateSeries, deleteSeries, exportSeriesMarkdown } from '@/lib/api/series'
import { deleteSession } from '@/lib/api/sessions'
import type { SeriesResponse, SessionListItem, SeriesMetricsResponse } from '@/lib/api/types'

function buildPeopleTooltip(s: SessionListItem): string {
  const lines: string[] = []

  if (s.ownerDisplayName) {
    lines.push(`Organizer: ${s.ownerDisplayName}`)
  }

  if (s.presenters && s.presenters.length > 0) {
    lines.push(`Presenters: ${s.presenters.map((p) => p.displayName).join(', ')}`)
  } else {
    lines.push('Presenters: None')
  }

  if (s.coordinators && s.coordinators.length > 0) {
    lines.push(`Co-organizers: ${s.coordinators.map((c) => c.displayName).join(', ')}`)
  } else {
    lines.push('Co-organizers: None')
  }

  return lines.join('\n')
}

function formatDateTime(iso: string | null | undefined) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

function formatDelivery(startsAt: string | null | undefined, endsAt: string | null | undefined) {
  if (!startsAt) return { date: '—', time: '', duration: '', tzShort: '', tzTooltip: '' }
  const start = new Date(startsAt)
  const date = start.toLocaleDateString(undefined, { dateStyle: 'medium' })

  const time = start.toLocaleTimeString(undefined, {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  })

  const tzShort =
    start
      .toLocaleTimeString(undefined, { timeZoneName: 'short' })
      .split(' ')
      .pop() ?? ''
  const tzLong = start
    .toLocaleTimeString(undefined, { timeZoneName: 'long' })
    .replace(/^[\d:]+\s*(AM|PM)?\s*/i, '')
  const offMins = -start.getTimezoneOffset()
  const sign = offMins >= 0 ? '+' : '-'
  const h = Math.floor(Math.abs(offMins) / 60)
  const m = Math.abs(offMins) % 60
  const offset = `GMT ${sign}${h}${m > 0 ? `:${String(m).padStart(2, '0')}` : ''}`
  const tzTooltip = `${tzLong} (${offset})`

  let duration = ''
  if (endsAt) {
    const end = new Date(endsAt)
    const diffMins = Math.round((end.getTime() - start.getTime()) / 60000)
    if (diffMins > 0) {
      const hrs = Math.floor(diffMins / 60)
      const mins = diffMins % 60
      if (hrs > 0) duration += `${hrs} hr${hrs > 1 ? 's' : ''}`
      if (mins > 0) duration += `${hrs > 0 ? ' ' : ''}${mins} min`
    }
  }

  return { date, time, duration, tzShort, tzTooltip }
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
  const [seriesTitle, setSeriesTitle] = useState(series.title)

  const sortedSessions = useMemo(
    () => [...sessions].sort((a, b) => new Date(a.startsAt).getTime() - new Date(b.startsAt).getTime()),
    [sessions],
  )

  const [editLoading, setEditLoading] = useState(false)
  const [editError, setEditError] = useState<string | null>(null)
  const busy = sessionStatus === 'loading' || editLoading

  useEffect(() => {
    setSeriesTitle(series.title)
  }, [series.title])

  async function handleTitleSave(nextTitle: string) {
    setEditLoading(true)
    setEditError(null)
    try {
      await updateSeries(series.seriesId, { title: nextTitle }, token)
      setSeriesTitle(nextTitle)
      router.refresh()
    } catch (err) {
      setEditError(err instanceof Error ? err.message : 'Failed to update series')
      throw err
    } finally {
      setEditLoading(false)
    }
  }

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

  const [isExporting, setIsExporting] = useState(false)
  const [exportError, setExportError] = useState<string | null>(null)

  async function handleExportMarkdown() {
    setIsExporting(true)
    setExportError(null)
    try {
      await exportSeriesMarkdown(series.seriesId, token)
    } catch (err) {
      setExportError(err instanceof Error ? err.message : 'Export failed')
    } finally {
      setIsExporting(false)
    }
  }

  const seriesDisplayStatus =
    series.status === 'Published' && series.draftSessionCount > 0
      ? 'Partially Published'
      : series.status

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center gap-3">
            <InlineEditableTitle
              value={seriesTitle}
              onSave={handleTitleSave}
              disabled={busy}
              editAriaLabel="Edit series title"
              saveAriaLabel="Save series title"
              inputAriaLabel="Series title"
              titleClassName="text-2xl font-bold tracking-tight"
            />
            <StatusBadge status={seriesDisplayStatus} />
          </div>
          <p className="text-xs" style={{ color: 'var(--fgColor-muted)' }}>
            Created {formatDateTime(series.createdAt)} · Updated {formatDateTime(series.updatedAt)}
          </p>
        </div>

        <div className="flex items-center gap-2">
          <IconButton
            icon={DownloadIcon}
            aria-label="Download Series Data"
            variant="default"
            onClick={handleExportMarkdown}
            disabled={busy || isExporting}
          />
          <IconButton
            icon={TrashIcon}
            aria-label="Delete series"
            variant="danger"
            onClick={() => {
              setDeleteError(null)
              setDeleteOpen(true)
            }}
            disabled={busy}
          />
        </div>
      </div>

      {deleteError && <ErrorBanner message={deleteError} onRetry={() => setDeleteOpen(true)} />}
      {editError && <ErrorBanner message={editError} />}
      {deleteSessionError && <ErrorBanner message={deleteSessionError} />}
      {exportError && <ErrorBanner message={exportError} onRetry={handleExportMarkdown} />}

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
            <p className="mb-2 text-xs font-medium" style={{ color: 'var(--fgColor-muted)' }}>
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

      <section aria-label="Sessions">
        <div className="mb-3 flex items-center justify-between">
          <h2
            className="text-sm font-semibold uppercase tracking-wide"
            style={{ color: 'var(--fgColor-muted)' }}
          >
            Sessions ({sortedSessions.length})
          </h2>
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

        {sortedSessions.length === 0 ? (
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
                  <th className="px-4 py-3 text-left font-medium">Status</th>
                  <th className="px-4 py-3 text-left font-medium">Delivery</th>
                  <th className="px-4 py-3 text-left font-medium">People</th>
                  <th className="px-4 py-3 text-right font-medium">Reg.</th>
                  <th className="px-4 py-3 text-right font-medium">Att.</th>
                  <th className="px-4 py-3 text-right font-medium w-20">Actions</th>
                </tr>
              </thead>
              <tbody>
                {sortedSessions.map((s, idx) => (
                  <tr
                    key={s.sessionId}
                    onClick={() => router.push(`/sessions/${s.sessionId}`)}
                    className="cursor-pointer transition-colors"
                    style={{
                      borderBottom:
                        idx < sortedSessions.length - 1
                          ? '1px solid var(--borderColor-default)'
                          : undefined,
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.backgroundColor = 'var(--bgColor-muted)'
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.backgroundColor = ''
                    }}
                  >
                    <td className="px-4 py-3 font-medium">{s.title}</td>
                    <td className="px-4 py-3">
                      <StatusBadge status={s.status} />
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap" style={{ color: 'var(--fgColor-muted)' }}>
                      {(() => {
                        const d = formatDelivery(s.startsAt, s.endsAt)
                        return (
                          <div className="leading-tight">
                            <div style={{ fontWeight: 600, color: 'var(--fgColor-default)' }}>{d.date}</div>
                            {d.time && (
                              <div className="text-xs" style={{ color: 'var(--fgColor-muted)' }}>
                                {d.time}
                                {d.duration && <> • {d.duration}</>}
                                {d.tzShort && (
                                  <>
                                    {' '}
                                    •{' '}
                                    <span
                                      title={d.tzTooltip}
                                      style={{ cursor: 'help', textDecoration: 'underline dotted' }}
                                    >
                                      {d.tzShort}
                                    </span>
                                  </>
                                )}
                              </div>
                            )}
                          </div>
                        )
                      })()}
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap" style={{ color: 'var(--fgColor-muted)' }}>
                      <Tooltip text={buildPeopleTooltip(s)} direction="s" type="description">
                        <button
                          type="button"
                          className="inline-flex items-center gap-3 text-xs"
                          style={{
                            cursor: 'default',
                            background: 'none',
                            border: 'none',
                            padding: 0,
                            color: 'inherit',
                            font: 'inherit',
                          }}
                        >
                          <span className="inline-flex items-center gap-1">
                            <PeopleIcon size={14} />
                            {s.presenterCount}
                          </span>
                          <span className="inline-flex items-center gap-1">
                            <OrganizationIcon size={14} />
                            {s.coordinatorCount}
                          </span>
                        </button>
                      </Tooltip>
                    </td>
                    <td className="px-4 py-3 tabular-nums text-right">{s.totalRegistrations}</td>
                    <td className="px-4 py-3 tabular-nums text-right">{s.totalAttendees}</td>
                    <td className="px-4 py-3 text-right" onClick={(e) => e.stopPropagation()}>
                      <div className="flex items-center justify-end gap-1">
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

      <ConfirmDialog
        open={deleteOpen}
        title="Delete Series"
        description="This will delete all sessions. Continue?"
        confirmLabel="Delete Series"
        dangerous
        loading={deleteLoading}
        onConfirm={handleDeleteSeries}
        onCancel={() => setDeleteOpen(false)}
      />

      <ConfirmDialog
        open={deleteSessionId !== null}
        title="Delete Session"
        description="This will permanently delete the session. Continue?"
        confirmLabel="Delete Session"
        dangerous
        loading={deleteSessionLoading}
        onConfirm={handleDeleteSession}
        onCancel={() => setDeleteSessionId(null)}
      />
    </div>
  )
}
