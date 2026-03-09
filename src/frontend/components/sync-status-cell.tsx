'use client'

import { useState, useEffect } from 'react'
import {
  CheckCircleFillIcon,
  DotFillIcon,
  SyncIcon,
  AlertFillIcon,
} from '@primer/octicons-react'
import { IconButton, Tooltip } from '@primer/react'
import type { SyncState } from '@/hooks/use-teams-sync'

interface SyncStatusCellProps {
  sessionId: string
  status: 'Draft' | 'Published'
  syncState: SyncState
  onSync: (sessionId: string) => void
}

export function SyncStatusCell({ sessionId, status, syncState, onSync }: SyncStatusCellProps) {
  const [hovered, setHovered] = useState(false)
  const [focused, setFocused] = useState(false)

  // Reset hover/focus when sync starts — the pulse animation replaces the
  // hoverable element so onMouseLeave never fires naturally.
  useEffect(() => {
    if (syncState === 'syncing') {
      setHovered(false)
      setFocused(false)
    }
  }, [syncState])

  function handleClick(e: React.MouseEvent) {
    e.stopPropagation()
    onSync(sessionId)
  }

  // Syncing: always show pulse animation
  if (syncState === 'syncing') {
    return (
      <span title="Syncing with Teams…" className="inline-flex items-center justify-center">
        <span className="sync-pulse" />
      </span>
    )
  }

  // Draft sessions: static gray dot, no hover interaction
  if (status !== 'Published') {
    return (
      <span title="Draft" className="inline-flex items-center justify-center" style={{ color: 'var(--fgColor-muted)' }}>
        <DotFillIcon size={16} />
      </span>
    )
  }

  // Published sessions (idle, done, or error): hover or focus reveals sync icon
  const isError = syncState === 'error'
  const showSyncButton = hovered || focused

  return (
    <span
      className="inline-flex items-center justify-center"
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
      onFocus={() => setFocused(true)}
      onBlur={() => setFocused(false)}
    >
      {showSyncButton ? (
        <Tooltip
          text={isError ? 'Sync failed — click to retry' : 'Sync from Teams'}
          direction="e"
          type="label"
        >
          <IconButton
            icon={SyncIcon}
            aria-label={isError ? `Retry sync for session` : `Sync session from Teams`}
            variant="invisible"
            size="small"
            onClick={handleClick}
            className="sync-icon-reveal"
            style={{ color: 'var(--fgColor-accent, #0969da)' }}
          />
        </Tooltip>
      ) : isError ? (
        <span title="Sync failed" className="inline-flex items-center justify-center" style={{ color: 'var(--fgColor-danger, #d1242f)' }}>
          <AlertFillIcon size={16} />
        </span>
      ) : (
        <span title="Published — hover or focus to sync" className="inline-flex items-center justify-center" style={{ color: 'var(--fgColor-success, #1a7f37)' }}>
          <CheckCircleFillIcon size={16} />
        </span>
      )}
    </span>
  )
}
