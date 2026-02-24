import { apiFetch } from './client'
import type { SeriesListItem, SeriesResponse } from './types'

export async function getSeries(accessToken: string): Promise<SeriesListItem[]> {
  return apiFetch<SeriesListItem[]>('/series', {}, accessToken)
}

export async function getSeriesById(id: string, accessToken: string): Promise<SeriesResponse> {
  return apiFetch<SeriesResponse>(`/series/${id}`, {}, accessToken)
}

export async function createSeries(
  data: { title: string },
  accessToken: string,
): Promise<SeriesResponse> {
  return apiFetch<SeriesResponse>(
    '/series',
    {
      method: 'POST',
      body: JSON.stringify(data),
    },
    accessToken,
  )
}

export async function updateSeries(
  id: string,
  data: { title: string },
  accessToken: string,
): Promise<SeriesResponse> {
  return apiFetch<SeriesResponse>(
    `/series/${id}`,
    {
      method: 'PUT',
      body: JSON.stringify(data),
    },
    accessToken,
  )
}

export async function deleteSeries(id: string, accessToken: string): Promise<void> {
  return apiFetch<void>(`/series/${id}`, { method: 'DELETE' }, accessToken)
}

export async function publishSeries(id: string, accessToken: string): Promise<SeriesResponse> {
  return apiFetch<SeriesResponse>(`/series/${id}/publish`, { method: 'POST' }, accessToken)
}
