export interface SeriesListItem {
  seriesId: string
  title: string
  status?: string
  sessionCount: number
  draftSessionCount?: number
  totalRegistrations: number
  totalAttendees: number
  uniqueAccountsInfluenced: number
  hasReconcileIssues?: boolean
  createdAt: string
  updatedAt: string
}

export interface SeriesResponse {
  seriesId: string
  title: string
  status?: string
  draftSessionCount?: number
  createdAt: string
  updatedAt: string
}

export type SessionImportType = 'registrations' | 'attendance' | 'qa'

export interface SessionImportSummary {
  importType: string
  fileName: string
  rowCount: number
  importedAt: string
}

export interface SessionImports {
  registrations?: SessionImportSummary | null
  attendance?: SessionImportSummary | null
  qa?: SessionImportSummary | null
}

export interface SessionListItem {
  sessionId: string
  title: string
  startsAt: string
  endsAt: string
  status?: string
  totalRegistrations: number
  totalAttendees: number
  imports?: SessionImports | null
}

export interface SessionResponse {
  sessionId: string
  seriesId: string
  title: string
  startsAt: string
  endsAt: string
  status?: string
  imports?: SessionImports | null
}

export interface SeriesMetricsResponse {
  seriesId: string
  totalRegistrations: number
  totalAttendees: number
  totalQaQuestions: number
  answeredQaQuestions: number
  uniqueRegistrantAccountDomains: number
  uniqueAccountsInfluenced: number
  warmAccounts: { accountDomain: string; warmRule: 'W1' | 'W2' }[]
}

export interface SessionMetricsResponse {
  sessionId: string
  totalRegistrations: number
  totalAttendees: number
  totalQaQuestions: number
  answeredQaQuestions: number
  uniqueRegistrantAccountDomains: number
  uniqueAttendeeAccountDomains: number
  warmAccountsTriggered: string[]
}

export interface SessionImportUploadResponse {
  importType: SessionImportType
  importedAt: string
  fileName: string
  rowCount: number
}

/**
 * Represents a single registrant parsed from a registration CSV file.
 * Status indicates success or failure; ErrorReason is only populated for failed registrants.
 */
export interface ParsedRegistrant {
  email: string
  firstName: string
  lastName: string
  registeredAt: string // ISO 8601 datetime
  status: 'success' | 'failed'
  errorReason?: string | null
}

/**
 * Response from the registration preview endpoint.
 * Provides a summary of parsed registrants without persisting to the database.
 */
export interface RegistrationPreviewDto {
  sessionTitle: string
  registrantCount: number
  successCount: number
  failedCount: number
  registrants: ParsedRegistrant[]
  warnings?: string[] | null
  errors?: string[] | null
}

/**
 * Request body for confirming and persisting a registration import.
 * Contains the list of registrants that passed validation in the preview step.
 */
export interface ConfirmRegistrationImportRequest {
  registrants: ParsedRegistrant[]
}
