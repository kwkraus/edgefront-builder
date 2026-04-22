// EdgeFront Builder Main Bicep Template
// Defines all Azure infrastructure for the full-stack application using Azure Verified Modules:
// - ASP.NET Core backend API on App Service
// - Next.js frontend on Static Web Apps
// - Azure SQL Database for data persistence
// - Application Insights for monitoring
// - Log Analytics for centralized logging

// ============================================================================
// PARAMETERS
// ============================================================================

@description('Deployment environment name (dev or prod)')
@allowed(['dev', 'prod'])
param environment string

@description('Project name for resource naming (max 20 alphanumeric chars)')
param projectName string

@description('Azure region for resource deployment')
param location string

@description('Prefix for all resource names (lowercase alphanumeric and hyphens, max 15 chars)')
param resourceNamePrefix string

// Compute parameters
@description('App Service Plan SKU')
param appServicePlanSku string

@description('ASP.NET Core runtime stack (format: DOTNETCORE|VERSION)')
param appServiceRuntimeStack string

// Database parameters
@description('SQL Server administrator username')
param sqlServerAdminUsername string

@description('SQL Server administrator password (must be 8-128 chars with uppercase, lowercase, digit, special char)')
@secure()
param sqlServerAdminPassword string

// Monitoring parameters
@description('Enable Application Insights monitoring')
param enableApplicationInsights bool

@description('Log Analytics data retention in days (1-730)')
param logAnalyticsRetentionDays int

// Application configuration parameters
@description('Azure AD tenant ID')
param entraadTenantId string

@description('App registration client ID')
param entraadClientId string

@description('App registration audience URI')
param entraadAudience string

@description('Microsoft Graph API endpoint')
param graphBaseUrl string

@description('CORS allowed origins for backend')
param corsAllowedOrigins array

// Security parameters
@description('Enable managed identity for resources')
param enableManagedIdentity bool

@description('Enable diagnostic logging')
param enableDiagnostics bool

// Tags
@description('Common tags applied to all resources')
param commonTags object

// ============================================================================
// VARIABLES
// ============================================================================

// Resource naming (using symbolic references)
var appServicePlanName = '${resourceNamePrefix}-plan'
var backendAppServiceName = '${resourceNamePrefix}-backend'
var frontendAppServiceName = '${resourceNamePrefix}-frontend'
var sqlServerName = '${resourceNamePrefix}-sql-${uniqueString(resourceGroup().id)}'
var sqlDatabaseName = '${projectName}db'
var applicationInsightsName = '${resourceNamePrefix}-appinsights'
var logAnalyticsWorkspaceName = '${resourceNamePrefix}-logs'
var storageAccountName = '${replace(resourceNamePrefix, '-', '')}storage'
var managedIdentityName = '${resourceNamePrefix}-identity'

// Connection strings and configuration
var sqlConnectionString = 'Server=tcp:${sqlServer.outputs.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

// Diagnostic settings names
var backendAppServiceDiagnosticsName = '${backendAppServiceName}-diagnostics'
var frontendAppServiceDiagnosticsName = '${frontendAppServiceName}-diagnostics'

// ============================================================================
// RESOURCES
// ============================================================================

