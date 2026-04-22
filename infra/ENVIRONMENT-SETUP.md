# Azure Developer CLI Environment Setup

This guide explains how to manage and switch between development and production environments using Azure Developer CLI (AZD).

## Quick Start

### Select Development Environment (Default)
```bash
azd env select dev
```

### Select Production Environment
```bash
azd env select prod
```

### View Current Environment Settings
```bash
azd env get-values
```

## Environment Structure

EdgeFront Builder uses two environments:

| Environment | Location | SKU | Database | Purpose |
|---|---|---|---|---|
| **dev** | eastus | Standard_B1s | Free | Local development and testing |
| **prod** | eastus | Standard_B1s | Standard | Production deployment |

### Environment Files

```
.azd/
├── config.json                 # AZD workspace defaults
└── environments/
    ├── .env.dev               # Dev environment variables
    └── .env.prod              # Prod environment variables
```

## Setting Environment Variables

### Automatic Load
When you run `azd` commands, AZD automatically loads variables from the selected environment's `.env.*` file.

### Manual Override
Override an environment variable for the current session:

```bash
# Set a temporary override
azd env set VARIABLE_NAME value

# View all environment variables
azd env get-values

# Unset a variable
azd env unset VARIABLE_NAME
```

### Sensitive Values (Passwords & Secrets)

**Never commit secrets to Git.** Instead:

#### Option 1: Azure Key Vault (Recommended)
Store secrets in Azure Key Vault and reference them in your Bicep template:

```bicep
@secure()
param sqlServerAdminPassword string = newGuid().toString()

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  // ... your KeyVault definition
}

// In your main template, reference the secret:
// resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
//   properties: {
//     administratorLogin: sqlServerAdminUsername
//     administratorLoginPassword: sqlServerAdminPassword
//   }
// }
```

#### Option 2: Set via AZD CLI
```bash
azd env set AZURE_SQL_ADMIN_PASSWORD "YourSecurePassword123!"
```

The password is stored in:
- `.azd/environments/.env.dev` (dev only - safe for local development)
- `.azd/environments/.env.prod` (should remain empty for production)

#### Option 3: GitHub Secrets (for CI/CD)
Store secrets in GitHub repository secrets (Organization Settings → Secrets → Actions) and reference them in your GitHub Actions workflows.

## Environment-Specific Configuration

### Development Environment (dev)

**Characteristics:**
- Minimal resources (B1s VM, Free SQL tier)
- Development-friendly CORS origins (localhost:3000, localhost:3001)
- 30-day log retention
- Suitable for local development and testing

**Configuration file:** `infra/main.dev.bicepparam`

**Default behavior:**
- Uses cost-optimized SKUs
- Enables Application Insights for development insights
- Local debugging enabled

### Production Environment (prod)

**Characteristics:**
- Production-grade resources (Standard SKU, Standard SQL tier)
- Restricted CORS origins (production domain only)
- 90-day log retention and extended backups (35 days)
- Enhanced security and compliance

**Configuration file:** `infra/main.prod.bicepparam`

**Default behavior:**
- Uses production-optimized SKUs
- Enforces managed identity and encryption
- Extended monitoring and diagnostics

### Switching Environments

```bash
# View available environments
azd env list

# Switch to development (default)
azd env select dev

# Switch to production
azd env select prod

# Verify current environment
azd env get-values | grep AZURE_ENV_NAME
```

## Provisioning & Deployment

### Provision Development Environment

```bash
# 1. Select dev environment
azd env select dev

# 2. Configure Azure Entra ID values (one-time setup)
azd env set AZURE_ENTRA_TENANT_ID <your-tenant-id>
azd env set AZURE_ENTRA_CLIENT_ID <your-dev-app-id>
azd env set AZURE_ENTRA_AUDIENCE api://edgefront-api-dev

# 3. Provision infrastructure
azd provision

# 4. Deploy application
azd deploy
```

### Provision Production Environment

**⚠️ WARNING: Proceed with caution. Production deployments require approval and change control.**

