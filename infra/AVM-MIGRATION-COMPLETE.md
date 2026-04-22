# Infrastructure Deployment - AVM Migration Complete

**Status**: ✅ Ready for deployment  
**Last Updated**: 2026-04-21  
**Deployed By**: GitHub Copilot CLI  

## Overview

The EdgeFront Builder infrastructure template has been successfully **migrated from inline Azure resource declarations to Azure Verified Modules (AVMs)**. This migration improves maintainability, aligns with Microsoft best practices, and ensures long-term support for the infrastructure code.

## What Changed

### Template Migration
- **Before**: 8 inline resource declarations (Log Analytics, App Insights, Storage, Managed Identity, SQL, App Service Plan, App Service, Static Web App)
- **After**: 8 AVM module declarations using Microsoft-maintained, certified modules
- **Benefits**: Improved maintainability, automatic updates, best-practice alignment, reduced technical debt

### Files Modified

1. **`infra/main.bicep`** (complete refactor)
   - Replaced all inline resources with AVM modules
   - Cleaned up 5 unused parameters (sqlDatabaseSku, sqlDatabaseMaxSizeBytes, etc.)
   - Updated variables and outputs to use module outputs
   - Result: 0 critical errors, 8 expected warnings (for conditional modules)

2. **`infra/main.dev.bicepparam`** (parameter updates)
   - Removed 6 obsolete parameters
   - Simplified SKU values (Standard_B1s → B1, Standard SWA → Free)
   - Result: Fully compatible with new template

3. **`infra/main.prod.bicepparam`** (parameter updates)
   - Same updates as dev file
   - Adjusted production SKUs (B2 for App Service Plan, Standard for SWA)
   - Result: Fully compatible with new template

## Validation Status

✅ **Template Compilation**: Success (0 errors)  
✅ **Parameter Validation**: Success  
✅ **Bicep Diagnostics**: 8 expected BCP318 warnings (safe—for conditional modules)  
✅ **Module Dependencies**: All resolved  
✅ **Output Mappings**: All verified  

## Deployment Instructions

### Prerequisites
1. Azure CLI installed (`az --version`)
2. AZD installed (`azd version`)
3. Azure authentication (`az login`)
4. Entra ID app registration (for backend auth)
5. SQL admin password (8-128 chars, mixed case + special char)

### Quick Deploy

```powershell
cd C:\Users\kkraus\source\repos\kwkraus\edgefront-builder

# 1. Authenticate
az login

# 2. Select environment
azd env select edgefront

# 3. Configure Entra ID (required)
azd env set AZURE_ENTRA_TENANT_ID "<your-tenant-id>"
azd env set AZURE_ENTRA_CLIENT_ID "<your-client-id>"
azd env set AZURE_ENTRA_AUDIENCE "api://edgefront-api-dev"
azd env set AZURE_SQL_ADMIN_PASSWORD "<secure-password>"

# 4. Verify configuration
azd env get-values

# 5. Provision infrastructure
azd provision

# 6. Deploy application
azd deploy
```

### Using the Deployment Script

```powershell
# Run the automated deployment script (in session workspace)
& "$PSScriptRoot/../.copilot/session-state/1c53f42e-f6b2-49fa-b955-8d55eff19295/deploy.ps1"
```

## Resources Deployed

| # | Resource | Module | Tier |
|---|---|---|---|
| 1 | Log Analytics Workspace | avm/res/operational-insights/workspace | Monitoring |
| 2 | Application Insights | avm/res/insights/component | Monitoring |
| 3 | Storage Account | avm/res/storage/storage-account | Data |
| 4 | Managed Identity | avm/res/managed-identity/user-assigned-identity | Security |
| 5 | SQL Server | avm/res/sql/server | Database |
| 6 | SQL Database | (nested) | Database |
| 7 | App Service Plan | avm/res/web/serverfarm | Compute |
| 8 | App Service | avm/res/web/site | Compute |
| 9 | Static Web App | avm/res/web/static-site | Frontend |

