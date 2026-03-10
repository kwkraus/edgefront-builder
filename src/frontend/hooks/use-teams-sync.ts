'use client'

import { useState, useCallback, useRef, useEffect } from 'react'
import { syncSession } from '@/lib/api/sessions'

export type SyncState = 'idle' | 'syncing' | 'done' | 'error'

export interface SyncableSession {
  sessionId: string
  status: 'Draft' | 'Published'
  teamsWebinarId: string | null
  lastSyncAt: string | null
}

interface UseTeamsSyncOptions {
  accessToken: string
  onSyncComplete?: () => void
  /** Per-session timeout in milliseconds. Default: 30 000 (30s). */
  timeoutMs?: number
}

const STALE_MS = 15 * 60 * 1000 // 15 minutes
const DEFAULT_TIMEOUT_MS = 30_000

// Must remain greater than the longest CSS sync animation:
//   sync-row-done  → 0.8 s  (sync-row-highlight keyframe in globals.css)
//   sync-cell-reveal → 0.6 s
const SYNC_DONE_CLEAR_MS = 900

function isSyncStale(lastSyncAt: string | null | undefined): boolean {
  if (!lastSyncAt) return true
  return Date.now() - new Date(lastSyncAt).getTime() > STALE_MS
}

function isPublishedWithWebinar(s: SyncableSession): boolean {
  return s.status === 'Published' && s.teamsWebinarId !== null
}

export function useTeamsSync({ accessToken, onSyncComplete, timeoutMs = DEFAULT_TIMEOUT_MS }: UseTeamsSyncOptions) {
  const [syncStates, setSyncStates] = useState<Map<string, SyncState>>(new Map())
  const abortRef = useRef<AbortController | null>(null)
  const hasSynced = useRef(false)
  const clearTimers = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map())

  const isSyncing = Array.from(syncStates.values()).some((s) => s === 'syncing')

  const getSyncState = useCallback(
    (sessionId: string): SyncState => syncStates.get(sessionId) ?? 'idle',
    [syncStates],
  )

  const updateState = useCallback((sessionId: string, state: SyncState) => {
    setSyncStates((prev) => {
      const next = new Map(prev)
      next.set(sessionId, state)
      return next
    })

    // Auto-clear 'done' state after animation completes
    if (state === 'done') {
      const existing = clearTimers.current.get(sessionId)
      if (existing) clearTimeout(existing)
      const timer = setTimeout(() => {
        clearTimers.current.delete(sessionId)
        setSyncStates((prev) => {
          const next = new Map(prev)
          if (next.get(sessionId) === 'done') next.set(sessionId, 'idle')
          return next
        })
      }, SYNC_DONE_CLEAR_MS)
      clearTimers.current.set(sessionId, timer)
    }
  }, [])

  const syncOne = useCallback(
    async (sessionId: string, signal?: AbortSignal) => {
      updateState(sessionId, 'syncing')
      const timeoutCtrl = new AbortController()
      const timer = setTimeout(() => timeoutCtrl.abort(), timeoutMs)

      // Combine the per-session timeout with the shared cancel signal.
      // AbortSignal.any is not universally available; fall back to a manual forwarder.
      let combinedSignal: AbortSignal
      if (signal) {
        if (typeof AbortSignal.any === 'function') {
          combinedSignal = AbortSignal.any([signal, timeoutCtrl.signal])
        } else {
          const combined = new AbortController()
          const forward = () => {
            combined.abort()
            // Clean up whichever listener did not fire
            signal.removeEventListener('abort', forward)
            timeoutCtrl.signal.removeEventListener('abort', forward)
          }
          signal.addEventListener('abort', forward, { once: true })
          timeoutCtrl.signal.addEventListener('abort', forward, { once: true })
          combinedSignal = combined.signal
        }
      } else {
        combinedSignal = timeoutCtrl.signal
      }

      try {
        await syncSession(sessionId, accessToken, combinedSignal)
        updateState(sessionId, 'done')
      } catch {
        // Treat user-initiated cancellation as idle (not an error).
        // Only mark error for non-abort failures (e.g. network/server errors).
        if (signal?.aborted) {
          updateState(sessionId, 'idle')
        } else {
          updateState(sessionId, 'error')
        }
      } finally {
        clearTimeout(timer)
      }
    },
    [accessToken, timeoutMs, updateState],
  )

  const syncAll = useCallback(
    async (sessions: SyncableSession[]) => {
      const eligible = sessions.filter(isPublishedWithWebinar)
      if (eligible.length === 0) return

      // Cancel any in-flight syncs
      abortRef.current?.abort()
      const controller = new AbortController()
      abortRef.current = controller

      await Promise.allSettled(
        eligible.map((s) => syncOne(s.sessionId, controller.signal)),
      )

      if (!controller.signal.aborted) {
        onSyncComplete?.()
      }
    },
    [syncOne, onSyncComplete],
  )

  const cancelAll = useCallback(() => {
    abortRef.current?.abort()
    abortRef.current = null
    setSyncStates((prev) => {
      const next = new Map(prev)
      for (const [id, state] of next) {
        if (state === 'syncing') next.set(id, 'idle')
      }
      return next
    })
  }, [])

  const autoSyncIfStale = useCallback(
    (sessions: SyncableSession[]) => {
      if (hasSynced.current) return
      hasSynced.current = true

      const eligible = sessions.filter(isPublishedWithWebinar)
      if (eligible.length === 0) return
      const needsSync = eligible.some((s) => isSyncStale(s.lastSyncAt))
      if (!needsSync) return

      syncAll(sessions)
    },
    [syncAll],
  )

  // Cleanup on unmount
  useEffect(() => {
    const timers = clearTimers.current
    return () => {
      abortRef.current?.abort()
      for (const timer of timers.values()) clearTimeout(timer)
      timers.clear()
    }
  }, [])

  return {
    syncStates,
    isSyncing,
    getSyncState,
    syncOne,
    syncAll,
    autoSyncIfStale,
    cancelAll,
  }
}
