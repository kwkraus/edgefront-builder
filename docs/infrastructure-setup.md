# EdgeFront Builder: Comprehensive Infrastructure Setup Guide

**📖 Last Updated**: 2025  
**🎯 Audience**: Developers, DevOps engineers, operations teams  
**⏱️ Reading Time**: 20-30 minutes  

---

## 📋 Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Resource Inventory](#resource-inventory)
3. [Deployment Architecture](#deployment-architecture)
4. [Security & Identity](#security--identity)
5. [Monitoring & Observability](#monitoring--observability)
6. [Development Workflow](#development-workflow)
7. [Production Deployment](#production-deployment)
8. [Maintenance & Operations](#maintenance--operations)
9. [Troubleshooting Guide](#troubleshooting-guide)
10. [Reference](#reference)

---

## Architecture Overview

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         AZURE CLOUD                                 │
│                                                                      │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐             │
│  │  Static Web  │  │  App Service │  │  SQL Server  │             │
│  │  App (SWA)   │  │  (.NET 10)   │  │  Database    │             │
│  │  Frontend    │  │  Backend API │  │  (PostgreSQL)│             │
│  │  (Next.js)   │  │  (ASP.NET)   │  │              │             │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘             │
│         │                 │                 │                      │
│         └─────────────────┼─────────────────┘                      │
│                           │                                         │
│         ┌─────────────────┴──────────────────┐                     │
│         │                                    │                     │
│    ┌────▼──────┐                    ┌───────▼────┐                │
│    │ Managed   │                    │ Managed    │                │
│    │ Identity  │                    │ Identity   │                │
│    │ (Backend) │                    │ (SQL)      │                │
│    └───────────┘                    └────────────┘                │
│                                                                    │
│  ┌────────────────────┐  ┌────────────────────┐                  │
│  │ Log Analytics      │  │ Application        │                  │
│  │ Workspace          │  │ Insights           │                  │
│  │ (Centralized logs) │  │ (Monitoring)       │                  │
│  └────────────────────┘  └────────────────────┘                  │
│                                                                    │
│  ┌────────────────────┐  ┌────────────────────┐                  │
│  │ Storage Account    │  │ Azure DevOps       │                  │
│  │ (Blob Storage)     │  │ (CI/CD Pipeline)   │                  │
│  └────────────────────┘  └────────────────────┘                  │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────┐
│                       CLIENT APPLICATIONS                           │
├────────────────────────────────────────────────────────────────────┤
│ Web Browser (Frontend: https://static-web-app.azurestaticapps.net) │
│ Mobile/Desktop (API: https://app-service.azurewebsites.net)       │
└────────────────────────────────────────────────────────────────────┘
```

### Component Relationships

```
User/Client
    │
    ├─► Static Web App (Frontend)
    │   └─► Entra ID Authentication
    │   └─► Next.js 16 + React 19
    │   └─► HTTPS enforced
    │
    └─► App Service (Backend API)
        └─► JWT Token Validation (from Entra ID)
        └─► ASP.NET Core Minimal API
        └─► Managed Identity → SQL Access
        │
        └─► SQL Server + Database
            └─► EF Core ORM
            └─► Backup: 7 days (dev), 35 days (prod)
            └─► Encryption at rest
            │
            └─► Monitoring
                ├─► Application Insights
                ├─► Log Analytics Workspace
                └─► Diagnostic Settings (all resources)
```

### Data Flow

1. **User Requests** → Static Web App (frontend)
2. **Authentication** → Entra ID (via next-auth)
3. **API Calls** → App Service (with JWT token)
4. **Token Validation** → Entra ID token verification
5. **Data Access** → Managed Identity → SQL Database
6. **Monitoring** → All requests logged to Log Analytics
7. **Alerts** → Application Insights triggers based on metrics

### Security Boundaries

| Boundary | Protection | Enforcement |
|----------|-----------|-------------|
| **Internet → Frontend** | HTTPS TLS 1.2+ | Static Web App enforced |
| **Internet → Backend** | HTTPS TLS 1.2+, JWT validation | App Service enforced |
| **Frontend → Backend** | CORS policy (configurable) | Backend middleware |
| **Backend → Database** | Managed Identity (no passwords) | RBAC role assignment |
| **Database Network** | Private connection string | SQL Server firewall rules |
| **Internal Logs** | Log Analytics encryption | Azure managed encryption |

---

## Resource Inventory

### Complete Resource List (14 Resources)

EdgeFront Builder deploys **14 Azure resources** organized by tier:

#### **Monitoring & Logging Layer (3 resources)**

| # | Resource Name | Type | Purpose | Dev SKU | Prod SKU | Pricing |
|---|---|---|---|---|---|---|
| 1 | `Log Analytics Workspace` | Microsoft.OperationalInsights/workspaces | Centralized log ingestion and KQL queries | PerGB2018 | PerGB2018 | Pay-per-GB (~$0.50/GB) |
| 2 | `Application Insights` | Microsoft.Insights/components | Performance monitoring, traces, exceptions | Conditional | Conditional | Free tier (1GB/day) |
| 3 | `Storage Account` | Microsoft.Storage/storageAccounts | Blob storage, future file upload support | Standard_LRS | Standard_LRS | ~$0.024/GB |

**Configuration Details:**
- **Log Analytics**: 30 days retention (dev), 90 days (prod)
- **App Insights**: Only created if `enableApplicationInsights = true`
- **Storage**: Hot tier, HTTPS-only, encryption enabled, public blob access disabled

---

#### **Compute Layer (2 resources)**

| # | Resource Name | Type | Purpose | Dev Config | Prod Config | Scaling |
|---|---|---|---|---|---|---|
| 4 | `App Service Plan` | Microsoft.Web/serverfarms | Compute tier for backend | Standard_B1s | Standard_B1s | Manual via SKU |
| 5 | `App Service` | Microsoft.Web/sites | ASP.NET Core backend API | Linux, Standard | Linux, Standard | 1-10 instances possible |

**Configuration Details:**
- **Runtime**: .NET 10 (configured as `DOTNETCORE|10.0`)
- **HTTP/2 enabled** for better performance
- **Always On** enabled (prevents app from unloading)
- **Min TLS 1.2** enforced (scm + public endpoints)
- **Managed Identity**: Attached for secure SQL access
- **Connection strings**: Passed via app settings (DefaultConnection)

---

#### **Frontend Layer (1 resource)**

| # | Resource Name | Type | Purpose | Dev SKU | Prod SKU | Hosting |
|---|---|---|---|---|---|---|
| 6 | `Static Web App` | Microsoft.Web/staticSites | Next.js 16 frontend | Standard | Standard | SPA + API proxy |

**Configuration Details:**
- **Build config**: `appLocation: 'src/frontend'`, `.next` artifact, `out` output
- **Node.js**: LTS version auto-managed by Azure
- **HTTPS**: Automatic, includes default domain + custom domains support
- **CI/CD**: GitHub Actions integration (optional)

---

#### **Database Layer (4 resources)**

| # | Resource Name | Type | Purpose | Dev Config | Prod Config | Backup |
|---|---|---|---|---|---|---|
| 7 | `SQL Server` | Microsoft.Sql/servers | Database engine host | v12.0 | v12.0 | 7 days (dev), 35 days (prod) |
| 8 | `SQL Database` | Microsoft.Sql/servers/databases | Application data store | Free SKU | Standard SKU | Automated |
| 9 | `SQL Firewall Rule` | Microsoft.Sql/servers/firewallRules | Allow Azure services | AllowAzureServices | AllowAzureServices | — |
| 10 | `Backup Policy` | Microsoft.Sql/servers/databases/backupShortTermRetentionPolicies | Short-term backup retention | 7 days | 35 days | Automated daily |

**Configuration Details:**
- **Admin auth**: SQL Server login + optional Entra ID
- **Encryption**: TLS 1.2 minimum, encryption at rest
- **Collation**: SQL_Latin1_General_CP1_CI_AS (US English, case-insensitive)
- **Backup**: Automated diff backups every 24 hours
- **Max size**: 1GB (dev), 10GB (prod)

---

#### **Security & Identity Layer (3 resources)**

| # | Resource Name | Type | Purpose | Dev Use | Prod Use | Access |
|---|---|---|---|---|---|---|
| 11 | `Managed Identity` | Microsoft.ManagedIdentity/userAssignedIdentities | Service principal for App Service | SQL access | SQL access | No passwords |
| 12 | `SQL DB Role Assignment` | Microsoft.Authorization/roleAssignments | Grant data reader/writer to identity | RBAC | RBAC | Automatic via role |
| 13 | `App Diagnostics` | Microsoft.Insights/diagnosticSettings | Log App Service events | All categories | All categories | Log Analytics |

---

#### **Monitoring Layer (1 resource)**

| # | Resource Name | Type | Purpose | Dev Logs | Prod Logs | Retention |
|---|---|---|---|---|---|---|
| 14 | `SQL Diagnostics` | Microsoft.Insights/diagnosticSettings | Audit, tune, security logs | 10 categories | 10 categories | 30-90 days |

**Diagnostic Categories Enabled:**
- Security audits (SQLSecurityAuditEvents)
- Performance insights (SQLInsights, AutomaticTuning)
- Query statistics (QueryStoreRuntimeStatistics, QueryStoreWaitStatistics)
- Errors and failures (Errors, Blocks, Deadlocks, Timeouts, DatabaseWaitStatistics)

---

### Cost Implications

#### Development Environment (Typical Monthly)

| Resource | Tier | Est. Cost | Notes |
|----------|------|-----------|-------|
| Log Analytics | PerGB2018 | $5–10 | 30-day retention, ~10-20GB/month |
| App Service Plan | Standard_B1s | $10.95 | Shared compute, always-on |
| App Service | (included) | $0 | Included in plan cost |
| Static Web App | Standard | $9.99 | Included tier |
| SQL Database | Free | $0 | Limited to 32GB, no SLO |
| SQL Server | (included) | $0 | No separate charge |
| Storage Account | Standard LRS | $0.50 | ~20GB at $0.024/GB |
| Managed Identity | (included) | $0 | No additional cost |
| Application Insights | Free | $0 | 1GB/day limit |
| **Total Monthly** | — | **~$26–$30** | Excludes data transfer |

#### Production Environment (Typical Monthly)

| Resource | Tier | Est. Cost | Notes |
|----------|------|-----------|-------|
| Log Analytics | PerGB2018 | $25–50 | 90-day retention, ~50-100GB/month |
| App Service Plan | Standard_B1s | $10.95 | Shared compute |
| App Service | (included) | $0 | Included in plan |
| Static Web App | Standard | $9.99 | Production tier |
| SQL Database | Standard S0 | $15 | 250 DTU, 10GB storage |
| SQL Server | (included) | $0 | No separate charge |
| Storage Account | Standard LRS | $1–2 | ~50GB at $0.024/GB |
| Managed Identity | (included) | $0 | No additional cost |
| Application Insights | $2.50/GB overage | $20–40 | Beyond 1GB/day |
| **Total Monthly** | — | **~$82–$130** | Excludes data transfer, backups |

> **Note**: These are estimates. Actual costs depend on traffic, data volume, and regional pricing. Use [Azure Pricing Calculator](https://azure.microsoft.com/en-us/pricing/calculator/) for precise estimates.

---

### SLA & Availability Guarantees

| Resource | SLA | RTO | RPO | Notes |
|----------|-----|-----|-----|-------|
| **App Service (Standard)** | 99.95% | 5 min | Near-zero | Auto-failover within region |
| **SQL Database (Standard)** | 99.99% | 30 sec | < 5 sec | Geo-redundant backups |
| **Static Web App (Standard)** | 99.95% | 5 min | Near-zero | CDN-backed, multi-regional |
| **Storage Account (Standard LRS)** | 99.9% | 1 hour | < 1 hour | Locally redundant |
| **Log Analytics** | 99.9% | 15 min | < 1 min | Region-specific |
| **Application Insights** | 99.9% | 15 min | Near-zero | Regional redundancy |

**Composite SLA (dev environment)**: ~99.77% (sequential dependencies)  
**Composite SLA (prod environment)**: ~99.77% (same configuration)

---

## Deployment Architecture

### AZD Workflow Overview

Azure Developer CLI (`azd`) orchestrates the entire deployment:

```
azd up / azd provision / azd deploy
    │
    ├─► Load environment config (.azd/config.json)
    ├─► Select environment (dev or prod)
    ├─► Load parameter file (main.dev.bicepparam / main.prod.bicepparam)
    │
    ├─► PREPROVISIONING CHECKS
    │   └─► Authenticate to Azure
    │   └─► Create resource group (if not exists)
    │   └─► Validate Bicep syntax
    │
    ├─► PROVISION PHASE (azd provision)
    │   └─► Deploy Bicep template (14 resources)
    │   └─► Output resource IDs, URLs, connection strings
    │   └─► Run post-provision hooks
    │
    ├─► BUILD PHASE (azd deploy)
    │   ├─► Build frontend: npm run build
    │   ├─► Build backend: dotnet build
    │   └─► Package artifacts for deployment
    │
    └─► DEPLOY PHASE
        ├─► Push frontend to Static Web App
        ├─► Push backend to App Service
        └─► Run health checks
```

### Bicep Template Organization

```
main.bicep (547 lines)
├─ PARAMETERS (100 lines)
│  ├─ environment, projectName, location, resourceNamePrefix
│  ├─ appServicePlanSku, appServicePlanTier, appServiceRuntimeStack
│  ├─ sqlServerAdminUsername, sqlServerAdminPassword, sqlDatabaseSku
│  ├─ staticWebAppsSku, nodeVersion
│  ├─ enableApplicationInsights, logAnalyticsRetentionDays
│  ├─ entraadTenantId, entraadClientId, entraadAudience
│  ├─ graphBaseUrl, corsAllowedOrigins
│  ├─ enableManagedIdentity, enableEncryption, enableDiagnostics
│  └─ commonTags
│
├─ VARIABLES (27 lines)
│  ├─ Resource naming (logAnalyticsWorkspaceName, appServiceName, etc.)
│  ├─ Connection strings (sqlConnectionString)
│  └─ Diagnostic setting names
│
├─ RESOURCES (306 lines)
│  ├─ Log Analytics Workspace
│  ├─ Application Insights (conditional)
│  ├─ Storage Account
│  ├─ Managed Identity
│  ├─ SQL Server
│  ├─ SQL Firewall Rule
│  ├─ SQL Database
│  ├─ SQL Backup Policy
│  ├─ SQL Diagnostics
│  ├─ App Service Plan
│  ├─ App Service (with identity, app settings)
│  ├─ App Service Diagnostics
│  ├─ SQL Database RBAC Role Assignment
│  └─ Static Web App
│
└─ OUTPUTS (20 lines)
   ├─ frontendUrl, backendUrl
   ├─ sqlServerFqdn, sqlDatabaseName
   ├─ appInsightsInstrumentationKey, appInsightsConnectionString
   ├─ logAnalyticsWorkspaceId, managedIdentityId, managedIdentityPrincipalId
   ├─ Resource IDs (appService, appServicePlan, sqlServer, sqlDatabase, etc.)
   ├─ Portal links (for quick navigation)
   └─ deploymentSummary (overview object)
```

### Environment-Specific Configuration

#### Development Environment (`main.dev.bicepparam`)

```bicep
environment = 'dev'
projectName = 'edgefront'
location = 'eastus'
resourceNamePrefix = 'ef-dev'

// Minimal compute: development use only
appServicePlanSku = 'Standard_B1s'
appServicePlanTier = 'Standard'

// Free SQL Database (1GB limit, no SLO)
sqlDatabaseSku = 'Free'
sqlDatabaseMaxSizeBytes = 1073741824 (1 GB)
backupRetentionDays = 7

// 30-day log retention (cost effective)
logAnalyticsRetentionDays = 30

// Dev-focused CORS (localhost)
corsAllowedOrigins = [
  'http://localhost:3000',
  'http://localhost:3001'
]

// Tags: engineering
commonTags = {
  environment: 'dev'
  project: 'edgefront'
  costCenter: 'engineering'
  createdBy: 'devops'
  managedBy: 'azd'
}
```

#### Production Environment (`main.prod.bicepparam`)

```bicep
environment = 'prod'
projectName = 'edgefront-builder'
location = 'eastus'
resourceNamePrefix = 'aie'

// Same compute tier (scale via SKU if needed)
appServicePlanSku = 'Standard_B1s'
appServicePlanTier = 'Standard'

// Standard SQL Database (10GB, SLO guaranteed)
sqlDatabaseSku = 'Standard'
sqlDatabaseMaxSizeBytes = 10737418240 (10 GB)
backupRetentionDays = 35 (IMPORTANT: regulatory retention)

// 90-day log retention (compliance)
logAnalyticsRetentionDays = 90

// Prod-focused CORS (production domain only)
corsAllowedOrigins = []  // Set via deployment pipeline

// Tags: operations
commonTags = {
  environment: 'prod'
  project: 'edgefront-builder'
  costCenter: 'operations'
  createdBy: 'deployment-pipeline'
  criticality: 'high'
}
```

### Parameter Inheritance & Overrides

Parameters flow through the deployment pipeline:

```
User Input (azd env set)
    ↓
Environment File (.azure/env)
    ↓
Parameter File (main.dev.bicepparam / main.prod.bicepparam)
    ↓
Runtime Overrides (azd env set during deployment)
    ↓
Bicep Template (main.bicep)
    ↓
Deployed Resources (Azure)
```

**Example: Overriding Entra ID values for dev**
```powershell
azd env set AZURE_ENTRA_TENANT_ID "12345678-1234-1234-1234-123456789012"
azd env set AZURE_ENTRA_CLIENT_ID "87654321-4321-4321-4321-210987654321"
```

These override the placeholder values in `main.dev.bicepparam`.

---

## Security & Identity

### Managed Identity Setup

**Purpose**: Allow App Service to access SQL Database without storing passwords.

**Flow**:
```
App Service
    ↓
Managed Identity (User-Assigned)
    ↓
RBAC Role: SQL DB Data Reader/Writer
    ↓
SQL Server (authenticates without password)
```

**Implementation**:
```bicep
// 1. Create Managed Identity
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = if (enableManagedIdentity) {
  name: managedIdentityName
  location: location
  tags: commonTags
}

// 2. Attach to App Service
resource appService 'Microsoft.Web/sites@2022-09-01' = {
  ...
  identity: enableManagedIdentity ? {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  } : {
    type: 'None'
  }
}

// 3. Grant RBAC Role (SQL DB Data Reader/Writer)
resource sqlDatabaseRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (enableManagedIdentity) {
  name: guid(sqlDatabase.id, 'dc9ce79b-5c97-4a28-92ac-4222ca76eacd')
  scope: sqlDatabase
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'dc9ce79b-5c97-4a28-92ac-4222ca76eacd') // SQL DB Data Reader/Writer
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// 4. Enable in app settings
{
  name: 'Database__UseManagedIdentity'
  value: string(enableManagedIdentity)
}
```

**Backend Code Integration** (ASP.NET Core):
```csharp
// Use managed identity in connection string
var connection = new SqlConnection(connectionString);
// ASP.NET Core automatically uses the app service's managed identity
// if the connection string doesn't include authentication credentials
```

### Entra ID Authentication Flow

**User Login**:
```
1. User visits frontend (Static Web App)
2. next-auth redirects to Entra ID login page
3. User enters credentials
4. Entra ID issues ID token + access token
5. next-auth stores tokens in session
6. Frontend calls backend with access token in Authorization header
7. Backend validates token against Entra ID (aud = entraadAudience)
8. Token grants access to backend endpoints
```

**Configuration**:
```bicep
// In bicep template, passed to backend via app settings:
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

// Example values:
// TenantId: "12345678-1234-1234-1234-123456789012"
// ClientId: "87654321-4321-4321-4321-210987654321"
// Audience: "api://edgefront-api-dev"
```

**Backend Validation** (ASP.NET Core):
```csharp
// In Program.cs or startup:
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidAudience = audience,
      ValidateAudience = true,
      // Additional validations...
    };
  });
```

### RBAC Role Assignments

**SQL Database Data Reader/Writer Role**:
- Role ID: `dc9ce79b-5c97-4a28-92ac-4222ca76eacd`
- Grants: SELECT, INSERT, UPDATE, DELETE on all tables
- Assigned to: Managed Identity (app service principal)
- Scope: SQL Database

**Verify role assignment**:
```powershell
az role assignment list --scope /subscriptions/{subId}/resourceGroups/{rgName}/providers/Microsoft.Sql/servers/{sqlServer}/databases/{database} \
  --query "[?properties.principalType=='ServicePrincipal']"
```

### Network Security

#### Firewall Rules

**SQL Server Firewall**:
```bicep
resource sqlServerFirewallRule 'Microsoft.Sql/servers/firewallRules@2021-11-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}
```

**Effect**: Allows all Azure services (App Service, Functions, etc.) to connect to SQL Server without IP whitelisting.

**Additional Security**:
- SQL Server public access: Enabled (required for App Service in same region)
- Min TLS version: 1.2
- Encryption: Always enabled
- Certificate validation: Required

#### CORS Configuration

**Backend CORS**:
```bicep
{
  name: 'CORS__AllowedOrigins'
  value: join(corsAllowedOrigins, ',')
}

// Dev example:
corsAllowedOrigins = [
  'http://localhost:3000',
  'http://localhost:3001'
]

// Prod example:
corsAllowedOrigins = [
  'https://edgefront.azurestaticapps.net',
  'https://www.edgefront.com'
]
```

**Backend Implementation** (ASP.NET Core):
```csharp
builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowFrontend", corsPolicyBuilder =>
  {
    var allowedOrigins = configuration["CORS__AllowedOrigins"]?.Split(',') ?? Array.Empty<string>();
    corsPolicyBuilder
      .WithOrigins(allowedOrigins)
      .AllowAnyMethod()
      .AllowAnyHeader();
  });
});

app.UseCors("AllowFrontend");
```

### Encryption at Rest and in Transit

**Encryption at Rest**:
```bicep
// Storage Account encryption
encryption: {
  services: {
    blob: {
      enabled: enableEncryption  // true
    }
    file: {
      enabled: enableEncryption  // true
    }
  }
  keySource: 'Microsoft.Storage'  // Azure-managed keys
}

// SQL Database encryption: Automatic (TDE - Transparent Data Encryption)
// Log Analytics encryption: Automatic (Azure-managed keys)
```

**Encryption in Transit**:
```bicep
// Enforce HTTPS only
httpsOnly: true

// Min TLS version
minTlsVersion: '1.2'
scmMinTlsVersion: '1.2'

// SQL Server
minimalTlsVersion: '1.2'
```

---

## Monitoring & Observability

### Application Insights Integration

**Instrumentation**:
```bicep
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
```

**Outputs**:
- **Instrumentation Key**: Unique ID for your app insights instance
- **Connection String**: Full connection details for SDK initialization

**Automatic Collection** (via agent):
- Request rates
- Response times
- Dependency calls (database, HTTP)
- Exception rates
- Performance counters (CPU, memory)
- Traces (logging output)

### Log Analytics Workspace

**Purpose**: Centralized log ingestion and KQL (Kusto Query Language) querying.

**Retention**:
- Dev: 30 days
- Prod: 90 days

**Key Metrics Tables**:
- `AppServiceHTTPLogs` — HTTP requests to App Service
- `AppServiceConsoleLogs` — stdout/stderr from app
- `AppServiceAppLogs` — Application-level logs
- `SQLSecurityAuditEvents` — SQL audit logs
- `SQLInsights` — SQL performance insights

**Example KQL Query** (top 10 slowest API calls):
```kusto
AppServiceHTTPLogs
| where Method == "GET" or Method == "POST"
| summarize AvgTime = avg(TimeTaken), MaxTime = max(TimeTaken) by CsUriStem
| top 10 by AvgTime desc
```

### Diagnostic Logging Configuration

**App Service Diagnostics** (enabled if `enableDiagnostics = true`):
```bicep
logs: [
  { category: 'AppServiceHTTPLogs', enabled: true }
  { category: 'AppServiceConsoleLogs', enabled: true }
  { category: 'AppServiceAppLogs', enabled: true }
  { category: 'AppServicePlatformLogs', enabled: true }
]
metrics: [
  { category: 'AllMetrics', enabled: true }
]
```

**SQL Database Diagnostics** (10 categories):
```bicep
logs: [
  { category: 'SQLSecurityAuditEvents', enabled: true }   // SQL audit
  { category: 'SQLInsights', enabled: true }              // Performance insights
  { category: 'AutomaticTuning', enabled: true }          // Auto-tune recommendations
  { category: 'QueryStoreRuntimeStatistics', enabled: true } // Query performance
  { category: 'QueryStoreWaitStatistics', enabled: true } // Query wait stats
  { category: 'Errors', enabled: true }                   // SQL errors
  { category: 'DatabaseWaitStatistics', enabled: true }   // DB wait statistics
  { category: 'Timeouts', enabled: true }                 // Connection timeouts
  { category: 'Blocks', enabled: true }                   // Query blocking
  { category: 'Deadlocks', enabled: true }                // Deadlock detection
]
metrics: [
  { category: 'Basic', enabled: true }                    // CPU, DTU, connections
]
```

### Key Metrics to Monitor

#### Backend (App Service)

| Metric | Alert Threshold | Action |
|--------|-----------------|--------|
| **CPU %** | > 80% for 5 min | Scale up or optimize code |
| **Memory %** | > 85% for 5 min | Investigate memory leaks |
| **Response Time** | > 2000ms (p95) | Check database performance |
| **Error Rate** | > 1% of requests | Review error logs |
| **Request Count** | > 1000 req/sec | Consider load balancing |
| **HTTP 5xx errors** | Any | Immediate investigation |
| **Availability** | < 99.9% | Escalate |

#### Database (SQL)

| Metric | Alert Threshold | Action |
|--------|-----------------|--------|
| **DTU %** | > 80% for 5 min | Scale to higher SKU |
| **Storage %** | > 90% | Archive old data or expand |
| **Connection count** | > 80 concurrent | Review connection pooling |
| **Query duration** | > 30s (p99) | Create indexes, optimize queries |
| **Deadlock count** | > 0 in 1 hour | Review transaction isolation |
| **Failed logins** | > 5 in 1 hour | Investigate auth issues |

#### Frontend (Static Web App)

| Metric | Alert Threshold | Action |
|--------|-----------------|--------|
| **Page load time** | > 3 seconds (p95) | Optimize bundle, check CDN |
| **First paint** | > 1.5 seconds | Optimize critical path |
| **Error rate** | > 0.5% | Check browser console logs |
| **Availability** | < 99.95% | Escalate to Microsoft |

### Alert Recommendations

**Critical Alerts** (immediate notification):
```
✓ HTTP 5xx errors > 10 in 1 hour
✓ Database connection failures > 3 in 1 hour
✓ Response time > 5 seconds (p95)
✓ Error rate > 5%
✓ Availability < 99%
```

**Warning Alerts** (digest):
```
✓ CPU utilization > 75% for 10 min
✓ Memory utilization > 80% for 10 min
✓ Storage usage > 80%
✓ Slow queries > 30s (log but don't alert immediately)
```

**Set up alerts** via Azure Portal:
1. Navigate to Log Analytics Workspace
2. Create new alert rule
3. Define KQL query
4. Set threshold and notification action group
5. Test alert

---

## Development Workflow

### Prerequisites

**Required Software**:
```powershell
# Azure CLI (version 2.50+)
az --version

# Azure Developer CLI (version 0.10+)
azd --version

# Git (for version control)
git --version

# Node.js (for frontend)
node --version

# .NET SDK 10 (for backend)
dotnet --version
```

**Azure Account**:
- Active Azure subscription
- Contributor role on subscription (or resource group)
- Permission to create app registrations in Entra ID

**Entra ID Setup** (see `docs/setup-entra-permissions.md`):
- Tenant ID
- App registration with Client ID
- Audience URI (e.g., `api://edgefront-api-dev`)

### Local Setup Steps

#### 1. Clone Repository
```powershell
git clone https://github.com/kwkraus/edgefront-builder.git
cd edgefront-builder
```

#### 2. Install Dependencies
```powershell
# Backend dependencies
cd src/backend
dotnet restore
cd ../..

# Frontend dependencies
cd src/frontend
npm install
cd ../..
```

#### 3. Configure Local Environment
```powershell
# Copy .env.example to .env.local (frontend)
cd src/frontend
cp .env.example .env.local
# Edit .env.local with your Entra ID values
# NEXT_PUBLIC_API_URL=http://localhost:3001
# NEXT_PUBLIC_ENTRA_TENANT_ID=...
# NEXT_PUBLIC_ENTRA_CLIENT_ID=...

cd ../backend
# Copy appsettings.Development.json if needed
# Update with local database connection string
cd ../..
```

#### 4. Run Local Database (Optional)
```powershell
# Using SQL Server Docker container
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=DevP@ss123!" `
  -p 1433:1433 `
  -d mcr.microsoft.com/mssql/server:2022-latest

# Or use local SQL Server installation
# Connection string: Server=localhost;Database=edgefront_dev;User Id=sa;Password=...;
```

#### 5. Run Frontend Locally
```powershell
cd src/frontend
npm run dev
# Access at http://localhost:3000
```

#### 6. Run Backend Locally
```powershell
cd src/backend
dotnet run
# Access at http://localhost:5001 or check terminal for exact URL
```

### Environment Selection

**List available environments**:
```powershell
azd env list
```

**Select environment for deployment**:
```powershell
# Development
azd env select dev

# Production (requires approval/confirmation)
azd env select prod

# Create new environment
azd env new <env-name>
```

**Verify selected environment**:
```powershell
azd env get-values
# Lists all current environment variables
```

### Deployment Commands

#### Provision Infrastructure
```powershell
# Full provision (create infrastructure)
azd provision

# With optional flags
azd provision --debug              # Verbose output
azd provision --dry-run            # Preview without deploying
```

**What happens**:
1. Creates resource group (if needed)
2. Deploys Bicep template
3. Outputs resource IDs and URLs
4. Runs post-provision hooks
5. Configures app settings

**Expected output**:
```
Deployment in progress...
✓ Resource group created/verified
✓ Bicep deployment started
  [5-15 minute wait]
✓ All resources provisioned successfully

Frontend URL: https://ef-dev-swa-abc123.azurestaticapps.net
Backend URL: https://ef-dev-app.azurewebsites.net
SQL Server FQDN: ef-dev-sql-abc123.database.windows.net
Application Insights: (if enabled)
```

#### Deploy Application Code
```powershell
# Full deploy (build + push to Azure)
azd deploy

# With optional flags
azd deploy --debug                 # Verbose output
```

**What happens**:
1. Builds frontend (Next.js)
2. Builds backend (ASP.NET Core)
3. Packages artifacts
4. Pushes to Static Web App
5. Pushes to App Service
6. Runs health checks

**Expected output**:
```
✓ Frontend deployment started
✓ Backend deployment started
  [5-10 minute wait]
✓ All deployments completed successfully
✓ Health checks passed
```

#### One-Command Deployment
```powershell
# Provision + Deploy in one command
azd up

# Equivalent to: azd provision && azd deploy
```

### Common Troubleshooting

#### Issue: "Not authenticated to Azure"
```powershell
# Solution: Login to Azure
az login

# Verify login
az account show
```

#### Issue: "Insufficient permissions"
```powershell
# Solution: Check your role on subscription
az role assignment list --assignee <your-email>

# Or get your current context
az account show --query user
```

#### Issue: "Entra configuration error"
```powershell
# Solution: Verify Entra values
azd env set AZURE_ENTRA_TENANT_ID "<correct-tenant-id>"
azd env set AZURE_ENTRA_CLIENT_ID "<correct-client-id>"
azd env set AZURE_ENTRA_AUDIENCE "api://edgefront-api-dev"

# Test with
azd env get-values | grep AZURE_ENTRA
```

#### Issue: "SQL password doesn't meet requirements"
```powershell
# Password requirements: 8-128 chars, uppercase, lowercase, digit, special char
# Valid examples: MyP@ssw0rd123! or Secure$Pass#2026

azd env set AZURE_SQL_ADMIN_PASSWORD "YourSecurePassword123!"
```

#### Issue: "Resource already exists"
```powershell
# Solution: Either delete and redeploy, or update existing
az group delete -n rg-edgefront-dev --yes
azd provision
```

---

## Production Deployment

### Pre-Deployment Checklist

#### ✅ Infrastructure Setup (1-2 hours)

- [ ] **Entra ID App Registration**
  - [ ] Created in production tenant
  - [ ] API permissions granted (Microsoft Graph: delegated, User.Read, Teams.Read.All, etc.)
  - [ ] Redirect URIs configured (frontend domain)
  - [ ] Client secret created and stored in Key Vault
  - [ ] Tenant ID, Client ID noted for deployment

- [ ] **Azure Resources**
  - [ ] Production subscription selected
  - [ ] Resource group naming convention defined
  - [ ] Backup policy: SQL backups 35 days
  - [ ] Log retention: 90 days
  - [ ] Cost budget alerts configured

- [ ] **Azure Key Vault**
  - [ ] Created in production resource group
  - [ ] SQL admin password stored
  - [ ] Entra ID secrets stored
  - [ ] Access policies configured (deployment pipeline identity)

- [ ] **Network & Security**
  - [ ] VNet peering (if applicable)
  - [ ] SQL firewall rules for production App Service
  - [ ] WAF (Web Application Firewall) configured on Static Web App (if needed)
  - [ ] DDoS protection: Standard (if required)

#### ✅ Application Readiness (1 hour)

- [ ] **Backend**
  - [ ] All unit tests passing: `dotnet test`
  - [ ] Database migrations scripted and tested
  - [ ] Error handling: No 500 errors logged with sensitive data
  - [ ] CORS origins updated to production domain
  - [ ] Logging: Structured logging in place

- [ ] **Frontend**
  - [ ] All E2E tests passing: `npx playwright test`
  - [ ] Environment variables set for production
  - [ ] Build optimized: `npm run build`
  - [ ] Bundle size acceptable (< 1MB for critical path)
  - [ ] CSP headers configured

- [ ] **Database**
  - [ ] Schema finalized and documented
  - [ ] Initial data seeded
  - [ ] Indexes created for common queries
  - [ ] Foreign key constraints verified
  - [ ] Backup tested (restore from backup)

#### ✅ Operations Readiness (2 hours)

- [ ] **Monitoring**
  - [ ] Application Insights configured
  - [ ] Alert rules created (critical, warning)
  - [ ] Runbook documentation prepared
  - [ ] On-call schedule defined
  - [ ] Escalation procedures documented

- [ ] **Compliance & Security**
  - [ ] Data classification documented
  - [ ] GDPR/CCPA compliance reviewed
  - [ ] Encryption at rest enabled
  - [ ] Encryption in transit enforced (TLS 1.2+)
  - [ ] Audit logging enabled

- [ ] **Change Management**
  - [ ] Change ticket created
  - [ ] Stakeholder approval obtained
  - [ ] Rollback plan documented
  - [ ] Communication plan finalized
  - [ ] Maintenance window scheduled

### Production-Specific Considerations

#### Scaling

**App Service Plan**:
- Start with: `Standard_B1s` (shared compute, cost-effective)
- Scale up if needed: `Standard_B2s`, `Standard_S1`, etc.
- Change via parameter: `azd env set AZURE_APPSERVICE_SKU "Standard_B2s"` + `azd provision`

**SQL Database**:
- Start with: `Standard` SKU (10GB, 250 DTU)
- Monitor DTU usage: If > 80%, scale to `Premium`
- Change via parameter: Update `sqlDatabaseSku` in `main.prod.bicepparam`

**Static Web App**:
- Standard SKU only (required for custom domains and HTTPS)
- Scale automatically (no manual scaling needed)

#### Cost Optimization Tips

1. **Reserved Instances**
   - Save 33-37% on App Service Plan with 1-year or 3-year commitment
   - Purchase via Azure Portal → Virtual Machine Scale Sets

2. **Log Analytics Retention**
   - Reduce retention from 90 to 60 days if compliance allows (saves ~30%)
   - Archive logs to Storage Account after 60 days

3. **Scheduled Scaling**
   - Scale down after business hours if traffic is predictable
   - Use Azure Automation runbooks for scheduled scaling

4. **Right-Size Resources**
   - Monitor actual usage for 30 days
   - Downgrade if over-provisioned (e.g., Standard_B1s is often sufficient)

5. **Disable Unused Services**
   - Application Insights: Only if needed (otherwise disable: `enableApplicationInsights = false`)
   - Storage Account: Can be removed if not used for blob storage

#### Safety Guards

**Manual Approval Required**:
```powershell
# Before production deployment, require human approval
# Add to CI/CD pipeline:
if ($environment -eq "prod") {
  Write-Host "⚠️ PRODUCTION DEPLOYMENT REQUIRES APPROVAL"
  $approval = Read-Host "Enter 'PROCEED' to continue"
  if ($approval -ne "PROCEED") { exit 1 }
}
```

**Automated Pre-Checks**:
```powershell
# Validate infrastructure before deployment
azd validate

# Run health checks after deployment
azd env get-values | grep -E "AZURE_FRONTEND_URL|AZURE_BACKEND_URL"
curl https://${AZURE_BACKEND_URL}/health
```

### Post-Deployment Verification

#### Test Endpoints
```powershell
# Frontend health check
curl -I https://<frontend-url>
# Expected: 200 OK

# Backend health check
curl https://<backend-url>/health
# Expected: { "status": "healthy" }

# Test API endpoint (authenticated)
curl -H "Authorization: Bearer <token>" https://<backend-url>/api/endpoint
# Expected: 200 OK with data
```

#### Monitor Initial Metrics
```powershell
# Check Application Insights for errors (should be 0)
az monitor app-insights metrics show \
  --resource-group rg-edgefront-prod \
  --app <app-insights-name> \
  --metric "server/failed" \
  --start-time 2025-01-01T00:00:00 \
  --end-time 2025-01-01T01:00:00

# Check database connectivity
sqlcmd -S <sql-fqdn> -d <database-name> -U <admin> -P <password> -Q "SELECT @@VERSION"
```

#### Verify Backups
```powershell
# List recent SQL Database backups
az sql db list-backups \
  --resource-group rg-edgefront-prod \
  --server <sql-server-name> \
  --database <database-name> \
  --query "[?slice_type=='Automated']" \
  --top 5
```

### Rollback Procedures

#### Scenario 1: Application Code Issue

**Immediate Rollback** (within 5 minutes):
```powershell
# Option 1: Redeploy previous version from Git
git checkout <previous-commit>
azd deploy

# Option 2: Manual rollback via Azure Portal
# Static Web App → Deployments → Select previous deployment → Re-deploy
# App Service → Deployments → Select previous deployment → Restart
```

#### Scenario 2: Infrastructure Configuration Issue

**Rollback Infrastructure** (within 15 minutes):
```powershell
# Restore from previous Bicep deployment
git checkout <previous-version>
azd provision  # Re-deploys previous infrastructure configuration

# Or manually via Azure Portal:
# Resource Group → Deployments → Select previous deployment → Re-deploy
```

#### Scenario 3: Database Data Issue

**Restore from Backup**:
```powershell
# List available backups (35 days)
az sql db list-backups \
  --resource-group rg-edgefront-prod \
  --server <sql-server-name> \
  --database <database-name>

# Restore to new database (for safety testing)
az sql db restore \
  --resource-group rg-edgefront-prod \
  --server <sql-server-name> \
  --name <database-name>-restored \
  --from-backup-id <backup-id>

# Swap connection string to restored database
azd env set AZURE_SQL_DATABASE_NAME "<database-name>-restored"
azd deploy
```

**Communication During Rollback**:
1. Notify stakeholders: "Issue detected, rolling back to previous version"
2. Monitor metrics during rollback
3. Notify when complete: "Service restored to version X"
4. Schedule post-incident review

---

## Maintenance & Operations

### Database Backups and Recovery

#### Backup Strategy

**Automated Backups** (included in SQL Database):
- **Full backups**: Weekly
- **Differential backups**: Daily
- **Transaction log backups**: Every 5-10 minutes
- **Retention**: 7 days (dev), 35 days (prod)

**Manual Backups** (if needed):
```powershell
# Create manual backup (BACPAC)
az sql db export \
  --resource-group rg-edgefront-prod \
  --server <sql-server-name> \
  --name <database-name> \
  --admin-user <admin> \
  --admin-password <password> \
  --storage-key <storage-key> \
  --storage-key-type "StorageAccessKey" \
  --storage-uri "https://<storage-account>.blob.core.windows.net/<container>/<filename>.bacpac"
```

#### Recovery Procedures

**Point-in-Time Restore** (within 35 days):
```powershell
# Restore database to specific point in time
az sql db restore \
  --resource-group rg-edgefront-prod \
  --server <sql-server-name> \
  --name <database-name>-restored \
  --from-backup-id <backup-id> \
  --restore-point-in-time "2025-01-01T12:00:00Z"

# Test connection to restored database
sqlcmd -S <sql-fqdn> -d <database-name>-restored -U <admin> -P <password> -Q "SELECT COUNT(*) FROM <table>"

# After validation, promote restored database or delete if not needed
az sql db delete \
  --resource-group rg-edgefront-prod \
  --server <sql-server-name> \
  --name <database-name>-restored \
  --yes
```

**Restore from BACPAC** (long-term recovery):
```powershell
# Import BACPAC to new database
az sql db import \
  --resource-group rg-edgefront-prod \
  --server <sql-server-name> \
  --name <database-name>-restored \
  --storage-uri "https://<storage-account>.blob.core.windows.net/<container>/<filename>.bacpac" \
  --storage-key <storage-key> \
  --storage-key-type "StorageAccessKey" \
  --admin-user <admin> \
  --admin-password <password>
```

### Scaling Resources

#### Horizontal Scaling (Adding Instances)

**App Service Scaling**:
```powershell
# Increase instances in App Service Plan (requires Premium/Standard tier)
az appservice plan update \
  --resource-group rg-edgefront-prod \
  --name <plan-name> \
  --number-of-workers 3

# Verify
az appservice plan show \
  --resource-group rg-edgefront-prod \
  --name <plan-name> \
  --query "sku.capacity"
```

**Auto-Scaling** (based on CPU/memory):
```powershell
# Create auto-scale settings (via Azure Portal or CLI)
# Recommended: Scale up if avg CPU > 70%, scale down if < 30%
# Min instances: 1, Max instances: 3 (adjust as needed)
```

#### Vertical Scaling (Upgrading SKU)

**Upgrade App Service Plan**:
```powershell
# Update parameter file
# main.prod.bicepparam: appServicePlanSku = "Standard_B2s"

# Re-provision (no downtime for scale-up)
azd provision
```

**Upgrade SQL Database**:
```powershell
# Update parameter file
# main.prod.bicepparam: sqlDatabaseSku = "Premium"

# Re-provision (may have brief downtime)
azd provision
```

### Cost Optimization

#### Cost Analysis

**Monthly Cost Breakdown** (typical prod):
| Resource | SKU | Monthly Cost |
|----------|-----|--------------|
| App Service Plan | Standard_B1s | $10.95 |
| Static Web App | Standard | $9.99 |
| SQL Database | Standard | $15.00 |
| Log Analytics | PerGB2018 | $25-50 |
| Storage Account | Standard LRS | $1-2 |
| App Insights | (free + overage) | $0-40 |
| **Total** | | **$62-128** |

**Cost Optimization Strategies**:

1. **Reduce Log Retention** (if compliance allows)
   - Change from 90 days → 60 days: Save ~30%
   - Change from 60 days → 30 days: Save additional 30%

2. **Right-Size SQL Database**
   - Monitor actual DTU usage (portal → Metrics)
   - If avg < 50 DTU, downgrade from Standard to Basic

3. **Reserved Instances** (1-year or 3-year)
   - App Service: 33% savings
   - SQL Database: 30% savings

4. **Scheduled Scaling** (if traffic predictable)
   - Scale down outside business hours
   - Use Azure Automation runbooks

5. **Disable Unused Services**
   - Application Insights: Only if needed
   - Storage Account: Can be removed if no blob storage required

### Updating the Template

#### Adding a New Resource

1. **Define in Bicep**:
   ```bicep
   param newResourceSku string = 'Standard'
   
   resource newResource 'Microsoft.Service/resource@2023-01-01' = {
     name: newResourceName
     location: location
     tags: commonTags
     sku: {
       name: newResourceSku
     }
     properties: {
       // configuration
     }
   }
   
   output newResourceId string = newResource.id
   ```

2. **Update Parameter Files**:
   ```bicep
   // main.dev.bicepparam
   param newResourceSku = 'Free'
   
   // main.prod.bicepparam
   param newResourceSku = 'Standard'
   ```

3. **Test Locally**:
   ```powershell
   az bicep build infra/main.bicep
   az deployment group validate \
     --resource-group rg-test \
     --template-file infra/main.bicep \
     --parameters infra/main.dev.bicepparam
   ```

4. **Deploy**:
   ```powershell
   azd provision
   ```

#### Modifying an Existing Resource

1. **Update Bicep Template**:
   ```bicep
   resource existingResource 'Microsoft.Web/sites@2022-09-01' = {
     // Add/modify properties
     properties: {
       // new configuration
     }
   }
   ```

2. **Test Deployment**:
   ```powershell
   azd provision --dry-run
   ```

3. **Deploy**:
   ```powershell
   azd provision
   ```

#### Deprecation Warnings

**Current Constraints**:
- App Service Plan: Only supports Standard tier (B-series) in this template
- SQL Database: Free SKU has 1GB limit, no SLO
- Static Web App: Only Standard SKU supported for production

**Planned Improvements**:
- [ ] Add Premium tier support for App Service
- [ ] Add configurable managed certificates
- [ ] Add custom domain setup automation
- [ ] Add Cosmos DB alternative for document workloads

---

## Troubleshooting Guide

### Common Errors and Solutions

#### 1. "Deployment failed: Template validation error"

**Symptoms**: Deployment stops immediately with schema error.

**Solutions**:
```powershell
# Validate Bicep syntax
az bicep build infra/main.bicep

# Check for typos in parameter names
# Verify all parameters are defined in template

# Test with simpler parameter file
azd deploy --debug
```

#### 2. "Insufficient permissions to complete operation"

**Symptoms**: "User does not have permission to perform action 'Microsoft.Web/sites/write'."

**Solutions**:
```powershell
# Check current role
az role assignment list --assignee <your-email>

# Verify contributor role on subscription/resource group
# If not, ask admin to grant Contributor role
az role assignment create \
  --role "Contributor" \
  --assignee <your-email> \
  --scope /subscriptions/<sub-id>
```

#### 3. "SQL Server admin password doesn't meet requirements"

**Symptoms**: "The provided password does not meet Azure SQL password policy requirements."

**Requirements**: 8-128 characters, must include:
- Uppercase letter (A-Z)
- Lowercase letter (a-z)
- Digit (0-9)
- Special character (!@#$%^&*)

**Valid Examples**:
- `MyP@ssw0rd123!`
- `Secure$Pass#2026`
- `EdgeFront!2025Dev`

**Solutions**:
```powershell
azd env set AZURE_SQL_ADMIN_PASSWORD "MyNewP@ssw0rd123!"
```

#### 4. "Static Web App deployment incomplete"

**Symptoms**: Frontend URL shows "404" or blank page.

**Solutions**:
```powershell
# SWA can take 5-10 minutes to initialize
# Wait 10 minutes, then check

# Check SWA status
az staticwebapp show \
  --resource-group rg-edgefront-dev \
  --name <swa-name>

# Check deployment logs (if GitHub Actions)
# GitHub → Actions → Workflows → Check latest run

# Redeploy manually
azd deploy
```

#### 5. "Backend not responding after deployment"

**Symptoms**: `curl https://<backend-url>` returns timeout or connection refused.

**Solutions**:
```powershell
# Backend can take 2-3 minutes to start after deployment
# Wait, then retry

# Check App Service status
az webapp show \
  --resource-group rg-edgefront-dev \
  --name <app-service-name> \
  --query "state"

# View deployment logs
az webapp log tail \
  --resource-group rg-edgefront-dev \
  --name <app-service-name>

# Check if managed identity is set up correctly
az webapp identity show \
  --resource-group rg-edgefront-dev \
  --name <app-service-name>

# Restart App Service
az webapp restart \
  --resource-group rg-edgefront-dev \
  --name <app-service-name>
```

#### 6. "Database connection failure"

**Symptoms**: Backend logs show "SqlException: Cannot connect to SQL Server."

**Solutions**:
```powershell
# Check SQL Server status
az sql server show \
  --resource-group rg-edgefront-dev \
  --name <sql-server-name>

# Check firewall rules
az sql server firewall-rule list \
  --resource-group rg-edgefront-dev \
  --server <sql-server-name>

# Verify managed identity permissions
az role assignment list \
  --scope /subscriptions/<sub-id>/resourceGroups/rg-edgefront-dev/providers/Microsoft.Sql/servers/<sql-server>/databases/<database>

# Test connection manually
sqlcmd -S <sql-fqdn> -d <database> -U <admin> -P <password> -Q "SELECT 1"
```

#### 7. "Entra ID authentication failed"

**Symptoms**: "AADSTS700016: Application with identifier 'xxx' was not found in the directory."

**Solutions**:
```powershell
# Verify app registration exists
az ad app list --display-name "EdgeFront Builder"

# Check tenant ID
az account show --query tenantId

# Verify Entra ID app settings in backend
azd env get-values | grep AZURE_ENTRA

# Update if incorrect
azd env set AZURE_ENTRA_TENANT_ID "<correct-tenant-id>"
azd env set AZURE_ENTRA_CLIENT_ID "<correct-client-id>"

# Redeploy
azd provision && azd deploy
```

#### 8. "CORS error in browser console"

**Symptoms**: "Access to XMLHttpRequest blocked by CORS policy."

**Solutions**:
```powershell
# Check CORS configuration on backend
azd env get-values | grep CORS

# Update CORS origins if needed
azd env set AZURE_CORS_ALLOWED_ORIGINS "https://frontend.azurestaticapps.net"

# Redeploy backend
azd deploy
```

### Log Inspection Procedures

#### View App Service Logs

**Option 1: Azure CLI**
```powershell
# Real-time logs
az webapp log tail \
  --resource-group rg-edgefront-dev \
  --name <app-service-name>

# Last N lines
az webapp log tail \
  --resource-group rg-edgefront-dev \
  --name <app-service-name> \
  --tail 100
```

**Option 2: Azure Portal**
- App Service → Log stream → View live logs

#### Query Log Analytics

**Common Queries**:

```kusto
# Last 10 HTTP errors
AppServiceHTTPLogs
| where CsStatus >= 400
| top 10 by TimeGenerated desc

# Response time statistics
AppServiceHTTPLogs
| summarize Avg=avg(TimeTaken), Max=max(TimeTaken), P95=percentile(TimeTaken, 95) by CsUriStem

# Failed SQL queries
SQLInsights
| where severity_level >= 2
| top 10 by timestamp desc

# Top 10 slowest queries
SQLInsights
| summarize AvgDuration=avg(query_time_ms) by query
| top 10 by AvgDuration desc
```

### Performance Diagnostics

#### Check CPU and Memory Usage

```powershell
# Get CPU percentage
az monitor metrics list \
  --resource /subscriptions/<sub-id>/resourceGroups/rg-edgefront-dev/providers/Microsoft.Web/sites/<app-service-name> \
  --metric "CpuPercentage" \
  --start-time "2025-01-01T00:00:00" \
  --end-time "2025-01-02T00:00:00" \
  --aggregation Average
```

#### Monitor SQL Database Performance

```powershell
# Check DTU usage
az sql db list-usages \
  --resource-group rg-edgefront-dev \
  --server <sql-server-name> \
  --name <database-name>

# Get query performance insights
az sql db query-store show \
  --resource-group rg-edgefront-dev \
  --server <sql-server-name> \
  --name <database-name>
```

### Contact & Escalation Procedures

**Escalation Matrix**:

| Issue Type | Severity | Response Time | Escalate To |
|-----------|----------|---------------|------------|
| 500 errors > 1% | Critical | 15 min | On-call engineer |
| Database down | Critical | 5 min | Database admin + on-call |
| Slow queries (p95 > 5s) | Warning | 1 hour | Backend team |
| High CPU (> 80%) | Warning | 30 min | DevOps team |
| Auth failures | Critical | 15 min | Security team |

**Support Channels**:
- **Slack**: #edgefront-ops (internal team)
- **Email**: devops@company.com (urgent requests)
- **On-Call**: PagerDuty (escalation only)

---

## Reference

### Configuration Files

| File | Purpose | Last Updated |
|------|---------|--------------|
| `infra/main.bicep` | Master infrastructure template | See git history |
| `infra/main.dev.bicepparam` | Dev environment parameters | See git history |
| `infra/main.prod.bicepparam` | Prod environment parameters | See git history |
| `azure.yaml` | AZD project manifest | See git history |
| `.azd/config.json` | AZD configuration | See git history |
| `infra/PARAMETERS.md` | Parameter documentation | See git history |
| `infra/OUTPUTS.md` | Output documentation | See git history |

### Related Documentation

- **Deployment Quick Start**: `DEPLOYMENT-QUICKSTART.md` (20 min guide)
- **Deployment Validation**: `infra/DEPLOYMENT-VALIDATION.md` (comprehensive validation)
- **Entra ID Setup**: `docs/setup-entra-permissions.md` (auth configuration)
- **AZD Official Docs**: https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/
- **Bicep Docs**: https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/
- **Azure SQL Docs**: https://learn.microsoft.com/en-us/azure/azure-sql/

### Parameter Reference (Summary)

**Environment & Naming** (4 parameters):
- `environment`: 'dev' or 'prod'
- `projectName`: Project identifier (max 20 chars)
- `location`: Azure region (e.g., 'eastus')
- `resourceNamePrefix`: Prefix for all resources (max 15 chars)

**Compute** (3 parameters):
- `appServicePlanSku`: App Service Plan SKU (e.g., 'Standard_B1s')
- `appServicePlanTier`: Tier ('Standard')
- `appServiceRuntimeStack`: Runtime (e.g., 'DOTNETCORE|10.0')

**Database** (5 parameters):
- `sqlServerAdminUsername`: SQL admin username
- `sqlServerAdminPassword`: SQL admin password (must be complex)
- `sqlDatabaseSku`: Database SKU ('Free' or 'Standard')
- `sqlDatabaseMaxSizeBytes`: Max database size in bytes
- `backupRetentionDays`: Retention days (1-35)

**Frontend** (3 parameters):
- `staticWebAppsSku`: SWA tier ('Free' or 'Standard')
- `staticWebAppsRuntimeStack`: Runtime ('node')
- `nodeVersion`: Node.js version ('lts')

**Monitoring** (3 parameters):
- `enableApplicationInsights`: Boolean (true/false)
- `applicationsInsightsSku`: SKU ('PerGB2018')
- `logAnalyticsRetentionDays`: Retention days (1-730)

**Application** (5 parameters):
- `entraadTenantId`: Entra ID tenant ID (GUID)
- `entraadClientId`: App registration client ID (GUID)
- `entraadAudience`: API audience URI (e.g., 'api://edgefront-api-dev')
- `graphBaseUrl`: Graph API endpoint (e.g., 'https://graph.microsoft.com/v1.0')
- `corsAllowedOrigins`: Array of CORS origins

**Security** (3 parameters):
- `enableManagedIdentity`: Boolean (true/false)
- `enableEncryption`: Boolean (true/false)
- `enableDiagnostics`: Boolean (true/false)

**Tags** (1 parameter):
- `commonTags`: Object with environment, project, costCenter, etc.

### Output Reference (Summary)

| Output | Type | Example | Usage |
|--------|------|---------|-------|
| `frontendUrl` | string | `https://xxx.azurestaticapps.net` | Display to user, CORS config |
| `backendUrl` | string | `https://xxx.azurewebsites.net` | API endpoint for frontend |
| `sqlServerFqdn` | string | `xxx.database.windows.net` | Connection string building |
| `sqlDatabaseName` | string | `edgefrontdb` | Database reference |
| `appInsightsInstrumentationKey` | string | GUID | SDK initialization |
| `logAnalyticsWorkspaceId` | string | Resource ID | Log querying |
| `managedIdentityId` | string | Resource ID | Identity reference |
| `appServiceId` | string | Resource ID | Portal navigation |
| `deploymentSummary` | object | See template | Quick reference |

### Useful Commands

```powershell
# List all resources in resource group
az resource list -g rg-edgefront-dev --output table

# View deployment outputs
azd env get-values

# Get resource details
az webapp show -g rg-edgefront-dev -n <app-service-name>

# Delete entire resource group (CAUTION)
az group delete -n rg-edgefront-dev --yes

# Monitor live logs
az webapp log tail -g rg-edgefront-dev -n <app-service-name>

# Export current template
az deployment group export -g rg-edgefront-dev

# List all role assignments
az role assignment list -g rg-edgefront-dev
```

---

## Summary

EdgeFront Builder's infrastructure is a **modern, cloud-native deployment** with:

✅ **14 Azure resources** fully defined in Bicep IaC  
✅ **Automated provisioning** via Azure Developer CLI  
✅ **Production-grade security** with managed identities and RBAC  
✅ **Comprehensive monitoring** with Application Insights and Log Analytics  
✅ **Multi-environment support** (dev/prod with different configurations)  
✅ **Cost-optimized** for both development and production use  

For questions or updates, refer to the documentation files in this repository or contact the DevOps team.

---

**Document Version**: 1.0  
**Last Updated**: 2025  
**Maintained By**: EdgeFront DevOps Team  
**Status**: ✅ Complete and Production-Ready
