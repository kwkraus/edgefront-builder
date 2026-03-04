import { apiFetch } from './client'
import type { SeriesMetricsResponse, SessionMetricsResponse } from './types'

export async function getSeriesMetrics(
  seriesId: string,
  accessToken: string,
): Promise<SeriesMetricsResponse> {
  return apiFetch<SeriesMetricsResponse>(`/series/${seriesId}/metrics`, {}, accessToken)
}

export async function getSessionMetrics(
  sessionId: string,
  accessToken: string,
): Promise<SessionMetricsResponse> {
  return apiFetch<SessionMetricsResponse>(`/sessions/${sessionId}/metrics`, {}, accessToken)
}
