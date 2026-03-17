'use client'

import { useId, useMemo, useState } from 'react'
import { Button, Spinner } from '@primer/react'
import {
  uploadSessionAttendanceCsv,
  uploadSessionQaCsv,
  uploadSessionRegistrationsCsv,
} from '@/lib/api/sessions'
import type {
  SessionImportType,
  SessionImports,
  SessionImportUploadResponse,
} from '@/lib/api/types'
import {
  formatDateTime,
  getImportFileName,
  getImportImportedAt,
  getImportRowCount,
  getImportSummary,
  sessionImportLabels,
} from '@/lib/session-analytics'

interface SessionImportsSectionProps {
  sessionId: string
  imports: SessionImports | null | undefined
  accessToken: string
  onUploadComplete?: (result: SessionImportUploadResponse) => Promise<void> | void
}

interface UploadState {
  file: File | null
  uploading: boolean
  error: string | null
}

const importDescriptions: Record<SessionImportType, string> = {
  registrations: 'Upload the session registration export to update registration analytics.',
  attendance: 'Upload the session attendance export to refresh attendee counts and reach.',
  qa: 'Upload the session Q&A export to surface engagement and question analytics.',
}

const uploaders: Record<
  SessionImportType,
  (sessionId: string, file: File, accessToken: string) => Promise<SessionImportUploadResponse>
> = {
  registrations: uploadSessionRegistrationsCsv,
  attendance: uploadSessionAttendanceCsv,
  qa: uploadSessionQaCsv,
}

function createInitialState(): Record<SessionImportType, UploadState> {
  return {
    registrations: { file: null, uploading: false, error: null },
    attendance: { file: null, uploading: false, error: null },
    qa: { file: null, uploading: false, error: null },
  }
}

export function SessionImportsSection({
  sessionId,
  imports,
  accessToken,
  onUploadComplete,
}: SessionImportsSectionProps) {
  const [uploadState, setUploadState] = useState<Record<SessionImportType, UploadState>>(
    createInitialState,
  )
  const baseId = useId()

  const cards = useMemo(
    () =>
      (Object.keys(sessionImportLabels) as SessionImportType[]).map((importType) => ({
        importType,
        label: sessionImportLabels[importType],
        description: importDescriptions[importType],
      })),
    [],
  )

  function updateUploadState(
    importType: SessionImportType,
    nextState: Partial<UploadState>,
  ) {
    setUploadState((current) => ({
      ...current,
      [importType]: {
        ...current[importType],
        ...nextState,
      },
    }))
  }

  async function handleUpload(importType: SessionImportType) {
    const selectedFile = uploadState[importType].file
    if (!selectedFile) {
      updateUploadState(importType, { error: 'Choose a CSV file before uploading.' })
      return
    }

    updateUploadState(importType, { uploading: true, error: null })

    try {
      const result = await uploaders[importType](sessionId, selectedFile, accessToken)
      updateUploadState(importType, { file: null, uploading: false, error: null })
      await onUploadComplete?.(result)
    } catch (error) {
      updateUploadState(importType, {
        uploading: false,
        error: error instanceof Error ? error.message : 'Upload failed.',
      })
    }
  }

  return (
    <section
      aria-labelledby={`${baseId}-heading`}
      className="space-y-4 rounded-lg border p-6"
      style={{ backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))' }}
    >
      <div className="space-y-1">
        <h2 id={`${baseId}-heading`} className="text-base font-semibold">
          Data imports
        </h2>
        <p
          className="text-sm"
          style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
        >
          CSV uploads attach to this session only. Import registrations, attendance, and Q&A
          files independently.
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 xl:grid-cols-3">
        {cards.map(({ importType, label, description }) => {
          const summary = getImportSummary(imports, importType)
          const importedAt = getImportImportedAt(summary)
          const fileName = getImportFileName(summary)
          const rowCount = getImportRowCount(summary)
          const state = uploadState[importType]
          const inputId = `${baseId}-${importType}`

          return (
            <section
              key={importType}
              aria-labelledby={`${inputId}-label`}
              className="space-y-4 rounded-lg border p-4"
              style={{
                backgroundColor: 'var(--bgColor-muted, var(--color-canvas-subtle))',
                borderColor: 'var(--borderColor-default, var(--color-border-default))',
              }}
            >
              <div className="space-y-1">
                <h3 id={`${inputId}-label`} className="text-sm font-semibold">
                  {label}
                </h3>
                <p
                  className="text-sm"
                  style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
                >
                  {description}
                </p>
              </div>

              <dl className="grid grid-cols-1 gap-2 text-sm sm:grid-cols-3 xl:grid-cols-1">
                <div>
                  <dt
                    className="text-xs font-medium uppercase tracking-wide"
                    style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
                  >
                    Last import
                  </dt>
                  <dd>{formatDateTime(importedAt)}</dd>
                </div>
                <div>
                  <dt
                    className="text-xs font-medium uppercase tracking-wide"
                    style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
                  >
                    File
                  </dt>
                  <dd className="break-words">{fileName ?? 'No CSV imported yet'}</dd>
                </div>
                <div>
                  <dt
                    className="text-xs font-medium uppercase tracking-wide"
                    style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
                  >
                    Rows
                  </dt>
                  <dd>{typeof rowCount === 'number' ? rowCount.toLocaleString() : '—'}</dd>
                </div>
              </dl>

              <div className="space-y-2">
                <label htmlFor={inputId} className="block text-sm font-medium">
                  Choose CSV
                </label>
                <input
                  id={inputId}
                  type="file"
                  accept=".csv,text/csv"
                  disabled={state.uploading}
                  onChange={(event) => {
                    updateUploadState(importType, {
                      file: event.target.files?.[0] ?? null,
                      error: null,
                    })
                  }}
                  className="block w-full text-sm"
                />
                <p
                  className="text-xs"
                  style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
                >
                  Uploads replace the current {label.toLowerCase()} dataset for this session.
                </p>
                {state.file && (
                  <p className="text-sm" role="status">
                    Ready to upload: <span className="font-medium">{state.file.name}</span>
                  </p>
                )}
                {state.error && (
                  <p
                    role="alert"
                    className="text-sm"
                    style={{ color: 'var(--fgColor-danger, var(--color-danger-fg))' }}
                  >
                    {state.error}
                  </p>
                )}
              </div>

              <Button
                type="button"
                variant="primary"
                disabled={!state.file || state.uploading}
                onClick={() => handleUpload(importType)}
              >
                {state.uploading ? (
                  <span className="inline-flex items-center gap-2">
                    <Spinner size="small" />
                    Uploading…
                  </span>
                ) : (
                  `Upload ${label} CSV`
                )}
              </Button>

              <div
                className="rounded-md border px-3 py-3 text-sm"
                style={{
                  backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))',
                  borderColor: 'var(--borderColor-default, var(--color-border-default))',
                }}
              >
                <p className="font-medium">
                  {importedAt ? 'Latest import summary' : 'No import summary yet'}
                </p>
                <p style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}>
                  {importedAt
                    ? `${formatDateTime(importedAt)}${typeof rowCount === 'number' ? ` · ${rowCount.toLocaleString()} rows` : ''}`
                    : `Import a ${label.toLowerCase()} CSV to populate analytics.`}
                </p>
              </div>
            </section>
          )
        })}
      </div>
    </section>
  )
}
