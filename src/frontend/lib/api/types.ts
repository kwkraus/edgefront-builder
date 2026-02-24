export interface SeriesListItem {
  seriesId: string
  title: string
  status: 'Draft' | 'Published'
  sessionCount: number
  totalRegistrations: number
  totalAttendees: number
  uniqueAccountsInfluenced: number
  hasReconcileIssues: boolean
  createdAt: string
  updatedAt: string
}

export interface SeriesResponse {
  seriesId: string
  title: string
  status: 'Draft' | 'Published'
  createdAt: string
  updatedAt: string
}

export interface SessionListItem {
  sessionId: string
  title: string
  startsAt: string
  endsAt: string
  status: 'Draft' | 'Published'
  reconcileStatus: 'Synced' | 'Reconciling' | 'Retrying' | 'Disabled'
  driftStatus: 'None' | 'DriftDetected'
  totalRegistrations: number
  totalAttendees: number
}

export interface SessionResponse {
  sessionId: string
  seriesId: string
  title: string
  startsAt: string
  endsAt: string
  status: 'Draft' | 'Published'
  teamsWebinarId: string | null
  reconcileStatus: 'Synced' | 'Reconciling' | 'Retrying' | 'Disabled'
  driftStatus: 'None' | 'DriftDetected'
  lastSyncAt: string | null
  lastError: string | null
}

export interface SeriesMetricsResponse {
  seriesId: string
  totalRegistrations: number
  totalAttendees: number
  uniqueRegistrantAccountDomains: number
  uniqueAccountsInfluenced: number
  warmAccounts: { accountDomain: string; warmRule: 'W1' | 'W2' }[]
}

export interface SessionMetricsResponse {
  sessionId: string
  totalRegistrations: number
  totalAttendees: number
  uniqueRegistrantAccountDomains: number
  uniqueAttendeeAccountDomains: number
  warmAccountsTriggered: string[]
}
