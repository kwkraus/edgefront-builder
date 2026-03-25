'use client'

import { useState, useCallback } from 'react'
import { Button, Spinner } from '@primer/react'
import { ErrorBanner } from '@/components/error-banner'
import { getRegistrationPreview, confirmRegistrationImport } from '@/lib/api/sessions'
import { RegistrationPreviewTable } from './registration-preview-table'
import { RegistrationUploadZone } from './registration-upload-zone'
import type { ParsedRegistrant, RegistrationPreviewDto, SessionImportUploadResponse } from '@/lib/api/types'

interface RegistrationUploadFlowProps {
  sessionId: string
  accessToken: string
  onUploadComplete?: (result: SessionImportUploadResponse) => Promise<void> | void
}

type FlowStep = 'upload' | 'preview' | 'edit' | 'confirming'

export function RegistrationUploadFlow({
  sessionId,
  accessToken,
  onUploadComplete,
}: RegistrationUploadFlowProps) {
  const [step, setStep] = useState<FlowStep>('upload')
  const [previewLoading, setPreviewLoading] = useState(false)
  const [previewData, setPreviewData] = useState<RegistrationPreviewDto | null>(null)
  const [editedRegistrants, setEditedRegistrants] = useState<ParsedRegistrant[]>([])
  const [error, setError] = useState<string | null>(null)
  const [confirming, setConfirming] = useState(false)

  const handleFileSelected = useCallback(async (file: File) => {
    setError(null)
    setStep('preview')
    setPreviewLoading(true)

    try {
      const preview = await getRegistrationPreview(sessionId, file, accessToken)
      setPreviewData(preview)
      setEditedRegistrants(preview.registrants)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to preview registration file'
      setError(message)
      setStep('upload')
    } finally {
      setPreviewLoading(false)
    }
  }, [sessionId, accessToken])

  const handleConfirm = useCallback(async () => {
    if (!editedRegistrants.length) {
      setError('No registrants to import')
      return
    }

    setConfirming(true)
    setError(null)

    try {
      const result = await confirmRegistrationImport(
        sessionId,
        editedRegistrants,
        accessToken,
      )

      // Call the callback to update parent component
      if (onUploadComplete) {
        await onUploadComplete(result)
      }

      // Reset flow
      setStep('upload')
      setPreviewData(null)
      setEditedRegistrants([])
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to import registrations'
      setError(message)
    } finally {
      setConfirming(false)
    }
  }, [editedRegistrants, sessionId, accessToken, onUploadComplete])

  const handleEditRegistrant = useCallback((index: number, updated: ParsedRegistrant) => {
    setEditedRegistrants((prev) => {
      const newList = [...prev]
      newList[index] = updated
      return newList
    })
  }, [])

  const handleCancel = useCallback(() => {
    setStep('upload')
    setPreviewData(null)
    setEditedRegistrants([])
    setError(null)
  }, [])

  if (step === 'upload') {
    return (
      <div className="space-y-3">
        <h3 className="text-sm font-semibold">Upload Registrations</h3>
        <p className="text-sm" style={{ color: 'var(--fgColor-muted, var(--color-fg-muted))' }}>
          Upload a registration CSV export to populate and update registrant information.
          The system will use AI to parse and validate the data before importing.
        </p>
        {error && <ErrorBanner message={error} />}
        <RegistrationUploadZone onFileSelected={handleFileSelected} />
      </div>
    )
  }

  if (step === 'preview') {
    if (previewLoading || !previewData) {
      return (
        <div className="space-y-3">
          <h3 className="text-sm font-semibold">Parsing Registration File…</h3>
          <div className="flex items-center gap-2">
            <Spinner size="small" />
            <p className="text-sm">Analyzing CSV and extracting registrant information…</p>
          </div>
        </div>
      )
    }

    const failedCount = previewData.registrants.filter((r) => r.status === 'failed').length

    return (
      <div className="space-y-4">
        <div className="space-y-2">
          <h3 className="text-sm font-semibold">Review Registration Import</h3>
          <div
            className="rounded-lg border px-4 py-3 text-sm space-y-2"
            style={{
              borderColor: 'var(--borderColor-default, var(--color-border-default))',
              backgroundColor: 'var(--bgColor-muted, var(--color-canvas-subtle))',
            }}
          >
            <div className="flex justify-between">
              <span>Session:</span>
              <strong>{previewData.sessionTitle}</strong>
            </div>
            <div className="flex justify-between">
              <span>Total Registrants:</span>
              <strong>{previewData.registrantCount}</strong>
            </div>
            <div className="flex justify-between">
              <span>Successfully Parsed:</span>
              <strong style={{ color: 'var(--fgColor-success, var(--color-success-fg))' }}>
                {previewData.successCount}
              </strong>
            </div>
            {failedCount > 0 && (
              <div className="flex justify-between">
                <span>Need Manual Review:</span>
                <strong style={{ color: 'var(--fgColor-danger, var(--color-danger-fg))' }}>
                  {failedCount}
                </strong>
              </div>
            )}
          </div>
        </div>

        {error && <ErrorBanner message={error} />}

        {failedCount > 0 && (
          <div
            className="rounded-lg border px-4 py-3 bg-yellow-50 dark:bg-yellow-900/20 text-sm"
            style={{
              borderColor: 'var(--borderColor-attention, var(--color-attention-emphasis))',
              color: 'var(--fgColor-attention, var(--color-attention-fg))',
            }}
          >
            <p className="font-medium mb-2">Rows that need manual review:</p>
            <p>Please review and correct the entries below before confirming the import.</p>
          </div>
        )}

        <RegistrationPreviewTable
          registrants={editedRegistrants}
          onEditRegistrant={handleEditRegistrant}
          showFailedOnly={failedCount > 0}
        />

        <div className="flex gap-2">
          <Button
            variant="primary"
            onClick={handleConfirm}
            disabled={confirming || failedCount > 0}
          >
            {confirming ? (
              <span className="inline-flex items-center gap-2">
                <Spinner size="small" />
                Importing…
              </span>
            ) : (
              'Confirm Import'
            )}
          </Button>
          <Button variant="default" onClick={handleCancel} disabled={confirming}>
            Cancel
          </Button>
        </div>
      </div>
    )
  }

  return null
}
