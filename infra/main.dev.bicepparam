using './main.bicep'

// ============================================================================
// Environment & Naming
// ============================================================================

param environment = 'dev'
param projectName = 'edgefront'
param location = 'eastus'
param resourceNamePrefix = 'ef-dev'

// ============================================================================
// Compute (App Service)
// ============================================================================

param appServicePlanSku = 'Standard_B1s'
param appServicePlanTier = 'Standard'
param appServiceRuntimeStack = 'DOTNETCORE|10.0'

// ============================================================================
// Database (SQL)
// ============================================================================

param sqlServerAdminUsername = 'sqladmin'
// IMPORTANT: This is a placeholder password. In production, use Azure Key Vault
// or provide via `azd env set` to avoid storing secrets in version control.
// Minimum requirements: 8-128 chars, uppercase, lowercase, digit, special char
param sqlServerAdminPassword = 'DevP@ssw0rd123!'

param sqlDatabaseSku = 'Free'
param sqlDatabaseMaxSizeBytes = 1073741824
param backupRetentionDays = 7

// ============================================================================
// Frontend (Static Web Apps)
// ============================================================================

param staticWebAppsSku = 'Standard'
param staticWebAppsRuntimeStack = 'node'
param nodeVersion = 'lts'

// ============================================================================
// Monitoring (Application Insights)
// ============================================================================

param enableApplicationInsights = true
param applicationsInsightsSku = 'PerGB2018'
param logAnalyticsRetentionDays = 30

// ============================================================================
// Application Configuration
// ============================================================================

// IMPORTANT: These must be provided with your actual Azure Entra values
// Obtain these from your Azure Entra app registration and Azure AI Foundry setup
// param entraadTenantId = '<your-tenant-id>'
// param entraadClientId = '<your-client-id>'
// param entraadAudience = '<your-audience-uri>'

// Placeholder values - MUST be replaced before deployment
param entraadTenantId = '00000000-0000-0000-0000-000000000000'
param entraadClientId = '11111111-1111-1111-1111-111111111111'
param entraadAudience = 'api://edgefront-api-dev'

param graphBaseUrl = 'https://graph.microsoft.com/v1.0'
param corsAllowedOrigins = [
  'http://localhost:3000'
  'http://localhost:3001'
]

// ============================================================================
// Security & Networking
// ============================================================================

param enableManagedIdentity = true
param enableEncryption = true
param enableDiagnostics = true

// ============================================================================
// Tags
// ============================================================================

param commonTags = {
  environment: 'dev'
  project: 'edgefront'
  costCenter: 'engineering'
  createdBy: 'devops'
  managedBy: 'azd'
}
