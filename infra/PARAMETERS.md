# Bicep Parameter Schema

## Overview

This document defines all configurable parameters for the EdgeFront Builder Bicep infrastructure-as-code templates. Parameters support dev and prod environments with environment-specific defaults and validation rules.

Parameters are organized into logical sections:
- Environment & Naming
- Compute (App Service)
- Database (SQL)
- Frontend (Static Web Apps)
- Monitoring (Application Insights)
- Application Configuration
- Security & Networking
- Tags

---

## Parameter Definitions

### Environment & Naming

| Parameter | Type | Dev Default | Prod Default | Required | Description | Validation |
|-----------|------|-------------|--------------|----------|-------------|------------|
| environment | string | `dev` | `prod` | Yes | Deployment environment name | Must be 'dev' or 'prod' |
| projectName | string | `edgefront` | `edgefront` | Yes | Project name for resource naming | Alphanumeric, max 20 chars |
| location | string | `eastus` | `eastus` | Yes | Azure region for resource deployment | Valid Azure region |
| resourceNamePrefix | string | `ef-${environment}` | `ef-${environment}` | Yes | Prefix for all resource names | Alphanumeric, lowercase, hyphens, max 15 chars |

**Example values:**
- Dev: `ef-dev` 
- Prod: `ef-prod`

---

### Compute (App Service)

| Parameter | Type | Dev Default | Prod Default | Required | Description | Validation |
|-----------|------|-------------|--------------|----------|-------------|------------|
| appServicePlanSku | string | `Standard_B1s` | `Standard_B1s` | Yes | App Service Plan SKU | Predefined SKU values |
| appServicePlanTier | string | `Standard` | `Standard` | Yes | App Service Plan tier | 'Free', 'Basic', 'Standard', 'Premium' |
| appServiceRuntimeStack | string | `DOTNETCORE\|10.0` | `DOTNETCORE\|10.0` | Yes | ASP.NET Core runtime stack | Format: `DOTNETCORE|VERSION` |

**Notes:**
- SKU and Tier are uniform across dev and prod per requirements
- .NET 10.0 is the required runtime for EdgeFront backend
- App Service will host the ASP.NET Core minimal API backend

---

### Database (SQL)

| Parameter | Type | Dev Default | Prod Default | Required | Description | Validation |
|-----------|------|-------------|--------------|----------|-------------|------------|
| sqlServerAdminUsername | string | N/A | N/A | Yes | SQL Server administrator login | Min 1, max 128 chars; no reserved names |
| sqlServerAdminPassword | securestring | N/A | N/A | Yes | SQL Server administrator password | Min 8 chars, must include uppercase, lowercase, numbers, special chars |
| sqlDatabaseSku | string | `Free` | `Standard` | Yes | SQL Database SKU | 'Free' (dev), 'Standard' (prod), 'Premium' |
| sqlDatabaseMaxSizeBytes | int | `1073741824` (1 GB) | `10737418240` (10 GB) | Yes | Maximum database size in bytes | 104,857,600 (100 MB) to 1,099,511,627,776 (1 TB) |
| backupRetentionDays | int | `7` | `35` | Yes | Backup retention period in days | 1-35 days |

**Notes:**
- Free tier is for development only; not suitable for production
- Standard tier provides better performance and backup options
- Secure string parameter ensures password is not logged
- Backup retention aligns with dev/prod operational requirements

---

### Frontend (Static Web Apps)

| Parameter | Type | Dev Default | Prod Default | Required | Description | Validation |
|-----------|------|-------------|--------------|----------|-------------|------------|
| staticWebAppsSku | string | `Standard` | `Standard` | Yes | Static Web Apps SKU | 'Free', 'Standard' |
| staticWebAppsRuntimeStack | string | `node` | `node` | Yes | Runtime stack for SWA | 'node', 'python', 'dotnet' |
| nodeVersion | string | `lts` | `lts` | Yes | Node.js version for SWA | 'lts' or specific version (e.g., '18.0.0') |

**Notes:**
- Static Web Apps hosts the Next.js 16 frontend
- Standard SKU enables custom domains and SSL
- Node.js LTS is recommended for production stability

---

### Monitoring (Application Insights)