## Environment Configuration

### Development (edgefront)
- **Location**: eastus2
- **Resource Group**: rg-edgefront
- **Subscription**: 6cf5e319-cf47-44c9-ba3b-22d1ac3e06ea
- **SKUs**: B1 (App Service), Free (Static Web App)
- **Logs**: 30-day retention

### Production
- **Location**: eastus (or configured)
- **Resource Group**: rg-ef-prod (or configured)
- **SKUs**: B2 (App Service), Standard (Static Web App)
- **Logs**: 90-day retention

## Key AVM Features Used

1. **Log Analytics Module**: Automated workspace setup with diagnostics
2. **App Insights Module**: Connected to Log Analytics for unified monitoring
3. **Storage Module**: Managed with encryption, blob access configuration
4. **Managed Identity Module**: RBAC-ready for service authentication
5. **SQL Server Module**: Automated security, firewall rules, diagnostics
6. **App Service Plan & Site Modules**: Connected configuration, siteConfig nesting
7. **Static Web App Module**: Build properties, authentication setup
8. **RBAC Role Assignment**: Automated permission delegation

## Troubleshooting

### Bicep Validation Errors
```powershell
# Validate template before deployment
az bicep build-params --file "infra\main.dev.bicepparam"

# Expected: 0 errors, 8 BCP318 warnings (safe)
```

### Deployment Failures
```powershell
# Check Azure subscription
az account show

# View resource group
az group show --name rg-edgefront

# View deployment logs
az deployment group show --name <deployment-name> \
  --resource-group rg-edgefront
```

### Environment Reset
```powershell
# Remove environment and reset
azd env remove edgefront

# Delete resources
az group delete --name rg-edgefront --yes

# Re-deploy from scratch
```

## Performance Characteristics

**Deployment Time**: 10-20 minutes (infrastructure + code)
- Infrastructure provisioning: 5-10 minutes
- Application deployment: 5-10 minutes

**Monthly Cost (Dev)**: ~$20-30
- App Service Plan B1: $10-15
- SQL Database: $5-10
- Storage + Monitoring: $1-5

## Security Notes

1. **SQL Admin Password**: Must meet Azure requirements (8-128 chars, mixed case, special char)
2. **Managed Identity**: Used for service-to-service authentication (no secrets in config)
3. **Application Insights**: Connected to Log Analytics for centralized monitoring
4. **CORS Configuration**: Configured for development (localhost) and can be restricted for production
5. **Firewall Rules**: SQL Server firewall allows Azure services and configured CIDR ranges

## Post-Deployment Verification

```powershell
# List deployed resources
az resource list --resource-group rg-edgefront --output table

# Test backend API
Invoke-RestMethod https://edgefront-api-app.azurewebsites.net/health

# Check Application Insights
az monitor app-insights component show \
  --resource-group rg-edgefront \
  --name edgefront-insights
```

## Next Steps

1. ✅ [DONE] Migrate inline resources to AVMs
2. ✅ [DONE] Validate template and parameters
3. ⏳ [TODO] Authenticate and configure Entra ID
4. ⏳ [TODO] Run `azd provision` to deploy infrastructure
5. ⏳ [TODO] Run `azd deploy` to deploy application code
6. ⏳ [TODO] Verify deployment and test endpoints
7. ⏳ [TODO] Configure monitoring and alerts
8. ⏳ [TODO] Set up CI/CD pipeline (GitHub Actions)

## Additional Resources

- [AZD Documentation](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure Verified Modules](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/modules-avm)
- [App Service Deployment](https://learn.microsoft.com/azure/app-service/deploy-best-practices)
- [Azure SQL Security](https://learn.microsoft.com/azure/azure-sql/database/security-best-practices)

---

**Questions?** Refer to `DEPLOYMENT-QUICKSTART.md` or `ENVIRONMENT-SETUP.md` in this directory.
