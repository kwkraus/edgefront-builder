# EdgeFront Builder - Deployment Quick Start Guide

**⏱️ Estimated Total Time**: 20-30 minutes  
**🎯 Goal**: Deploy infrastructure and application to Azure  
**📋 Prerequisites**: Azure subscription, AZD installed, Azure CLI authenticated

---

## ✅ Pre-Deployment Checklist (5 minutes)

- [ ] Azure subscription is active
- [ ] Azure CLI installed: `az --version` (2.50+)
- [ ] AZD installed: `azd --version` (0.10+)
- [ ] Logged into Azure: `az login`
- [ ] Have Azure Entra app registration details ready:
  - [ ] Tenant ID
  - [ ] Client ID
  - [ ] API Audience URI

---

## 🚀 Quick Deployment Steps

### Step 1: Configure Environment (2 minutes)

```powershell
# Select development environment (default)
azd env select dev

# Set Azure Entra configuration
azd env set AZURE_ENTRA_TENANT_ID "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
azd env set AZURE_ENTRA_CLIENT_ID "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy"
azd env set AZURE_ENTRA_AUDIENCE "api://edgefront-api-dev"

# Set SQL admin password securely
azd env set AZURE_SQL_ADMIN_PASSWORD "YourSecurePassword123!"

# Verify configuration
azd env get-values
```

### Step 2: Provision Infrastructure (10-15 minutes)

```powershell
# Provision all Azure resources
azd provision

# What happens:
# • Creates resource group (rg-edgefront-dev)
# • Deploys Bicep template (14 resources)
# • Configures managed identity and security
# • Sets up monitoring and logging
# Expected: Successful completion with ✅ status
```

**⏳ Wait for provisioning to complete. You'll see:**
```
✓ Preprovision validation complete
✓ Resource group created/verified
✓ Bicep deployment started
[5-15 minute wait]
✓ All resources provisioned successfully
✓ Postprovision hook completed
  - Frontend URL: https://xxx.azurewebsites.net
  - Backend URL: https://yyy.azurewebsites.net
  - SQL Server: xxx.database.windows.net
```

### Step 3: Deploy Application Code (5-10 minutes)

```powershell
# Build and deploy frontend and backend
azd deploy

# What happens:
# • Builds Next.js 16 frontend
# • Builds ASP.NET Core 10 backend
# • Deploys frontend to Static Web App
# • Deploys backend to App Service
# Expected: Successful completion with ✅ status
```

**✅ Success indicators:**
```
✓ Frontend deployed to Static Web App
✓ Backend deployed to App Service
✓ Application Insights receiving metrics
✓ Health check passed
✓ All endpoints responding
```

---

## 🎉 Deployment Complete!

### Verify Your Deployment

```powershell
# Get all resource URLs and configuration
azd env get-values

# Test frontend
curl https://<frontend-url>

# Test backend health
curl https://<backend-url>/health

# View logs
az webapp log tail -g rg-edgefront-dev -n <app-service-name>
```

### Key Resource URLs

After deployment, you'll have:

| Resource | Type | Example URL |
|----------|------|-------------|
| **Frontend** | Static Web App | `https://xxx.azurewebsites.net` |
| **Backend API** | App Service | `https://yyy.azurewebsites.net` |
| **Health Check** | App Service | `https://yyy.azurewebsites.net/health` |
| **Database** | SQL Server | `xxx.database.windows.net` |
| **Monitoring** | Application Insights | Azure Portal |
| **Logs** | Log Analytics | Azure Portal |

### Next Steps

1. **Configure Frontend**: Update `.env.local` with backend API URL
2. **Run Tests**: Execute integration tests
3. **Monitor**: Check Application Insights dashboard
4. **Backup**: Verify SQL Database backups are enabled
5. **Security**: Validate managed identity permissions

---

## 🆘 Quick Troubleshooting

### "Not authenticated to Azure"
```powershell
az login
```