| Parameter | Type | Dev Default | Prod Default | Required | Description | Validation |
|-----------|------|-------------|--------------|----------|-------------|------------|
| enableApplicationInsights | bool | `true` | `true` | Yes | Enable Application Insights monitoring | true or false |
| applicationsInsightsSku | string | `PerGB2018` | `PerGB2018` | Yes | Application Insights pricing model | 'PerGB2018', 'Basic' |
| logAnalyticsRetentionDays | int | `30` | `90` | Yes | Log Analytics data retention in days | 1-730 days |

**Notes:**
- Application Insights required for production observability
- PerGB2018 pricing is pay-as-you-go with 30 days free ingestion
- Dev: 30 days retention (cost-optimized)
- Prod: 90 days retention (compliance/audit trail)

---

### Application Configuration

| Parameter | Type | Dev Default | Prod Default | Required | Description | Validation |
|-----------|------|-------------|--------------|----------|-------------|------------|
| entraadTenantId | string | N/A | N/A | Yes | Azure AD tenant ID (GUID) | Valid Azure AD tenant ID |
| entraadClientId | string | N/A | N/A | Yes | App registration client ID (GUID) | Valid client ID |
| entraadAudience | string | N/A | N/A | Yes | App registration audience URI | Valid URI format (e.g., `api://edgefront-api`) |
| graphBaseUrl | string | `https://graph.microsoft.com/v1.0` | `https://graph.microsoft.com/v1.0` | Yes | Microsoft Graph API endpoint | Valid HTTPS URI |
| corsAllowedOrigins | array | `['http://localhost:3000']` | `['https://app.edgefront.com']` | Yes | CORS allowed origins for backend | Array of valid URIs |

**Notes:**
- Entra ID parameters must be obtained from Azure app registrations
- Graph base URL can be overridden for sovereign clouds if needed
- CORS origins are environment-specific and must match frontend deployment URLs
- Dev: localhost for local development
- Prod: Production domain (example shown; actual value depends on deployment)

---

### Security & Networking

| Parameter | Type | Dev Default | Prod Default | Required | Description | Validation |
|-----------|------|-------------|--------------|----------|-------------|------------|
| enableManagedIdentity | bool | `true` | `true` | Yes | Enable managed identity for resources | true or false |
| enableEncryption | bool | `true` | `true` | Yes | Enable encryption at rest | true or false |
| enableDiagnostics | bool | `true` | `true` | Yes | Enable diagnostic logging | true or false |

**Notes:**
- Managed identity eliminates need for stored credentials
- Encryption at rest protects sensitive data in storage
- Diagnostics required for monitoring and compliance
- All security features enabled in both dev and prod

---

### Tags

| Parameter | Type | Dev Default | Prod Default | Required | Description | Validation |
|-----------|------|-------------|--------------|----------|-------------|------------|
| commonTags | object | See examples | See examples | Yes | Tags applied to all resources | Object with string key-value pairs |

**Tag Keys (Required):**
- `environment`: Value from environment parameter
- `project`: Value from projectName parameter
- `costCenter`: Cost allocation identifier
- `createdBy`: Creator identifier
- `createdDate`: ISO 8601 date string

**Example Dev Tags:**
```json
{
  "environment": "dev",
  "project": "edgefront",
  "costCenter": "engineering",
  "createdBy": "devops",
  "createdDate": "2024-01-15T10:30:00Z"
}
```

**Example Prod Tags:**
```json
{
  "environment": "prod",
  "project": "edgefront",
  "costCenter": "operations",
  "createdBy": "deployment-pipeline",
  "createdDate": "2024-01-15T14:45:00Z"
}
```

---

## Environment-Specific Defaults Summary

### Development Environment (dev)

