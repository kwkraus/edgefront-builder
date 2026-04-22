// EdgeFront Builder Main Bicep Template
// Defines all Azure infrastructure for the full-stack application:
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

@description('App Service Plan tier')
param appServicePlanTier string

@description('ASP.NET Core runtime stack (format: DOTNETCORE|VERSION)')
param appServiceRuntimeStack string

// Database parameters
@description('SQL Server administrator username')
param sqlServerAdminUsername string

@description('SQL Server administrator password (must be 8-128 chars with uppercase, lowercase, digit, special char)')
@secure()
param sqlServerAdminPassword string

@description('SQL Database SKU (Free for dev, Standard for prod)')
param sqlDatabaseSku string

@description('SQL Database maximum size in bytes')
param sqlDatabaseMaxSizeBytes int

@description('Backup retention period in days (1-35)')
param backupRetentionDays int

// Frontend parameters
@description('Static Web Apps SKU')
param staticWebAppsSku string

@description('Static Web Apps runtime stack')
param staticWebAppsRuntimeStack string

@description('Node.js version for Static Web Apps')
param nodeVersion string

// Monitoring parameters
@description('Enable Application Insights monitoring')
param enableApplicationInsights bool

@description('Application Insights pricing model')
param applicationsInsightsSku string

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

@description('Enable encryption at rest')
param enableEncryption bool

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
var appServiceName = '${resourceNamePrefix}-app'
var sqlServerName = '${resourceNamePrefix}-sql-${uniqueString(resourceGroup().id)}'
var sqlDatabaseName = '${projectName}db'
var staticWebAppName = '${resourceNamePrefix}-swa'
var applicationInsightsName = '${resourceNamePrefix}-appinsights'
var logAnalyticsWorkspaceName = '${resourceNamePrefix}-logs'
var storageAccountName = '${replace(resourceNamePrefix, '-', '')}storage'
var managedIdentityName = '${resourceNamePrefix}-identity'

// Connection strings and configuration
var sqlConnectionString = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

// Diagnostic settings names
var appServiceDiagnosticsName = '${appServiceName}-diagnostics'
var sqlDatabaseDiagnosticsName = '${sqlDatabaseName}-diagnostics'

// ============================================================================
// RESOURCES
// ============================================================================

// Log Analytics Workspace - Central logging
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: commonTags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: logAnalyticsRetentionDays
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// Application Insights - Monitoring and observability
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = if (enableApplicationInsights) {
  name: applicationInsightsName
  location: location
  kind: 'web'
  tags: commonTags
  properties: {
    Application_Type: 'web'
    RetentionInDays: logAnalyticsRetentionDays
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

// Storage Account - Optional for future file uploads
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  tags: commonTags
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        blob: {
          enabled: enableEncryption
        }
        file: {
          enabled: enableEncryption
        }
      }
      keySource: 'Microsoft.Storage'
    }
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

// Managed Identity for App Service - Enables secure SQL connection without passwords
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = if (enableManagedIdentity) {
  name: managedIdentityName
  location: location
  tags: commonTags
}

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2021-11-01-preview' = {
  name: sqlServerName
  location: location
  tags: commonTags
  properties: {
    administratorLogin: sqlServerAdminUsername
    administratorLoginPassword: sqlServerAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: false
      login: sqlServerAdminUsername
      principalType: 'User'
      sid: ''
      tenantId: subscription().tenantId
    }
  }
}

// SQL Server Firewall Rule - Allow Azure services
resource sqlServerFirewallRule 'Microsoft.Sql/servers/firewallRules@2021-11-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: commonTags
  sku: {
    name: sqlDatabaseSku
    tier: sqlDatabaseSku == 'Free' ? 'Free' : 'Standard'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: sqlDatabaseMaxSizeBytes
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: false
  }
}

// SQL Database Backup (short-term retention)
resource sqlDatabaseBackupPolicy 'Microsoft.Sql/servers/databases/backupShortTermRetentionPolicies@2021-11-01-preview' = {
  parent: sqlDatabase
  name: 'default'
  properties: {
    retentionDays: backupRetentionDays
    diffBackupIntervalInHours: 24
  }
}

// SQL Database Diagnostics - Send logs to Log Analytics
resource sqlDatabaseDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableDiagnostics) {
  name: sqlDatabaseDiagnosticsName
  scope: sqlDatabase
  properties: {
    workspaceId: logAnalyticsWorkspace.id
    logs: [
      {
        category: 'SQLSecurityAuditEvents'
        enabled: true
      }
      {
        category: 'SQLInsights'
        enabled: true
      }
      {
        category: 'AutomaticTuning'
        enabled: true
      }
      {
        category: 'QueryStoreRuntimeStatistics'
        enabled: true
      }
      {
        category: 'QueryStoreWaitStatistics'
        enabled: true
      }
      {
        category: 'Errors'
        enabled: true
      }
      {
        category: 'DatabaseWaitStatistics'
        enabled: true
      }
      {
        category: 'Timeouts'
        enabled: true
      }
      {
        category: 'Blocks'
        enabled: true
      }
      {
        category: 'Deadlocks'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'Basic'
        enabled: true
      }
    ]
  }
}

