import type {
  SessionImportSummary,
  SessionImportType,
  SessionImports,
  SessionMetricsResponse,
} from '@/lib/api/types'

export const sessionImportLabels: Record<SessionImportType, string> = {
  registrations: 'Registrations',
  attendance: 'Attendance',
  qa: 'Q&A',
}

export function formatDateTime(iso: string | null | undefined): string {
  if (!iso) return '—'

  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) {
    return '—'
  }

  return date.toLocaleString(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  })
}

export function formatSessionSchedule(
  startsAt: string | null | undefined,
  endsAt: string | null | undefined,
) {
  if (!startsAt) {
    return { date: '—', time: '', duration: '', tzShort: '', tzTooltip: '' }
  }

  const start = new Date(startsAt)
  const date = start.toLocaleDateString(undefined, { dateStyle: 'medium' })
  const time = start.toLocaleTimeString(undefined, {
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  })

  const tzShort =
    start
      .toLocaleTimeString(undefined, { timeZoneName: 'short' })
      .split(' ')
      .pop() ?? ''
  const tzLong = start
    .toLocaleTimeString(undefined, { timeZoneName: 'long' })
    .replace(/^[\d:]+\s*(AM|PM)?\s*/i, '')

  const offsetMinutes = -start.getTimezoneOffset()
  const sign = offsetMinutes >= 0 ? '+' : '-'
  const hours = Math.floor(Math.abs(offsetMinutes) / 60)
  const minutes = Math.abs(offsetMinutes) % 60
  const offset = `GMT ${sign}${hours}${minutes > 0 ? `:${String(minutes).padStart(2, '0')}` : ''}`

  let duration = ''
  if (endsAt) {
    const end = new Date(endsAt)
    const diffMinutes = Math.round((end.getTime() - start.getTime()) / 60000)
    if (diffMinutes > 0) {
      const durationHours = Math.floor(diffMinutes / 60)
      const durationMinutes = diffMinutes % 60

      if (durationHours > 0) {
        duration += `${durationHours} hr${durationHours === 1 ? '' : 's'}`
      }

      if (durationMinutes > 0) {
        duration += `${durationHours > 0 ? ' ' : ''}${durationMinutes} min`
      }
    }
  }

  return {
    date,
    time,
    duration,
    tzShort,
    tzTooltip: `${tzLong} (${offset})`,
  }
}

export function getImportSummary(
  imports: SessionImports | null | undefined,
  importType: SessionImportType,
): SessionImportSummary | null {
  return imports?.[importType] ?? null
}

export function getImportImportedAt(summary: SessionImportSummary | null | undefined) {
  return summary?.importedAt ?? null
}

export function getImportFileName(summary: SessionImportSummary | null | undefined) {
  return summary?.fileName ?? null
}

export function getImportRowCount(summary: SessionImportSummary | null | undefined) {
  return summary?.rowCount ?? null
}

export function getLatestImportInfo(imports: SessionImports | null | undefined) {
  const candidates = (Object.keys(sessionImportLabels) as SessionImportType[])
    .map((importType) => {
      const summary = getImportSummary(imports, importType)
      const importedAt = getImportImportedAt(summary)

      if (!importedAt) {
        return null
      }

      return {
        importType,
        importedAt,
        fileName: getImportFileName(summary),
        rowCount: getImportRowCount(summary),
      }
    })
    .filter((candidate): candidate is NonNullable<typeof candidate> => candidate !== null)

  if (candidates.length === 0) {
    return null
  }

  return candidates.sort(
    (left, right) =>
      new Date(right.importedAt).getTime() - new Date(left.importedAt).getTime(),
  )[0]
}

export function getQaAnalytics(
  metrics: SessionMetricsResponse | null | undefined,
): {
  totalQuestions: number
  answeredQuestions: number
  unansweredQuestions: number
} {
  const totalQuestions = metrics?.totalQaQuestions ?? 0
  const answeredQuestions = metrics?.answeredQaQuestions ?? 0

  return {
    totalQuestions,
    answeredQuestions,
    unansweredQuestions: Math.max(totalQuestions - answeredQuestions, 0),
  }
}

export function buildPrimarySessionMetricCards(
  metrics: SessionMetricsResponse | null | undefined,
) {
  return [
    { label: 'Registrations', value: metrics?.totalRegistrations ?? 0 },
    { label: 'Attendees', value: metrics?.totalAttendees ?? 0 },
    {
      label: 'Registrant Domains',
      value: metrics?.uniqueRegistrantAccountDomains ?? 0,
    },
    {
      label: 'Attendee Domains',
      value: metrics?.uniqueAttendeeAccountDomains ?? 0,
    },
  ]
}

export function buildQaMetricCards(metrics: SessionMetricsResponse | null | undefined) {
  const qa = getQaAnalytics(metrics)

  return [
    { label: 'Questions', value: qa.totalQuestions },
    { label: 'Answered', value: qa.answeredQuestions },
    { label: 'Unanswered', value: qa.unansweredQuestions },
  ].filter(
    (
      card,
    ): card is {
      label: string
      value: number
    } => typeof card.value === 'number',
  )
}
