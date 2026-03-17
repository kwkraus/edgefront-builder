'use client'

import { useMemo, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useSession } from 'next-auth/react'
import { PencilIcon, PlusIcon, TrashIcon } from '@primer/octicons-react'
import { Button, Dialog, FormControl, IconButton, TextInput, Token } from '@primer/react'
import { ConfirmDialog } from '@/components/confirm-dialog'
import { ErrorBanner } from '@/components/error-banner'
import { MetricsPanel } from '@/components/metrics-panel'
import { deleteSession } from '@/lib/api/sessions'
import { deleteSeries, updateSeries } from '@/lib/api/series'
import type { SeriesMetricsResponse, SeriesResponse, SessionListItem } from '@/lib/api/types'
import {
  formatDateTime,
  formatSessionSchedule,
  getImportRowCount,
  getImportSummary,
  getLatestImportInfo,
  sessionImportLabels,
} from '@/lib/session-analytics'

interface Props {
  series: SeriesResponse
  sessions: SessionListItem[]
  metrics: SeriesMetricsResponse | null
}

export default function SeriesDetailView({ series, sessions, metrics }: Props) {
  const { data: authSession, status: sessionStatus } = useSession()
  const router = useRouter()
  const accessToken = authSession?.accessToken ?? ''
  const busy = sessionStatus === 'loading'

  const [editOpen, setEditOpen] = useState(false)
  const [editTitle, setEditTitle] = useState(series.title)
  const [editLoading, setEditLoading] = useState(false)
  const [editError, setEditError] = useState<string | null>(null)

  const [deleteOpen, setDeleteOpen] = useState(false)
  const [deleteLoading, setDeleteLoading] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const [deleteSessionId, setDeleteSessionId] = useState<string | null>(null)
  const [deleteSessionLoading, setDeleteSessionLoading] = useState(false)
  const [deleteSessionError, setDeleteSessionError] = useState<string | null>(null)

  const sessionCoverage = useMemo(() => {
    return {
      registrations: sessions.filter((session) => {
        const summary = getImportSummary(session.imports, 'registrations')
        return Boolean(summary && getImportRowCount(summary) !== null)
      }).length,
      attendance: sessions.filter((session) => {
        const summary = getImportSummary(session.imports, 'attendance')
        return Boolean(summary && getImportRowCount(summary) !== null)
      }).length,
      qa: sessions.filter((session) => {
        const summary = getImportSummary(session.imports, 'qa')
        return Boolean(summary && getImportRowCount(summary) !== null)
      }).length,
    }
  }, [sessions])

  async function handleEditSubmit(event: React.FormEvent) {
    event.preventDefault()
    if (!editTitle.trim()) return

    setEditLoading(true)
    setEditError(null)

    try {
      await updateSeries(series.seriesId, { title: editTitle.trim() }, accessToken)
      setEditOpen(false)
      router.refresh()
    } catch (error) {
      setEditError(error instanceof Error ? error.message : 'Failed to update series.')
    } finally {
      setEditLoading(false)
    }
  }

  async function handleDeleteSeries() {
    setDeleteLoading(true)
    setDeleteError(null)

    try {
      await deleteSeries(series.seriesId, accessToken)
      router.push('/series')
    } catch (error) {
      setDeleteError(error instanceof Error ? error.message : 'Failed to delete series.')
      setDeleteOpen(false)
      setDeleteLoading(false)
    }
  }

  async function handleDeleteSession() {
    if (!deleteSessionId) return

    setDeleteSessionLoading(true)
    setDeleteSessionError(null)

    try {
      await deleteSession(deleteSessionId, accessToken)
      setDeleteSessionId(null)
      router.refresh()
    } catch (error) {
      setDeleteSessionError(error instanceof Error ? error.message : 'Failed to delete session.')
      setDeleteSessionId(null)
      setDeleteSessionLoading(false)
    }
  }

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1.5">
          <div className="flex items-center gap-3">
            <h1 className="text-2xl font-bold tracking-tight">{series.title}</h1>
            <IconButton
              icon={PencilIcon}
              aria-label="Edit series title"
              variant="invisible"
              size="small"
              onClick={() => {
                setEditTitle(series.title)
                setEditError(null)
                setEditOpen(true)
              }}
              disabled={busy}
            />
          </div>
          <p
            className="text-sm"
            style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
          >
            Local-only series summary with session analytics and CSV import coverage.
          </p>
          <p
            className="text-xs"
            style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
          >
            Created {formatDateTime(series.createdAt)} · Updated {formatDateTime(series.updatedAt)}
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            as={Link}
            href={`/series/${series.seriesId}/sessions/new`}
            variant="primary"
            leadingVisual={PlusIcon}
          >
            Add session
          </Button>
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
      </header>

      {deleteError && <ErrorBanner message={deleteError} />}
      {deleteSessionError && <ErrorBanner message={deleteSessionError} />}

      <section
        aria-labelledby="series-analytics-heading"
        className="space-y-4 rounded-lg border p-6"
        style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}
      >
        <div className="space-y-1">
          <h2 id="series-analytics-heading" className="text-base font-semibold">
            Analytics overview
          </h2>
          <p
            className="text-sm"
            style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
          >
            Series-level metrics summarize the CSV data attached to each local session.
          </p>
        </div>

        <MetricsPanel
          metrics={[
            { label: 'Sessions', value: sessions.length },
            { label: 'Registrations', value: metrics?.totalRegistrations ?? 0 },
            { label: 'Attendees', value: metrics?.totalAttendees ?? 0 },
            { label: 'Q&A', value: metrics?.totalQaQuestions ?? 0 },
            { label: 'Accts Influenced', value: metrics?.uniqueAccountsInfluenced ?? 0 },
          ]}
        />

        <div className="flex flex-wrap gap-2">
          <Token text={`Registrations imported for ${sessionCoverage.registrations}/${sessions.length} sessions`} />
          <Token text={`Attendance imported for ${sessionCoverage.attendance}/${sessions.length} sessions`} />
          <Token text={`Q&A imported for ${sessionCoverage.qa}/${sessions.length} sessions`} />
        </div>

        {metrics && metrics.warmAccounts.length > 0 && (
          <div className="space-y-2">
            <h3 className="text-sm font-semibold">Warm accounts</h3>
            <div className="flex flex-wrap gap-2">
              {metrics.warmAccounts.map((entry) => (
                <Token
                  key={`${entry.accountDomain}-${entry.warmRule}`}
                  text={
                    <>
                      {entry.accountDomain}{' '}
                      <span style={{ color: 'var(--fgColor-accent, var(--color-accent-fg))' }}>
                        {entry.warmRule}
                      </span>
                    </>
                  }
                />
              ))}
            </div>
          </div>
        )}
      </section>

      <section aria-labelledby="series-sessions-heading" className="space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="space-y-1">
            <h2 id="series-sessions-heading" className="text-base font-semibold">
              Sessions
            </h2>
            <p
              className="text-sm"
              style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
            >
              Manage local session definitions and inspect import coverage at a glance.
            </p>
          </div>
        </div>

        {sessions.length === 0 ? (
          <div
            className="rounded-lg border px-8 py-12 text-center"
            style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}
          >
            <p
              className="text-sm"
              style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
            >
              No sessions yet. Add a session to start defining local analytics inputs.
            </p>
          </div>
        ) : (
          <div
            className="overflow-x-auto rounded-lg border"
            style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}
          >
            <table className="min-w-full text-sm">
              <thead>
                <tr
                  className="text-xs uppercase tracking-wide"
                  style={{
                    color: 'var(--fgColor-muted, var(--color-fg-muted))',
                    backgroundColor: 'var(--bgColor-muted, var(--color-canvas-subtle))',
                    borderBottom: '1px solid var(--borderColor-default, var(--color-border-default))',
                  }}
                >
                  <th className="px-4 py-3 text-left font-medium">Title</th>
                  <th className="px-4 py-3 text-left font-medium">Schedule</th>
                  <th className="px-4 py-3 text-left font-medium">Latest import</th>
                  <th className="px-4 py-3 text-right font-medium">Reg.</th>
                  <th className="px-4 py-3 text-right font-medium">Att.</th>
                  <th className="px-4 py-3 text-right font-medium">Q&amp;A rows</th>
                  <th className="px-4 py-3 text-right font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {sessions.map((session, index) => {
                  const schedule = formatSessionSchedule(session.startsAt, session.endsAt)
                  const latestImport = getLatestImportInfo(session.imports)
                  const qaSummary = getImportSummary(session.imports, 'qa')
                  const qaRows = getImportRowCount(qaSummary)

                  return (
                    <tr
                      key={session.sessionId}
                      style={{
                        borderBottom:
                          index < sessions.length - 1
                            ? '1px solid var(--borderColor-default, var(--color-border-default))'
                            : undefined,
                      }}
                    >
                      <td className="px-4 py-3 align-top">
                        <Link
                          href={`/sessions/${session.sessionId}`}
                          className="font-medium hover:underline"
                        >
                          {session.title}
                        </Link>
                      </td>
                      <td className="px-4 py-3 align-top">
                        <div>
                          <div className="font-medium">{schedule.date}</div>
                          {schedule.time && (
                            <div
                              className="text-xs"
                              style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
                            >
                              {schedule.time}
                              {schedule.duration && <> · {schedule.duration}</>}
                              {schedule.tzShort && (
                                <>
                                  {' '}
                                  ·{' '}
                                  <span
                                    title={schedule.tzTooltip}
                                    style={{ textDecoration: 'underline dotted' }}
                                  >
                                    {schedule.tzShort}
                                  </span>
                                </>
                              )}
                            </div>
                          )}
                        </div>
                      </td>
                      <td className="px-4 py-3 align-top">
                        {latestImport ? (
                          <div>
                            <div className="font-medium">
                              {sessionImportLabels[latestImport.importType]}
                            </div>
                            <div
                              className="text-xs"
                              style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
                            >
                              {formatDateTime(latestImport.importedAt)}
                              {latestImport.fileName ? ` · ${latestImport.fileName}` : ''}
                            </div>
                          </div>
                        ) : (
                          <span
                            style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
                          >
                            No CSVs yet
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-right tabular-nums align-top">
                        {session.totalRegistrations}
                      </td>
                      <td className="px-4 py-3 text-right tabular-nums align-top">
                        {session.totalAttendees}
                      </td>
                      <td className="px-4 py-3 text-right tabular-nums align-top">
                        {typeof qaRows === 'number' ? qaRows.toLocaleString() : '—'}
                      </td>
                      <td className="px-4 py-3 text-right align-top">
                        <div className="inline-flex items-center gap-2">
                          <Button
                            as={Link}
                            href={`/sessions/${session.sessionId}`}
                            size="small"
                            variant="default"
                          >
                            Open
                          </Button>
                          <IconButton
                            icon={TrashIcon}
                            aria-label={`Delete ${session.title}`}
                            variant="invisible"
                            onClick={() => {
                              setDeleteSessionError(null)
                              setDeleteSessionId(session.sessionId)
                            }}
                          />
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {editOpen && (
        <Dialog
          title="Edit series title"
          onClose={() => setEditOpen(false)}
          footerButtons={[
            { buttonType: 'default', content: 'Cancel', onClick: () => setEditOpen(false) },
            {
              buttonType: 'primary',
              content: editLoading ? 'Saving…' : 'Save',
              onClick: () => {
                const form = document.getElementById('edit-series-form')
                form?.dispatchEvent(new Event('submit', { cancelable: true, bubbles: true }))
              },
              disabled: editLoading || !editTitle.trim(),
            },
          ]}
        >
          <form id="edit-series-form" onSubmit={handleEditSubmit} className="space-y-3">
            <FormControl required>
              <FormControl.Label>Title</FormControl.Label>
              <TextInput
                value={editTitle}
                onChange={(event) => setEditTitle(event.target.value)}
                block
                autoFocus
              />
            </FormControl>
            {editError && (
              <p
                className="text-sm"
                style={{ color: 'var(--fgColor-danger, var(--color-danger-fg))' }}
              >
                {editError}
              </p>
            )}
          </form>
        </Dialog>
      )}

      <ConfirmDialog
        open={deleteOpen}
        title="Delete series"
        description="This will permanently delete the series and all local sessions attached to it. Continue?"
        confirmLabel="Delete series"
        dangerous
        loading={deleteLoading}
        onConfirm={handleDeleteSeries}
        onCancel={() => setDeleteOpen(false)}
      />

      <ConfirmDialog
        open={deleteSessionId !== null}
        title="Delete session"
        description="This will permanently delete the session definition and any CSV imports attached to it. Continue?"
        confirmLabel="Delete session"
        dangerous
        loading={deleteSessionLoading}
        onConfirm={handleDeleteSession}
        onCancel={() => setDeleteSessionId(null)}
      />
    </div>
  )
}
