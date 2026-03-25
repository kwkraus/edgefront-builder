import { apiFetch, ApiError } from './client'
import type {
  SessionListItem,
  SessionResponse,
  PersonSearchResult,
  SessionPresenterDto,
  SessionCoordinatorDto,
  PersonInput,
  ImportResult,
} from './types'

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
  signal?: AbortSignal,
): Promise<{ synced: boolean }> {
  return apiFetch<{ synced: boolean }>(
    `/sessions/${id}/sync`,
    { method: 'POST', signal },
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

// --- People search ---

export async function searchPeople(
  query: string,
  accessToken: string,
): Promise<PersonSearchResult[]> {
  return apiFetch<PersonSearchResult[]>(
    `/people/search?q=${encodeURIComponent(query)}`,
    {},
    accessToken,
  )
}

// --- Presenters ---

export async function getSessionPresenters(
  sessionId: string,
  accessToken: string,
): Promise<SessionPresenterDto[]> {
  return apiFetch<SessionPresenterDto[]>(
    `/sessions/${sessionId}/presenters`,
    {},
    accessToken,
  )
}

export async function setSessionPresenters(
  sessionId: string,
  people: PersonInput[],
  accessToken: string,
): Promise<SessionPresenterDto[]> {
  return apiFetch<SessionPresenterDto[]>(
    `/sessions/${sessionId}/presenters`,
    { method: 'PUT', body: JSON.stringify({ people }) },
    accessToken,
  )
}

// --- Coordinators ---

export async function getSessionCoordinators(
  sessionId: string,
  accessToken: string,
): Promise<SessionCoordinatorDto[]> {
  return apiFetch<SessionCoordinatorDto[]>(
    `/sessions/${sessionId}/coordinators`,
    {},
    accessToken,
  )
}

export async function setSessionCoordinators(
  sessionId: string,
  people: PersonInput[],
  accessToken: string,
): Promise<SessionCoordinatorDto[]> {
  return apiFetch<SessionCoordinatorDto[]>(
    `/sessions/${sessionId}/coordinators`,
    { method: 'PUT', body: JSON.stringify({ people }) },
    accessToken,
  )
}

// ── CSV Import ────────────────────────────────────────────────────────────────

function getBaseUrl(): string {
  if (typeof window === 'undefined') {
    return (
      process.env.BACKEND_API_BASE_URL ??
      process.env.NEXT_PUBLIC_BACKEND_API_BASE_URL ??
      'http://localhost:5000'
    )
  }
  return process.env.NEXT_PUBLIC_BACKEND_API_BASE_URL ?? 'http://localhost:5000'
}

export async function uploadRegistrationsCsv(
  sessionId: string,
  file: File,
  accessToken: string,
): Promise<ImportResult> {
  const baseUrl = getBaseUrl()

  const formData = new FormData()
  formData.append('file', file)

  const res = await fetch(`${baseUrl}/api/v1/sessions/${sessionId}/imports/registrations`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${accessToken}` },
    body: formData,
  })

  if (!res.ok) {
    let message = `HTTP ${res.status}`
    try {
      const body = await res.json()
      message = body.message ?? body.error ?? message
    } catch { /* ignore */ }
    throw new ApiError(message, res.status)
  }

  return res.json() as Promise<ImportResult>
}

export async function uploadAttendanceCsv(
  sessionId: string,
  file: File,
  accessToken: string,
): Promise<ImportResult> {
  const baseUrl = getBaseUrl()

  const formData = new FormData()
  formData.append('file', file)

  const res = await fetch(`${baseUrl}/api/v1/sessions/${sessionId}/imports/attendance`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${accessToken}` },
    body: formData,
  })

  if (!res.ok) {
    let message = `HTTP ${res.status}`
    try {
      const body = await res.json()
      message = body.message ?? body.error ?? message
    } catch { /* ignore */ }
    throw new ApiError(message, res.status)
  }

  return res.json() as Promise<ImportResult>
}
