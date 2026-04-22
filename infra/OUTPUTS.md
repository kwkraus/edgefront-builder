# EdgeFront Builder Bicep Outputs Documentation

## Overview
This document describes all outputs from the `main.bicep` template. These outputs are used by AZD hooks, the Azure Portal, and post-deployment configuration scripts.

## Output Reference

### Frontend & Backend URLs

#### `frontendUrl`
- **Type**: `string`
- **Description**: HTTPS URL for the Static Web App frontend (Next.js 16)
- **Example (dev)**: `https://example-swa-abc123.azurestaticapps.net`
- **Example (prod)**: `https://example-swa-prod-def456.azurestaticapps.net`
- **Usage**: 
  - Displayed in console after deployment
  - Used by AZD to configure CORS allowed origins on backend
  - Configure with frontend in `next.config.ts` if needed

#### `backendUrl`
- **Type**: `string`
- **Description**: HTTPS URL for the App Service backend API (.NET 10)
- **Example (dev)**: `https://example-app-dev.azurewebsites.net`
- **Example (prod)**: `https://example-app-prod.azurewebsites.net`
- **Usage**:
  - Displayed in console after deployment
  - Used by frontend environment variables (configured via postprovision hook)
  - Must be added to Static Web App configuration or CORS headers

### Database

#### `sqlServerFqdn`
- **Type**: `string`
- **Description**: Fully qualified domain name (FQDN) of the SQL Server
- **Example**: `example-sql-abc123.database.windows.net`
- **Usage**:
  - Used to build connection strings from client applications
  - Added to backend app settings as `Database__Host` (if needed)
  - Required for firewall rules if accessing from outside Azure

#### `sqlDatabaseName`
- **Type**: `string`
- **Description**: Name of the Azure SQL Database
- **Default**: `${projectName}db` (e.g., `edgefrontdb`)
- **Usage**:
  - Used in connection strings (Initial Catalog parameter)
  - Displayed in console for reference
  - Fixed at deployment time; cannot be changed without redeployment

### Monitoring & Observability

#### `appInsightsInstrumentationKey`
- **Type**: `string` (empty if `enableApplicationInsights` is false)
- **Description**: Instrumentation key for Application Insights
- **Example**: `12345678-1234-1234-1234-123456789012`
- **Usage**:
  - Legacy format; primarily for backward compatibility
  - Used by SDKs to identify which App Insights resource to report to
  - Already injected into App Service app settings (`APPLICATIONINSIGHTS_CONNECTION_STRING`)

#### `appInsightsConnectionString`
- **Type**: `string` (empty if `enableApplicationInsights` is false)
- **Description**: Connection string for Application Insights
- **Example**: `InstrumentationKey=12345678-1234-1234-1234-123456789012;IngestionEndpoint=https://eastus-0.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/`
- **Usage**:
  - Injected into App Service via `APPLICATIONINSIGHTS_CONNECTION_STRING` app setting
  - Used by Application Insights SDK in .NET runtime
  - Allows backend to send telemetry, logs, and exceptions to App Insights

#### `logAnalyticsWorkspaceId`
- **Type**: `string`
- **Description**: Resource ID of the Log Analytics workspace
- **Example**: `/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/example-rg/providers/Microsoft.OperationalInsights/workspaces/example-logs`
- **Usage**:
  - Diagnostic settings are automatically connected to this workspace
  - Can be used to query logs via KQL (Kusto Query Language)
  - Central sink for all SQL and App Service diagnostics

### Security & Identity

#### `managedIdentityId`
- **Type**: `string` (empty if `enableManagedIdentity` is false)
- **Description**: Resource ID of the User-Assigned Managed Identity
- **Example**: `/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/example-rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/example-identity`
- **Usage**:
  - Assigned to App Service to enable passwordless SQL access
  - Can be used for other Azure service integrations (e.g., Key Vault, Storage)
  - Replaces SQL username/password authentication

