import { apiFetch } from './client'
import type {
  SessionListItem,
  SessionResponse,
  SessionImportUploadResponse,
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

async function uploadSessionImport(
  sessionId: string,
  importType: 'registrations' | 'attendance' | 'qa',
  file: File,
  accessToken: string,
): Promise<SessionImportUploadResponse> {
  const formData = new FormData()
  formData.append('file', file)

  return apiFetch<SessionImportUploadResponse>(
    `/sessions/${sessionId}/imports/${importType}`,
    {
      method: 'POST',
      body: formData,
    },
    accessToken,
  )
}

export async function uploadSessionRegistrationsCsv(
  sessionId: string,
  file: File,
  accessToken: string,
): Promise<SessionImportUploadResponse> {
  return uploadSessionImport(sessionId, 'registrations', file, accessToken)
}

export async function uploadSessionAttendanceCsv(
  sessionId: string,
  file: File,
  accessToken: string,
): Promise<SessionImportUploadResponse> {
  return uploadSessionImport(sessionId, 'attendance', file, accessToken)
}

export async function uploadSessionQaCsv(
  sessionId: string,
  file: File,
  accessToken: string,
): Promise<SessionImportUploadResponse> {
  return uploadSessionImport(sessionId, 'qa', file, accessToken)
}
