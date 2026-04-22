using './main.bicep'

// ============================================================================
// PRODUCTION ENVIRONMENT PARAMETERS
// ============================================================================
// This parameter file defines all infrastructure settings for production deployment.
// Values are optimized for reliability, security, compliance, and performance.
//
// IMPORTANT: Sensitive parameters (sqlServerAdminPassword) should be sourced
// from Azure Key Vault during deployment, not hardcoded in this file.
// See deployment scripts for Azure CLI/ARM integration patterns.

// ============================================================================
// ENVIRONMENT & NAMING (4 parameters)
// ============================================================================

param environment = 'prod'
param projectName = 'edgefront-builder'
param location = 'eastus'
param resourceNamePrefix = 'aie'

// ============================================================================
// COMPUTE (2 parameters)
// ============================================================================

param appServicePlanSku = 'B2'
param appServiceRuntimeStack = 'DOTNETCORE|10.0'

// ============================================================================
// DATABASE (2 parameters)
// ============================================================================

param sqlServerAdminUsername = 'sqladmin'
param sqlServerAdminPassword = ''

// ============================================================================
// FRONTEND
// ============================================================================

// Frontend now runs on App Service instead of Static Web Apps

// ============================================================================
// MONITORING (2 parameters)
// ============================================================================

param enableApplicationInsights = true
param logAnalyticsRetentionDays = 90

// ============================================================================
// APPLICATION CONFIGURATION (5 parameters)
// ============================================================================

param entraadTenantId = ''
param entraadClientId = ''
param entraadAudience = ''
param graphBaseUrl = 'https://graph.microsoft.com/v1.0'
param corsAllowedOrigins = []

// ============================================================================
// SECURITY & NETWORKING (2 parameters)
// ============================================================================

param enableManagedIdentity = true
param enableDiagnostics = true

// ============================================================================
// TAGS (1 parameter)
// ============================================================================

param commonTags = {
  environment: 'prod'
  project: 'edgefront-builder'
  costCenter: 'operations'
  createdBy: 'deployment-pipeline'
  criticality: 'high'
}