// App Service Plan (Linux, Standard tier)
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  kind: 'Linux'
  tags: commonTags
  sku: {
    name: appServicePlanSku
    tier: appServicePlanTier
  }
  properties: {
    reserved: true
  }
}

// App Service - Backend API (.NET 10)
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
  location: location
  kind: 'app,linux'
  tags: commonTags
  identity: enableManagedIdentity ? {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  } : {
    type: 'None'
  }
  properties: {
    serverFarmId: appServicePlan.id
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
          value: enableApplicationInsights ? applicationInsights.properties.ConnectionString : ''
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
    httpsOnly: true
    publicNetworkAccess: 'Enabled'
  }
}

// App Service Diagnostics - Send logs to Log Analytics
resource appServiceDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (enableDiagnostics) {
  name: appServiceDiagnosticsName
  scope: appService
  properties: {
    workspaceId: logAnalyticsWorkspace.id
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
}

// RBAC Role Assignment - App Service Managed Identity gets SQL Database Data Reader/Writer
resource sqlDatabaseRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enableManagedIdentity) {
  name: guid(sqlDatabase.id, 'dc9ce79b-5c97-4a28-92ac-4222ca76eacd')
  scope: sqlDatabase
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'dc9ce79b-5c97-4a28-92ac-4222ca76eacd')
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Static Web App - Frontend (Next.js 16)
resource staticWebApp 'Microsoft.Web/staticSites@2022-09-01' = {
  name: staticWebAppName
  location: location
  kind: 'staticsite'
  tags: commonTags
  sku: {
    name: staticWebAppsSku
    tier: staticWebAppsSku == 'Free' ? 'Free' : 'Standard'
  }
  properties: {
    buildProperties: {
      appLocation: 'src/frontend'
      apiLocation: ''
      appArtifactLocation: '.next'
      outputLocation: 'out'
      githubActionSecretNameOverride: ''
    }
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

@description('Frontend URL - Static Web Apps domain')
output frontendUrl string = 'https://${staticWebApp.properties.defaultHostname}'

@description('Backend URL - App Service domain')
output backendUrl string = 'https://${appService.properties.defaultHostNames[0]}'

@description('SQL Server FQDN')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('SQL Database name')
output sqlDatabaseName string = sqlDatabaseName

@description('Application Insights instrumentation key')
output appInsightsInstrumentationKey string = enableApplicationInsights ? applicationInsights.properties.InstrumentationKey : ''

@description('Application Insights connection string')
output appInsightsConnectionString string = enableApplicationInsights ? applicationInsights.properties.ConnectionString : ''

@description('Log Analytics workspace ID')
output logAnalyticsWorkspaceId string = logAnalyticsWorkspace.id

@description('Managed Identity resource ID')
output managedIdentityId string = enableManagedIdentity ? managedIdentity.id : ''

@description('Managed Identity principal ID')
output managedIdentityPrincipalId string = enableManagedIdentity ? managedIdentity.properties.principalId : ''

@description('App Service resource ID')
output appServiceId string = appService.id

@description('App Service plan resource ID')
output appServicePlanId string = appServicePlan.id

@description('SQL Server resource ID')
output sqlServerId string = sqlServer.id

@description('SQL Database resource ID')
output sqlDatabaseId string = sqlDatabase.id

@description('Storage Account resource ID')
output storageAccountId string = storageAccount.id

@description('Static Web App resource ID')
output staticWebAppId string = staticWebApp.id

@description('Azure Portal App Service link')
output portalAppServiceLink string = 'https://portal.azure.com/#resource${appService.id}/overview'

@description('Azure Portal SQL Database link')
output portalSqlDatabaseLink string = 'https://portal.azure.com/#resource${sqlDatabase.id}/overview'

@description('Azure Portal Application Insights link')
output portalAppInsightsLink string = enableApplicationInsights ? 'https://portal.azure.com/#resource${applicationInsights.id}/overview' : ''

@description('Azure Portal Log Analytics link')
output portalLogAnalyticsLink string = 'https://portal.azure.com/#resource${logAnalyticsWorkspace.id}/overview'

@description('Deployment summary')
output deploymentSummary object = {
  environment: environment
  projectName: projectName
  location: location
  resourceNamePrefix: resourceNamePrefix
  frontendUrl: 'https://${staticWebApp.properties.defaultHostname}'
  backendUrl: 'https://${appService.properties.defaultHostNames[0]}'
  sqlFqdn: sqlServer.properties.fullyQualifiedDomainName
  appInsightsEnabled: enableApplicationInsights
  managedIdentityEnabled: enableManagedIdentity
  diagnosticsEnabled: enableDiagnostics
}
