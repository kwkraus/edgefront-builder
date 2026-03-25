# Azure AI Foundry Configuration

This document describes how to configure Azure AI Foundry for registration CSV parsing in EdgeFront Builder.

## Overview

EdgeFront Builder uses **Azure AI Foundry Projects** with **EntraID (DefaultAzureCredential)** authentication to parse registration CSV files. The service extracts registrant information (email, first name, last name, registration datetime) from uploaded CSV data.

> **Note**: API key authentication has been disabled by tenant policy. All authentication must use EntraID with `DefaultAzureCredential`.

---

## Required Configuration

The backend requires the following Azure AI Foundry configuration values:

| Config Key | Description | Required | Example |
|---|---|---|---|
| `AzureAI:Endpoint` | Azure AI Foundry project endpoint URL | ✓ | `https://your-project.openai.azure.com/` |
| `AzureAI:ProjectName` | Azure AI Foundry project name (used as model deployment) | ✓ | `gpt-4` or `your-project` |
| `AzureAI:TenantId` | Azure tenant ID where the AI resource lives | Optional | `16b3c013-d300-468d-ac64-7eda0820b6d3` |

Set `AzureAI:TenantId` only if your Azure AI Foundry resource is in a different tenant than your user's home tenant.

---

## Setup Steps

### 1. Get Your Azure AI Foundry Endpoint

The endpoint format depends on your Azure AI Foundry resource type:

#### Option A: Azure OpenAI (Recommended)
If using Azure OpenAI, the endpoint is your resource URL:
- Format: `https://{resource-name}.openai.azure.com`
- Example: `https://my-openai-resource.openai.azure.com`

#### Option B: Azure AI Services (Project)
If using Azure AI Services with a project:
- Format: `https://{resource-name}.services.ai.azure.com/api/projects/{project-name}`
- Example: `https://edgefront-project-resource.services.ai.azure.com/api/projects/edgefront-project`

**To find your endpoint:**
1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your **Azure OpenAI resource** or **Azure AI Services resource**
3. On the overview page, find the **Endpoint** URL
4. Copy the full endpoint (do NOT include `/chat/completions` or API version - the service adds those automatically)

### 2. Verify Your Project Name

1. In the Azure AI Foundry project, locate your **model deployment name**
2. This is typically the name of your deployed model (e.g., `gpt-4`, `gpt-35-turbo`)
3. If you're unsure, check the **Deployments** section of your Azure AI project

### 3. Configure for Local Development

Edit `src/backend/appsettings.json`:

```json
{
  "AzureAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ProjectName": "your-deployment-name"
  }
}
```

Or for Azure AI Services with projects:

```json
{
  "AzureAI": {
    "Endpoint": "https://your-resource.services.ai.azure.com/api/projects/your-project-name",
    "ProjectName": "your-deployment-name"
  }
}
```

> **⚠️ Important**: Do NOT commit this file with real values. Use user secrets instead (see below).

### 4. Configure Using User Secrets (Recommended for Local Dev)

User secrets keep sensitive values out of source control:

```powershell
cd src/backend

# Set the endpoint (Azure OpenAI format)
dotnet user-secrets set "AzureAI:Endpoint" "https://your-resource.openai.azure.com"

# Or for Azure AI Services with projects
# dotnet user-secrets set "AzureAI:Endpoint" "https://your-resource.services.ai.azure.com/api/projects/your-project"

# Set the deployment name
dotnet user-secrets set "AzureAI:ProjectName" "your-deployment-name"

# ONLY if the resource is in a different tenant: set the resource tenant ID
# dotnet user-secrets set "AzureAI:TenantId" "16b3c013-d300-468d-ac64-7eda0820b6d3"

# Verify secrets were set
dotnet user-secrets list
```

### Cross-Tenant Resource Access

If your Azure AI Foundry resource is in a **different Azure tenant** than your user's home tenant:

**How it works**: The application uses a credential chain that tries both your home tenant and the resource tenant. You can configure this by setting `AzureAI:TenantId`.

**To enable cross-tenant access:**

```powershell
# Set the tenant ID where the AI Foundry resource lives
dotnet user-secrets set "AzureAI:TenantId" "16b3c013-d300-468d-ac64-7eda0820b6d3"
```

**Your authentication options:**

1. **Option 1: Be a guest user in the resource tenant (Recommended)**
   - Ask your Azure administrator to add your account as a guest in the resource tenant
   - Accept the guest invitation
   - Run `az login` (credentials now work for both tenants)
   - The credential chain will successfully acquire tokens in the resource tenant

