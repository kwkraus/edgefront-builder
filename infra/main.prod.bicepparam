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
// COMPUTE (3 parameters)
// ============================================================================

param appServicePlanSku = 'Standard_B1s'
param appServicePlanTier = 'Standard'
param appServiceRuntimeStack = 'DOTNETCORE|10.0'

// ============================================================================
// DATABASE (5 parameters)
// ============================================================================

param sqlServerAdminUsername = 'sqladmin'
param sqlServerAdminPassword = ''
param sqlDatabaseSku = 'Standard'
param sqlDatabaseMaxSizeBytes = 10737418240
param backupRetentionDays = 35

// ============================================================================
// FRONTEND (3 parameters)
// ============================================================================

param staticWebAppsSku = 'Standard'
param staticWebAppsRuntimeStack = 'node'
param nodeVersion = 'lts'

// ============================================================================
// MONITORING (3 parameters)
// ============================================================================

param enableApplicationInsights = true
param applicationsInsightsSku = 'PerGB2018'
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
// SECURITY & NETWORKING (3 parameters)
// ============================================================================

param enableManagedIdentity = true
param enableEncryption = true
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