```bash
# 1. Select prod environment
azd env select prod

# 2. Configure production values (VERIFY ALL SETTINGS BEFORE RUNNING)
azd env set AZURE_ENTRA_TENANT_ID <your-production-tenant-id>
azd env set AZURE_ENTRA_CLIENT_ID <your-production-app-id>
azd env set AZURE_ENTRA_AUDIENCE <your-production-audience-uri>

# 3. Set SQL Server admin password from Key Vault or secure source
azd env set AZURE_SQL_ADMIN_PASSWORD <your-secure-password>

# 4. Review all environment settings before proceeding
azd env get-values

# 5. Provision infrastructure
azd provision

# 6. Deploy application
azd deploy
```

### View Deployment Status

```bash
# Check resource group contents
az group show --name <resource-group-name>

# View deployed resources
az resource list --resource-group <resource-group-name> -o table

# Monitor Application Insights
az monitor app-insights component show \
  --resource-group <resource-group-name> \
  --name <app-insights-name>
```

## Troubleshooting

### Issue: Wrong Environment Selected

```bash
# Check current environment
azd env get-values | grep AZURE_ENV_NAME

# Switch to correct environment
azd env select dev  # or prod
```

### Issue: Environment Variables Not Loading

```bash
# Reload environment variables
azd env get-values

# Check specific variable
azd env get-values | grep VARIABLE_NAME
```

### Issue: Authentication Failure

```bash
# Verify Azure login
az account show

# Re-authenticate
az login

# Select correct subscription
az account set --subscription <subscription-id>
```

### Issue: Bicep Parameter Mismatch

```bash
# Verify correct parameter file is being used
azd env get-values | grep bicepParam

# Check parameter file references in azure.yaml
cat azure.yaml
```

## Best Practices

1. **Always verify environment before deployment:**
   ```bash
   azd env get-values
   ```

2. **Never commit .env files with secrets:**
   ```bash
   # .env files are already in .gitignore
   cat .gitignore | grep ".env"
   ```

3. **Use Azure Key Vault for production secrets:**
   - Store sensitive values in Key Vault
   - Reference them in Bicep templates with `@secure()` parameters
   - Load them dynamically during provisioning

4. **Tag production environments:**
   - Use clear naming conventions (rg-ef-prod-*)
   - Add descriptive tags in Bicep parameters
   - Document all manual configurations

5. **Maintain separate subscriptions:**
   - Use different Azure subscriptions for dev and prod
   - Prevents accidental cross-environment resource access

6. **Implement change control for production:**
   - Require peer review before prod provisioning
   - Document all configuration changes
   - Test in dev environment first

7. **Monitor and validate deployments:**
   - Use Application Insights for production
   - Set up alerts for critical metrics
   - Regularly test rollback procedures

## Azure Entra ID Configuration

### Get Your Tenant ID

```bash
az account show --query tenantId -o tsv
```

### Register Application

1. Go to Azure Portal → Azure Active Directory → App registrations
2. Create new registration: `edgefront-api-dev` (or `edgefront-api-prod`)
3. Note the Client ID and Tenant ID
4. Configure API scopes and expose the API

### Update AZD Environment

```bash
azd env set AZURE_ENTRA_TENANT_ID <tenant-id>
azd env set AZURE_ENTRA_CLIENT_ID <client-id>
azd env set AZURE_ENTRA_AUDIENCE <api-uri>
```

## Related Files

- **Bicep Template:** `infra/main.bicep`
- **Dev Parameters:** `infra/main.dev.bicepparam`
- **Prod Parameters:** `infra/main.prod.bicepparam`
- **AZD Config:** `.azd/config.json`
- **Dev Environment:** `.azd/environments/.env.dev`
- **Prod Environment:** `.azd/environments/.env.prod`

## Additional Resources

- [Azure Developer CLI Documentation](https://learn.microsoft.com/azure/developer/azure-developer-cli/)
- [Bicep Documentation](https://learn.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure App Service Deployment](https://learn.microsoft.com/azure/app-service/deploy-best-practices)
- [Azure SQL Security Best Practices](https://learn.microsoft.com/azure/azure-sql/database/security-best-practices)