// Log Analytics Workspace AVM - Central logging
module logAnalyticsWorkspace 'br/public:avm/res/operational-insights/workspace:0.6.0' = {
  name: 'logAnalyticsWorkspace-${uniqueString(deployment().name)}'
  params: {
    name: logAnalyticsWorkspaceName
    location: location
    tags: commonTags
    dataRetention: logAnalyticsRetentionDays
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Application Insights AVM - Monitoring and observability
module applicationInsights 'br/public:avm/res/insights/component:0.7.1' = if (enableApplicationInsights) {
  name: 'applicationInsights-${uniqueString(deployment().name)}'
  params: {
    name: applicationInsightsName
    location: location
    tags: commonTags
    applicationType: 'web'
    kind: 'web'
    retentionInDays: logAnalyticsRetentionDays
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
  }
}

// Storage Account AVM - Optional for future file uploads
module storageAccount 'br/public:avm/res/storage/storage-account:0.12.0' = {
  name: 'storageAccount-${uniqueString(deployment().name)}'
  params: {
    name: storageAccountName
    location: location
    tags: commonTags
    kind: 'StorageV2'
    skuName: 'Standard_LRS'
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

// Managed Identity AVM - Enables secure SQL connection without passwords
module managedIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.3.0' = if (enableManagedIdentity) {
  name: 'managedIdentity-${uniqueString(deployment().name)}'
  params: {
    name: managedIdentityName
    location: location
    tags: commonTags
  }
}

// SQL Server AVM
module sqlServer 'br/public:avm/res/sql/server:0.21.1' = {
  name: 'sqlServer-${uniqueString(deployment().name)}'
  params: {
    name: sqlServerName
    location: location
    tags: commonTags
    administratorLogin: sqlServerAdminUsername
    administratorLoginPassword: sqlServerAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    databases: [
      {
        name: sqlDatabaseName
        collation: 'SQL_Latin1_General_CP1_CI_AS'
        availabilityZone: 1
      }
    ]
  }
}

// App Service Plan AVM
module appServicePlan 'br/public:avm/res/web/serverfarm:0.7.0' = {
  name: 'appServicePlan-${uniqueString(deployment().name)}'
  params: {
    name: appServicePlanName
    location: location
    tags: commonTags
    kind: 'Linux'
    reserved: true
    skuName: appServicePlanSku
    skuCapacity: appServicePlanSku == 'F1' ? 1 : appServicePlanSku == 'B1' ? 1 : 2
  }
}

// App Service AVM - Backend API (.NET 10)
module appServiceModule 'br/public:avm/res/web/site:0.22.0' = {
  name: 'appService-backend-${uniqueString(deployment().name)}'
  params: {
    name: backendAppServiceName
    location: location
    tags: commonTags
    kind: 'app'
    serverFarmResourceId: appServicePlan.outputs.resourceId
    httpsOnly: true
    publicNetworkAccess: 'Enabled'
    managedIdentities: enableManagedIdentity ? {
      userAssignedResourceIds: [
        managedIdentity.outputs.resourceId
      ]
    } : null
    siteConfig: {
      linuxFxVersion: appServiceRuntimeStack
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      use32BitWorkerProcess: false
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: enableApplicationInsights ? applicationInsights.outputs.connectionString : ''
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'recommended'
        }
        {
          name: 'EntraAD__TenantId'
          value: entraadTenantId
        }
        {
          name: 'EntraAD__ClientId'
          value: entraadClientId
        }
        {
          name: 'EntraAD__Audience'
          value: entraadAudience
        }
        {
          name: 'GraphAPI__BaseUrl'
          value: graphBaseUrl
        }
        {
          name: 'CORS__AllowedOrigins'
          value: join(corsAllowedOrigins, ',')
        }
        {
          name: 'Database__ConnectionString'
          value: sqlConnectionString
        }
        {
          name: 'Database__UseManagedIdentity'
          value: string(enableManagedIdentity)
        }
      ]
      connectionStrings: [
        {
          name: 'DefaultConnection'
          connectionString: sqlConnectionString
          type: 'SQLServer'
        }
      ]
    }
    diagnosticSettings: enableDiagnostics ? [
      {
        name: backendAppServiceDiagnosticsName
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
        logs: [
          {
            category: 'AppServiceHTTPLogs'
            enabled: true
          }
          {
            category: 'AppServiceConsoleLogs'
            enabled: true
          }
          {
            category: 'AppServiceAppLogs'
            enabled: true
          }
          {
            category: 'AppServicePlatformLogs'
            enabled: true
          }
        ]
        metrics: [
          {
            category: 'AllMetrics'
            enabled: true
          }
        ]
      }
    ] : []
  }
}

// RBAC Role Assignment - App Service Managed Identity gets SQL Database Data Reader/Writer
resource sqlDatabaseRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enableManagedIdentity) {
  name: guid(sqlServer.name, 'dc9ce79b-5c97-4a28-92ac-4222ca76eacd')
  scope: resourceGroup()
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'dc9ce79b-5c97-4a28-92ac-4222ca76eacd')
    principalId: managedIdentity.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

