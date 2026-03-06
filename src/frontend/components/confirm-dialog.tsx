'use client'

import { ConfirmationDialog } from '@primer/react'

interface ConfirmDialogProps {
  open: boolean
  title: string
  description: string
  confirmLabel?: string
  cancelLabel?: string
  dangerous?: boolean
  loading?: boolean
  onConfirm: () => void
  onCancel: () => void
}

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  dangerous = false,
  loading = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  if (!open) return null

  return (
    <ConfirmationDialog
      title={title}
      onClose={(gesture) => {
        if (loading) return
        if (gesture === 'confirm') {
          onConfirm()
        } else {
          onCancel()
        }
      }}
      confirmButtonContent={confirmLabel}
      confirmButtonType={dangerous ? 'danger' : 'normal'}
      cancelButtonContent={cancelLabel}
    >
      {description}
    </ConfirmationDialog>
  )
}
