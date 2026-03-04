import { apiFetch } from './client'
import type { SessionListItem, SessionResponse } from './types'

export async function getSessionsBySeries(
  seriesId: string,
  accessToken: string,
): Promise<SessionListItem[]> {
  return apiFetch<SessionListItem[]>(`/series/${seriesId}/sessions`, {}, accessToken)
}

export async function createSession(
  seriesId: string,
  data: { title: string; startsAt: string; endsAt: string },
  accessToken: string,
): Promise<SessionResponse> {
  return apiFetch<SessionResponse>(
    `/series/${seriesId}/sessions`,
    {
      method: 'POST',
      body: JSON.stringify(data),
    },
    accessToken,
  )
}

export async function getSessionById(id: string, accessToken: string): Promise<SessionResponse> {
  return apiFetch<SessionResponse>(`/sessions/${id}`, {}, accessToken)
}

export async function updateSession(
  id: string,
  data: { title: string; startsAt: string; endsAt: string },
  accessToken: string,
): Promise<SessionResponse> {
  return apiFetch<SessionResponse>(
    `/sessions/${id}`,
    {
      method: 'PUT',
      body: JSON.stringify(data),
    },
    accessToken,
  )
}

export async function deleteSession(id: string, accessToken: string): Promise<void> {
  return apiFetch<void>(`/sessions/${id}`, { method: 'DELETE' }, accessToken)
}

export async function syncSession(
  id: string,
  accessToken: string,
): Promise<{ synced: boolean }> {
  return apiFetch<{ synced: boolean }>(
    `/sessions/${id}/sync`,
    { method: 'POST' },
    accessToken,
  )
}

export async function publishSession(
  id: string,
  accessToken: string,
): Promise<SessionResponse> {
  return apiFetch<SessionResponse>(
    `/sessions/${id}/publish`,
    { method: 'POST' },
    accessToken,
  )
}
