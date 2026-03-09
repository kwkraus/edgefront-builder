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
  }, [])

  const syncOne = useCallback(
    async (sessionId: string, signal?: AbortSignal) => {
      updateState(sessionId, 'syncing')
      const timeoutCtrl = new AbortController()
      const timer = setTimeout(() => timeoutCtrl.abort(), timeoutMs)

      // Combine the per-session timeout with the shared cancel signal
      const combinedSignal = signal
        ? AbortSignal.any([signal, timeoutCtrl.signal])
        : timeoutCtrl.signal

      try {
        await syncSession(sessionId, accessToken, combinedSignal)
        updateState(sessionId, 'done')
      } catch {
        // Aborted by cancellation or timeout
        if (combinedSignal.aborted) {
          updateState(sessionId, 'error')
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
    return () => {
      abortRef.current?.abort()
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
