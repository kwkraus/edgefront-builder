'use client'

import { useState, useRef } from 'react'
import { Button, Spinner, Banner } from '@primer/react'
import { UploadIcon } from '@primer/octicons-react'
import type { ImportResult } from '@/lib/api/types'

interface CsvImportDialogProps {
  open: boolean
  onClose: () => void
  title: string
  onUpload: (file: File) => Promise<ImportResult>
  onSuccess?: () => void
}

export function CsvImportDialog({ open, onClose, title, onUpload, onSuccess }: CsvImportDialogProps) {
  const [file, setFile] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const [result, setResult] = useState<ImportResult | null>(null)
  const [error, setError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  function handleClose() {
    if (result && (result.importedCount > 0 || result.skippedCount > 0)) {
      onSuccess?.()
    }
    setFile(null)
    setResult(null)
    setError(null)
    onClose()
  }

  async function handleUpload() {
    if (!file) return
    setUploading(true)
    setError(null)
    setResult(null)
    try {
      const importResult = await onUpload(file)
      setResult(importResult)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Import failed')
    } finally {
      setUploading(false)
    }
  }

  if (!open) return null

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center"
      style={{ backgroundColor: 'rgba(0,0,0,0.4)' }}
      role="dialog"
      aria-modal="true"
      aria-label={title}
    >
      <div
        className="w-full max-w-lg rounded-lg shadow-lg"
        style={{
          backgroundColor: 'var(--bgColor-default, var(--color-canvas-default))',
          borderWidth: 'var(--borderWidth-thin, 1px)',
          borderStyle: 'solid',
          borderColor: 'var(--borderColor-default, var(--color-border-default))',
        }}
      >
        {/* Header */}
        <div
          className="flex items-center justify-between px-4 py-3"
          style={{
            borderBottomWidth: 'var(--borderWidth-thin, 1px)',
            borderBottomStyle: 'solid',
            borderBottomColor: 'var(--borderColor-default, var(--color-border-default))',
          }}
        >
          <h2 className="text-base font-semibold">{title}</h2>
          <button
            onClick={handleClose}
            className="rounded p-1 text-lg leading-none transition-colors focus:outline-none focus-visible:ring-2"
            style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}
            aria-label="Close dialog"
          >
            ×
          </button>
        </div>

        {/* Body */}
        <div className="space-y-4 px-4 py-4">
          {!result && !error && (
            <>
              <div className="space-y-2">
                <label
                  htmlFor="csv-file-input"
                  className="block text-sm font-medium"
                  style={{ color: 'var(--fgColor-default, var(--color-fg-default))' }}
                >
                  Select a CSV file
                </label>
                <input
                  id="csv-file-input"
                  ref={fileInputRef}
                  type="file"
                  accept=".csv,text/csv"
                  onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                  disabled={uploading}
                  className="block w-full text-sm file:mr-3 file:rounded file:border-0 file:px-3 file:py-1.5 file:text-sm file:font-medium"
                  style={{ color: 'var(--fgColor-default, var(--color-fg-default))' }}
                />
                {file && (
                  <p className="text-xs" style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}>
                    {file.name} ({(file.size / 1024).toFixed(1)} KB)
                  </p>
                )}
              </div>

              {uploading && (
                <div className="flex items-center gap-2" aria-live="polite" aria-busy="true">
                  <Spinner size="small" />
                  <span className="text-sm" style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}>
                    Importing…
                  </span>
                </div>
              )}
            </>
          )}

          {error && (
            <Banner variant="critical" title="Import failed">
              {error}
            </Banner>
          )}

          {result && (
            <div className="space-y-3">
              {result.invalidCount === 0 && result.importedCount > 0 && (
                <Banner variant="info" title="Import complete">
                  All rows imported successfully.
                </Banner>
              )}
              {result.invalidCount > 0 && result.importedCount > 0 && (
                <Banner variant="warning" title="Partial import">
                  Some rows could not be imported. Valid rows were saved.
                </Banner>
              )}
              {result.invalidCount > 0 && result.importedCount === 0 && result.skippedCount === 0 && (
                <Banner variant="critical" title="Import failed">
                  No rows could be imported. Please check the errors below.
                </Banner>
              )}
              {result.importedCount === 0 && result.skippedCount > 0 && result.invalidCount === 0 && (
                <Banner variant="info" title="No new data">
                  All rows already exist. No changes were made.
                </Banner>
              )}

              <dl className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
                <dt style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}>Total rows</dt>
                <dd className="font-medium">{result.totalRows}</dd>
                <dt style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}>Imported</dt>
                <dd className="font-medium">{result.importedCount}</dd>
                <dt style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}>Skipped (duplicates)</dt>
                <dd className="font-medium">{result.skippedCount}</dd>
                <dt style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}>Invalid</dt>
                <dd className="font-medium">{result.invalidCount}</dd>
              </dl>

              {result.errors.length > 0 && (
                <div className="space-y-2">
                  <p className="text-xs font-medium" style={{ color: 'var(--fgColor-danger, var(--color-danger-fg))' }}>
                    Row errors:
                  </p>
                  <div
                    className="max-h-40 overflow-y-auto rounded text-xs"
                    style={{
                      backgroundColor: 'var(--bgColor-muted, var(--color-canvas-subtle))',
                      padding: 'var(--base-size-8, 8px)',
                    }}
                  >
                    {result.errors.map((e) => (
                      <div key={e.row} className="flex gap-2 py-0.5">
                        <span className="font-mono font-medium" style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}>
                          Row {e.row}:
                        </span>
                        <span>{e.reason}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Footer */}
        <div
          className="flex justify-end gap-2 px-4 py-3"
          style={{
            borderTopWidth: 'var(--borderWidth-thin, 1px)',
            borderTopStyle: 'solid',
            borderTopColor: 'var(--borderColor-default, var(--color-border-default))',
          }}
        >
          {!result && !error && (
            <>
              <Button onClick={handleClose} disabled={uploading}>
                Cancel
              </Button>
              <Button
                variant="primary"
                leadingVisual={UploadIcon}
                onClick={handleUpload}
                disabled={!file || uploading}
              >
                Import
              </Button>
            </>
          )}
          {(result || error) && (
            <>
              {error && (
                <Button
                  onClick={() => {
                    setError(null)
                    setResult(null)
                  }}
                >
                  Try Again
                </Button>
              )}
              <Button variant="primary" onClick={handleClose}>
                Done
              </Button>
            </>
          )}
        </div>
      </div>
    </div>
  )
}