#### `managedIdentityPrincipalId`
- **Type**: `string` (empty if `enableManagedIdentity` is false)
- **Description**: Principal ID (object ID) of the Managed Identity
- **Example**: `87654321-4321-4321-4321-210987654321`
- **Usage**:
  - Used for RBAC role assignments (already assigned SQL Database Data Reader/Writer role)
  - Can be used to grant additional permissions to other Azure resources
  - Identifies the identity in Azure AD audit logs

### Resource Identifiers

#### `appServiceId`, `appServicePlanId`, `sqlServerId`, `sqlDatabaseId`, `storageAccountId`, `staticWebAppId`
- **Type**: `string`
- **Description**: Azure Resource Manager (ARM) resource IDs for infrastructure components
- **Usage**:
  - Used by infrastructure management tools and scripts
  - Can be passed to other Bicep modules or ARM templates
  - Useful for scripting resource-level operations

### Azure Portal Links

#### `portalAppServiceLink`, `portalSqlDatabaseLink`, `portalAppInsightsLink`, `portalLogAnalyticsLink`
- **Type**: `string`
- **Description**: Direct links to each resource in Azure Portal overview pages
- **Format**: `https://portal.azure.com/#resource{resourceId}/overview`
- **Usage**:
  - Printed in console after deployment for quick access
  - Copied into runbooks or documentation for operators
  - Eliminates manual search in Azure Portal

### Deployment Summary

#### `deploymentSummary`
- **Type**: `object`
- **Description**: Comprehensive snapshot of deployment configuration and URLs
- **Contents**:
  ```json
  {
    "environment": "dev|prod",
    "projectName": "edgefront",
    "location": "eastus|westus|etc",
    "resourceNamePrefix": "example",
    "frontendUrl": "https://example-swa.azurestaticapps.net",
    "backendUrl": "https://example-app.azurewebsites.net",
    "sqlFqdn": "example-sql.database.windows.net",
    "appInsightsEnabled": true|false,
    "managedIdentityEnabled": true|false,
    "diagnosticsEnabled": true|false
  }
  ```
- **Usage**:
  - Printed as JSON by postprovision.ps1 hook for operator visibility
  - Can be stored in a deployment manifest or CI/CD artifact
  - Useful for environment documentation and change tracking

---

## AZD Integration

### `azd env get-values`
After deployment, run:
```bash
azd env get-values
```

This retrieves all outputs and stores them in `.env` files:
- `FRONTEND_URL` → `frontendUrl`
- `BACKEND_URL` → `backendUrl`
- `SQL_SERVER_FQDN` → `sqlServerFqdn`
- etc.

These can then be referenced in postprovision hooks via `$env:FRONTEND_URL`, etc.

### Postprovision Hook Usage
The `postprovision.ps1` PowerShell script uses these outputs to:
1. Display a summary of deployed resources
2. Configure frontend and backend with each other's URLs
3. Provide links for operator quick access to resources

---

## Environment-Specific Notes

### Development (`dev`)
- Static Web Apps: **Free** tier (1 free static site)
- SQL Database: **Free** tier (40 DTU max, 5 GB storage)
- App Service: **B1** (1 core, 1.75 GB RAM)
- Backup retention: 7 days
- Example resource prefix: `edgefront-dev`

### Production (`prod`)
- Static Web Apps: **Standard** tier (unlimited deployments, custom domain)
- SQL Database: **Standard S0** tier (10 DTUs)
- App Service: **B2** or higher (2 cores, 3.5 GB RAM)
- Backup retention: 35 days
- Example resource prefix: `edgefront-prod`

---

## Troubleshooting

### Empty Outputs
If outputs are empty:
- Check `enableApplicationInsights`, `enableManagedIdentity`, `enableDiagnostics` parameters
- Conditional outputs only populate if their feature is enabled
- Review deployment logs for errors during resource creation

### CORS Issues
- Add `backendUrl` to Static Web App environment variable
- Verify `corsAllowedOrigins` parameter includes frontend URL
- Check App Service app settings for `CORS__AllowedOrigins`

### Database Connectivity
- Ensure SQL firewall rule "AllowAzureServices" is active (created by template)
- For managed identity access, verify role assignment (created if `enableManagedIdentity` is true)
- Test connection string with SQL Server Management Studio before deployment