```bicepparam
param environment = 'dev'
param projectName = 'edgefront'
param location = 'eastus'
param resourceNamePrefix = 'ef-dev'

param appServicePlanSku = 'Standard_B1s'
param appServicePlanTier = 'Standard'
param appServiceRuntimeStack = 'DOTNETCORE|10.0'

param sqlDatabaseSku = 'Free'
param sqlDatabaseMaxSizeBytes = 1073741824 // 1 GB
param backupRetentionDays = 7

param staticWebAppsSku = 'Standard'
param staticWebAppsRuntimeStack = 'node'
param nodeVersion = 'lts'

param enableApplicationInsights = true
param applicationsInsightsSku = 'PerGB2018'
param logAnalyticsRetentionDays = 30

param graphBaseUrl = 'https://graph.microsoft.com/v1.0'
param corsAllowedOrigins = [ 'http://localhost:3000' ]

param enableManagedIdentity = true
param enableEncryption = true
param enableDiagnostics = true

param commonTags = {
  environment: 'dev'
  project: 'edgefront'
  costCenter: 'engineering'
  createdBy: 'devops'
  createdDate: utcNow('u')
}

// Required - no defaults (must be provided)
// param sqlServerAdminUsername = '<admin-username>'
// param sqlServerAdminPassword = '<admin-password>'
// param entraadTenantId = '<tenant-id>'
// param entraadClientId = '<client-id>'
// param entraadAudience = '<audience-uri>'
```

### Production Environment (prod)

```bicepparam
param environment = 'prod'
param projectName = 'edgefront'
param location = 'eastus'
param resourceNamePrefix = 'ef-prod'

param appServicePlanSku = 'Standard_B1s'
param appServicePlanTier = 'Standard'
param appServiceRuntimeStack = 'DOTNETCORE|10.0'

param sqlDatabaseSku = 'Standard'
param sqlDatabaseMaxSizeBytes = 10737418240 // 10 GB
param backupRetentionDays = 35

param staticWebAppsSku = 'Standard'
param staticWebAppsRuntimeStack = 'node'
param nodeVersion = 'lts'

param enableApplicationInsights = true
param applicationsInsightsSku = 'PerGB2018'
param logAnalyticsRetentionDays = 90

param graphBaseUrl = 'https://graph.microsoft.com/v1.0'
param corsAllowedOrigins = [ 'https://app.edgefront.com' ]

param enableManagedIdentity = true
param enableEncryption = true
param enableDiagnostics = true

param commonTags = {
  environment: 'prod'
  project: 'edgefront'
  costCenter: 'operations'
  createdBy: 'deployment-pipeline'
  createdDate: utcNow('u')
}

// Required - no defaults (must be provided)
// param sqlServerAdminUsername = '<admin-username>'
// param sqlServerAdminPassword = '<admin-password>'
// param entraadTenantId = '<tenant-id>'
// param entraadClientId = '<client-id>'
// param entraadAudience = '<audience-uri>'
```

---

## Parameter Validation Rules

### String Parameters

- **environment**: Must match regex `^(dev|prod)$`
- **projectName**: Must match regex `^[a-zA-Z0-9]{1,20}$`
- **location**: Must be valid Azure region (e.g., eastus, westus, northeurope)
- **resourceNamePrefix**: Must match regex `^[a-z0-9-]{1,15}$` (lowercase, alphanumeric, hyphens)
- **appServiceRuntimeStack**: Must follow format `DOTNETCORE|VERSION` where VERSION matches `\d+\.\d+`
- **sqlServerAdminUsername**: No reserved SQL Server names (admin, sa, etc.)

### Integer Parameters

- **sqlDatabaseMaxSizeBytes**: Range 104,857,600 (100 MB) to 1,099,511,627,776 (1 TB)
- **backupRetentionDays**: Range 1-35
- **logAnalyticsRetentionDays**: Range 1-730

### Array Parameters

- **corsAllowedOrigins**: Non-empty array of valid HTTPS/HTTP URIs
- Dev environments may use HTTP (localhost)
- Prod environments must use HTTPS only

### Secure String Parameters

