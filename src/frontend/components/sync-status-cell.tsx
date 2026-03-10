'use client'

import { useEffect, useRef } from 'react'
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
  const containerRef = useRef<HTMLSpanElement>(null)

  // When sync starts the pulse replaces the hover target, so clear any
  // lingering :hover via a tiny unmount/remount isn't needed — CSS :hover
  // handles this naturally because the container stays in the DOM.
  // However, the IconButton may retain focus; blur it on sync start.
  useEffect(() => {
    if (syncState === 'syncing') {
      const active = document.activeElement
      if (active instanceof HTMLElement && containerRef.current?.contains(active)) {
        active.blur()
      }
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

  // Published sessions: both icons always in DOM, CSS :hover swaps visibility.
  // This avoids React re-render timing issues with fast mouse movement.
  const isError = syncState === 'error'

  return (
    <span ref={containerRef} className="sync-hover-cell inline-flex items-center justify-center relative">
      {/* Default icon — visible when not hovered */}
      <span className="sync-hover-default inline-flex items-center justify-center" style={{ color: isError ? 'var(--fgColor-danger, #d1242f)' : 'var(--fgColor-success, #1a7f37)' }}>
        {isError ? <AlertFillIcon size={16} /> : <CheckCircleFillIcon size={16} />}
      </span>
      {/* Sync button — visible on hover/focus-within */}
      <span className="sync-hover-action absolute inset-0 inline-flex items-center justify-center">
        <Tooltip
          text={isError ? 'Sync failed — click to retry' : 'Sync from Teams'}
          direction="e"
          type="label"
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
