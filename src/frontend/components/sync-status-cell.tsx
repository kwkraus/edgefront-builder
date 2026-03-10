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

  // Reset hover when sync starts — the pulse replaces this entire subtree.
  useEffect(() => {
    if (syncState === 'syncing') setHovered(false)
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

  // Published sessions: BOTH icons always in DOM, toggle via inline styles.
  // Stable DOM avoids missed mouseLeave events during fast mouse movement.
  const isError = syncState === 'error'

  return (
    <span
      className="inline-flex items-center justify-center"
      style={{ position: 'relative', minWidth: 28, minHeight: 28 }}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
    >
      {/* Default icon — hidden when hovered */}
      <span
        className="inline-flex items-center justify-center"
        style={{
          color: isError ? 'var(--fgColor-danger, #d1242f)' : 'var(--fgColor-success, #1a7f37)',
          opacity: hovered ? 0 : 1,
          transition: 'opacity 0.1s ease',
        }}
      >
        {isError ? <AlertFillIcon size={16} /> : <CheckCircleFillIcon size={16} />}
      </span>
      {/* Sync button — shown when hovered, layered on top */}
      <span
        className="inline-flex items-center justify-center"
        style={{
          position: 'absolute',
          inset: 0,
          opacity: hovered ? 1 : 0,
          pointerEvents: hovered ? 'auto' : 'none',
          transition: 'opacity 0.1s ease',
        }}
      >
        <Tooltip
          text={isError ? 'Sync failed — click to retry' : 'Sync from Teams'}
          direction="e"
          type="description"
        >
          <IconButton
            icon={SyncIcon}
            aria-label={isError ? 'Retry sync for session' : 'Sync session from Teams'}
            variant="invisible"
            size="small"
            onClick={handleClick}
            tabIndex={-1}
            style={{ color: 'var(--fgColor-accent, #0969da)' }}
          />
        </Tooltip>
      </span>
    </span>
  )
}
