export interface SeriesListItem {
  seriesId: string
  title: string
  status: 'Draft' | 'Published'
  sessionCount: number
  draftSessionCount: number
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
  draftSessionCount: number
  createdAt: string
  updatedAt: string
}

export interface SessionListItem {
  sessionId: string
  title: string
  startsAt: string
  endsAt: string
  status: 'Draft' | 'Published'
  teamsWebinarId: string | null
  joinWebUrl: string | null
  reconcileStatus: 'Synced' | 'Reconciling'
  driftStatus: 'None' | 'DriftDetected'
  totalRegistrations: number
  totalAttendees: number
  lastSyncAt: string | null
  presenterCount: number
  coordinatorCount: number
  ownerDisplayName: string
  presenters: PersonSummary[]
  coordinators: PersonSummary[]
}

export interface PersonSummary {
  displayName: string
  email: string
}

export interface SessionResponse {
  sessionId: string
  seriesId: string
  title: string
  startsAt: string
  endsAt: string
  status: 'Draft' | 'Published'
  teamsWebinarId: string | null
  joinWebUrl: string | null
  reconcileStatus: 'Synced' | 'Reconciling'
  driftStatus: 'None' | 'DriftDetected'
  lastSyncAt: string | null
  lastError: string | null
  presenters: SessionPresenterDto[]
  coordinators: SessionCoordinatorDto[]
}

export interface PersonSearchResult {
  entraUserId: string
  displayName: string
  email: string
}

export interface SessionPresenterDto {
  sessionPresenterId: string
  entraUserId: string
  displayName: string
  email: string
}

export interface SessionCoordinatorDto {
  sessionCoordinatorId: string
  entraUserId: string
  displayName: string
  email: string
}

export interface PersonInput {
  entraUserId: string
  displayName: string
  email: string
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

export interface ImportResult {
  totalRows: number
  importedCount: number
  skippedCount: number
  invalidCount: number
  errors: RowError[]
}

export interface RowError {
  row: number
  reason: string
}