2. **Option 2: Explicitly log in to the resource tenant**
   ```bash
   az login --tenant 16b3c013-d300-468d-ac64-7eda0820b6d3
   ```
   - This authenticates you to the resource tenant directly
   - The credential chain will use these credentials

With either approach and `AzureAI:TenantId` configured, the application will automatically acquire valid tokens for the cross-tenant resource.

### 5. Configure for Production

For production deployments, set environment variables:

#### Azure App Service

1. Go to your App Service in Azure Portal
2. Navigate to **Configuration** → **Application settings**
3. Add new settings:
   - Name: `AzureAI__Endpoint`
   - Value: `https://your-resource.openai.azure.com` (or your Azure AI Services endpoint)
   
   - Name: `AzureAI__ProjectName`
   - Value: `your-deployment-name`

4. Click **Save**

> **Note**: Use `__` (double underscore) for nested configuration keys in environment variables.

#### Docker / Container Deployment

Set environment variables when running the container:

```bash
docker run \
  -e "AzureAI__Endpoint=https://your-actual-project.cognitiveservices.azure.com/" \
  -e "AzureAI__ProjectName=your-deployment-name" \
  your-image
```

---

## Authentication: DefaultAzureCredential with Multi-Tenant Support

The service uses a **credential chain** that automatically tries multiple authentication sources:

1. **DefaultAzureCredential** — Tries credentials in order: environment variables, workload identity, managed identity, Azure CLI, Visual Studio, VS Code
2. **Resource-Tenant DefaultAzureCredential** (if `AzureAI:TenantId` is configured) — Tries the same sources but scoped to the resource tenant

This dual-credential approach supports both same-tenant and cross-tenant resource access. The credential chain stops at the first successful authentication.

### For Local Development

Run `az login` to authenticate with your Azure account:

```bash
az login
```

This sets up local credentials that `DefaultAzureCredential` will use automatically.

### For Azure App Service (Managed Identity)

1. Enable **System-assigned managed identity** on your App Service
2. Grant the managed identity access to the Azure AI Foundry resource:
   - Go to your Azure AI Foundry resource
   - **Access Control (IAM)** → **Add role assignment**
   - Role: `Cognitive Services User`
   - Assign to: Your App Service's managed identity

DefaultAzureCredential will automatically use the managed identity when running on App Service.

---

## Verification

### Test Configuration Locally

```powershell
cd src/backend
dotnet run
```

Then navigate to the registrations upload page in the frontend. If configuration is correct, the upload should attempt to call the Azure AI Foundry API. You should see either:
- Successful parsing (if CSV is provided)
- HTTP 403/401 error (if authentication failed)
- API error message (if endpoint is wrong)

### Troubleshooting

| Error | Likely Cause | Solution |
|---|---|---|
| `No such host is known (your-ai-foundry-project.cognitiveservices.azure.com)` | Endpoint is still a placeholder | Replace with actual endpoint in appsettings.json or user secrets |
| `BadRequest: Token tenant {id} does not match resource tenant` | Cross-tenant mismatch | Set `AzureAI:TenantId` and be a guest in the resource tenant, OR use `az login --tenant <resource-tenant-id>`. The credential chain will then try both tenants |
| `CredentialUnavailableException: Please run 'az login'` | No valid credentials available | Run `az login`. If using cross-tenant resource, also set `AzureAI:TenantId` and optionally use `az login --tenant <resource-tenant-id>` |
| `Unauthorized (401)` | Token scope or access issue | Verify you have access to the resource (be a member or guest of the resource tenant). Check `AzureAI:TenantId` if resource is in a different tenant |
| `UnauthorizedException` or `AuthenticationFailedException` | DefaultAzureCredential failed to acquire token | Run `az login` or set up managed identity + role assignment |
| `ResourceNotFound` (404) | Endpoint or project name is wrong | Verify endpoint URL and project name in Azure Portal, ensure ProjectName matches deployed model name |
| `AuthenticationFailedException: Azure PowerShell authentication timed out` | PowerShell credential in chain is slow | This is already excluded in the code. If you see this, clear Azure credential cache or restart the application |

---

## API Scope

The service requests the following Azure scope for token acquisition:

```
https://ai.azure.com/.default
```

This scope grants access to Azure AI Foundry Projects API (and is compatible with both Azure OpenAI and Azure AI Services endpoints). The scope is automatically applied by `DefaultAzureCredential` and provides the necessary permissions for registration CSV parsing.

---

## Related Code

- **Service**: `src/backend/Features/Sessions/RegistrationParsingService.cs`
- **Configuration**: `src/backend/appsettings.json`
- **Tests**: `tests/backend/EdgeFront.Builder.Tests/Features/Sessions/RegistrationParsingServiceUnitTests.cs`