- **sqlServerAdminPassword**: Must be 8-128 characters and contain:
  - At least one uppercase letter (A-Z)
  - At least one lowercase letter (a-z)
  - At least one digit (0-9)
  - At least one special character (!@#$%^&*)
  - Not contain username or prohibited characters

---

## Usage Examples

### Deploying to Development

Create `infra/main.dev.bicepparam`:
```bicepparam
using './main.bicep'

param environment = 'dev'
param projectName = 'edgefront'
param location = 'eastus'
param resourceNamePrefix = 'ef-dev'

param appServicePlanSku = 'Standard_B1s'
param appServicePlanTier = 'Standard'
param appServiceRuntimeStack = 'DOTNETCORE|10.0'

param sqlServerAdminUsername = 'sqladmin'
param sqlServerAdminPassword = 'SecureP@ss123!'
param sqlDatabaseSku = 'Free'
param sqlDatabaseMaxSizeBytes = 1073741824
param backupRetentionDays = 7

param staticWebAppsSku = 'Standard'
param staticWebAppsRuntimeStack = 'node'
param nodeVersion = 'lts'

param enableApplicationInsights = true
param applicationsInsightsSku = 'PerGB2018'
param logAnalyticsRetentionDays = 30

param entraadTenantId = '00000000-0000-0000-0000-000000000000'
param entraadClientId = '11111111-1111-1111-1111-111111111111'
param entraadAudience = 'api://edgefront-api-dev'
param graphBaseUrl = 'https://graph.microsoft.com/v1.0'
param corsAllowedOrigins = [ 'http://localhost:3000' ]

param enableManagedIdentity = true
param enableEncryption = true
param enableDiagnostics = true

param commonTags = {
  environment: 'dev'
  project: 'edgefront'
  costCenter: 'engineering'
  createdBy: 'devops'
  createdDate: utcNow('u')
}
```

### Deploying to Production

Create `infra/main.prod.bicepparam`:
```bicepparam
using './main.bicep'

param environment = 'prod'
param projectName = 'edgefront'
param location = 'eastus'
param resourceNamePrefix = 'ef-prod'

param appServicePlanSku = 'Standard_B1s'
param appServicePlanTier = 'Standard'
param appServiceRuntimeStack = 'DOTNETCORE|10.0'

param sqlServerAdminUsername = 'prodadmin'
param sqlServerAdminPassword = 'SecureP@ss123!Prod'
param sqlDatabaseSku = 'Standard'
param sqlDatabaseMaxSizeBytes = 10737418240
param backupRetentionDays = 35

param staticWebAppsSku = 'Standard'
param staticWebAppsRuntimeStack = 'node'
param nodeVersion = 'lts'

param enableApplicationInsights = true
param applicationsInsightsSku = 'PerGB2018'
param logAnalyticsRetentionDays = 90

param entraadTenantId = '00000000-0000-0000-0000-000000000000'
param entraadClientId = '22222222-2222-2222-2222-222222222222'
param entraadAudience = 'api://edgefront-api-prod'
param graphBaseUrl = 'https://graph.microsoft.com/v1.0'
param corsAllowedOrigins = [ 'https://app.edgefront.com' ]

param enableManagedIdentity = true
param enableEncryption = true
param enableDiagnostics = true

param commonTags = {
  environment: 'prod'
  project: 'edgefront'
  costCenter: 'operations'
  createdBy: 'deployment-pipeline'
  createdDate: utcNow('u')
}
```

### CLI Deployment Commands

**Development:**
```bash
az deployment group create \
  --resource-group ef-dev-rg \
  --template-file infra/main.bicep \
  --parameters infra/main.dev.bicepparam
```

**Production:**
```bash
az deployment group create \
  --resource-group ef-prod-rg \
  --template-file infra/main.bicep \
  --parameters infra/main.prod.bicepparam
```

---

## Parameter Dependencies and Notes

### Cross-Parameter References

1. **Resource Naming**: `resourceNamePrefix` is used to construct names for all resources
   - App Service: `${resourceNamePrefix}-app`
   - SQL Server: `${resourceNamePrefix}-sql`
   - Storage Account: `${resourceNamePrefix}storage` (no hyphens)
   - Static Web App: `${resourceNamePrefix}-swa`

2. **Environment Coupling**:
   - `environment` parameter must match the environment-specific bicepparam file
   - Mismatch will result in incorrect tagging and naming

3. **Security Parameters**:
   - `enableManagedIdentity` should always be true to eliminate stored credentials
   - `enableEncryption` protects data at rest in SQL Database and Storage accounts
   - `enableDiagnostics` routes logs to Application Insights and Log Analytics

4. **CORS Configuration**:
   - Must include all frontend deployment URLs
   - Add both staging and production URLs if applicable
   - Update when frontend domain changes

---

## Next Steps

1. **main.bicep** - Implement the main template using these parameters
2. **main.dev.bicepparam** - Create dev parameters file
3. **main.prod.bicepparam** - Create prod parameters file
4. **CI/CD Integration** - Update deployment pipelines to use these parameter files