// App Service AVM - Frontend (Next.js 16)
module frontendAppServiceModule 'br/public:avm/res/web/site:0.22.0' = {
  name: 'appService-frontend-${uniqueString(deployment().name)}'
  params: {
    name: frontendAppServiceName
    location: location
    tags: commonTags
    kind: 'app'
    serverFarmResourceId: appServicePlan.outputs.resourceId
    httpsOnly: true
    publicNetworkAccess: 'Enabled'
    siteConfig: {
      linuxFxVersion: 'NODE|20'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      use32BitWorkerProcess: false
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: enableApplicationInsights ? applicationInsights.outputs.connectionString : ''
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'recommended'
        }
        {
          name: 'NODE_ENV'
          value: 'production'
        }
      ]
    }
    diagnosticSettings: enableDiagnostics ? [
      {
        name: frontendAppServiceDiagnosticsName
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
        logs: [
          {
            category: 'AppServiceHTTPLogs'
            enabled: true
          }
          {
            category: 'AppServiceConsoleLogs'
            enabled: true
          }
          {
            category: 'AppServiceAppLogs'
            enabled: true
          }
          {
            category: 'AppServicePlatformLogs'
            enabled: true
          }
        ]
        metrics: [
          {
            category: 'AllMetrics'
            enabled: true
          }
        ]
      }
    ] : []
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

@description('Frontend URL - App Service domain')
output frontendUrl string = frontendAppServiceModule.outputs.defaultHostname

@description('Backend URL - App Service domain')
output backendUrl string = appServiceModule.outputs.defaultHostname

@description('SQL Server FQDN')
output sqlServerFqdn string = sqlServer.outputs.fullyQualifiedDomainName

@description('SQL Database name')
output sqlDatabaseName string = sqlDatabaseName

@description('Application Insights instrumentation key')
output appInsightsInstrumentationKey string = enableApplicationInsights ? applicationInsights.outputs.instrumentationKey : ''

@description('Application Insights connection string')
output appInsightsConnectionString string = enableApplicationInsights ? applicationInsights.outputs.connectionString : ''

@description('Log Analytics workspace ID')
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.outputs.resourceId

@description('Managed Identity resource ID')
output managedIdentityId string = enableManagedIdentity ? managedIdentity.outputs.resourceId : ''

@description('Managed Identity principal ID')
output managedIdentityPrincipalId string = enableManagedIdentity ? managedIdentity.outputs.principalId : ''

@description('Backend App Service resource ID')
output backendAppServiceId string = appServiceModule.outputs.resourceId

@description('Frontend App Service resource ID')
output frontendAppServiceId string = frontendAppServiceModule.outputs.resourceId

@description('App Service plan resource ID')
output appServicePlanId string = appServicePlan.outputs.resourceId

@description('SQL Server resource ID')
output sqlServerId string = sqlServer.outputs.resourceId

@description('Storage Account resource ID')
output storageAccountId string = storageAccount.outputs.resourceId

@description('Azure Portal Backend App Service link')
output portalBackendAppServiceLink string = 'https://portal.azure.com/#resource${appServiceModule.outputs.resourceId}/overview'

@description('Azure Portal Frontend App Service link')
output portalFrontendAppServiceLink string = 'https://portal.azure.com/#resource${frontendAppServiceModule.outputs.resourceId}/overview'

@description('Azure Portal Application Insights link')
output portalAppInsightsLink string = enableApplicationInsights ? 'https://portal.azure.com/#resource${applicationInsights.outputs.resourceId}/overview' : ''

@description('Azure Portal Log Analytics link')
output portalLogAnalyticsLink string = 'https://portal.azure.com/#resource${logAnalyticsWorkspace.outputs.resourceId}/overview'

@description('Deployment summary')
output deploymentSummary object = {
  environment: environment
  projectName: projectName
  location: location
  resourceNamePrefix: resourceNamePrefix
  frontendUrl: frontendAppServiceModule.outputs.defaultHostname
  backendUrl: appServiceModule.outputs.defaultHostname
  sqlFqdn: sqlServer.outputs.fullyQualifiedDomainName
  appInsightsEnabled: enableApplicationInsights
  managedIdentityEnabled: enableManagedIdentity
  diagnosticsEnabled: enableDiagnostics
}