### "Insufficient permissions"
```powershell
# Verify you have Contributor role on subscription
az role assignment list --assignee <your-email>
```

### "Entra configuration error"
```powershell
# Verify app registration and values
az ad app list --display-name "EdgeFront Builder API"
azd env set AZURE_ENTRA_TENANT_ID "<correct-value>"
```

### "SQL password doesn't meet requirements"
```powershell
# Password must be: 8-128 chars, uppercase, lowercase, digit, special char
# Examples: MyP@ssw0rd123! or Secure$Pass#2026
azd env set AZURE_SQL_ADMIN_PASSWORD "YourSecurePassword123!"
```

### "Backend not responding after deployment"
```powershell
# Backend may still be starting - wait 2-3 minutes
# Check deployment status
az deployment group list -g rg-edgefront-dev --query "[?properties.provisioningState!='Succeeded']"

# View logs
az webapp log tail -g rg-edgefront-dev -n <app-service-name>
```

### "Static Web App deployment incomplete"
```powershell
# SWA may still be initializing - wait 5-10 minutes
az staticwebapp list -g rg-edgefront-dev
```

---

## 📋 Production Deployment

For production, use the same steps but select `prod` environment:

```powershell
# Select production
azd env select prod

# Set production values (MUST be set before provisioning)
azd env set AZURE_ENTRA_TENANT_ID "prod-tenant-id"
azd env set AZURE_ENTRA_CLIENT_ID "prod-client-id"
azd env set AZURE_SQL_ADMIN_PASSWORD "prod-secure-password"

# Provision production infrastructure
azd provision

# Deploy to production
azd deploy
```

**⚠️ Production Notes:**
- Ensure you have change control approval
- Use Azure Key Vault for secrets (not environment variables)
- Verify CORS is set to production domain only
- Enable advanced monitoring and alerts
- Test rollback procedure before production deployment

---

## 📚 Full Documentation

For complete details, configuration options, and troubleshooting:

👉 **See**: `infra/DEPLOYMENT-VALIDATION.md`

This document includes:
- ✅ 823-line comprehensive validation report
- ✅ All 14 Azure resources documented
- ✅ Complete troubleshooting guide
- ✅ Pre/post-deployment procedures
- ✅ Cost estimates
- ✅ Security best practices

---

## 💡 Tips

- **Re-run provisioning**: If infrastructure needs updating, just run `azd provision` again
- **View resources**: `https://portal.azure.com/#resource/subscriptions/<sub-id>/resourceGroups/rg-edgefront-dev`
- **Delete all resources**: `az group delete -n rg-edgefront-dev --yes` (careful!)
- **Scale up**: Change `appServicePlanSku` in `.bicepparam` and re-provision
- **Backup database**: Already configured automatically (7 days dev, 35 days prod)

---

## ⏱️ Deployment Timeline

```
├─ Step 1: Configuration           (2 min)
│  └─ Set environment variables
├─ Step 2: Provision Infrastructure (10-15 min)
│  └─ Create 14 Azure resources
├─ Step 3: Deploy Application       (5-10 min)
│  └─ Build & deploy frontend/backend
└─ Total Time                        ~20-30 minutes

After deployment is complete:
├─ Verify endpoints
├─ Check Application Insights
├─ Monitor logs
└─ Configure monitoring alerts
```

---

## 🔒 Security Reminders

- ✅ **Never commit passwords** to version control
- ✅ **Use Azure Key Vault** for production secrets
- ✅ **Managed Identity** is enabled for secure SQL access
- ✅ **HTTPS only** on all endpoints (enforced)
- ✅ **TLS 1.2 minimum** (enforced)
- ✅ **Encryption at rest** (enabled)

---

**Questions?** See `infra/DEPLOYMENT-VALIDATION.md` for detailed troubleshooting.

**Ready to deploy?** Run: `azd env select dev && azd provision && azd deploy`
