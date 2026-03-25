# Azure AI Foundry Authentication Update - Summary

## What Was Changed

The Azure AI Foundry registration parsing service has been updated to use **EntraID (DefaultAzureCredential)** authentication instead of API keys. This aligns with your tenant's policy of disabling API key authentication.

### Code Changes

1. **RegistrationParsingService.cs**
   - Added `using Azure.Identity;` 
   - Instantiates `DefaultAzureCredential` in constructor
   - Removed API key validation from configuration checks
   - Token acquisition using `GetTokenAsync()` with scope `https://cognitiveservices.azure.com/.default`
   - Sets `Authorization: Bearer {token}` header instead of `api-key` header

2. **appsettings.json**
   - Removed `ApiKey` from `AzureAI` configuration section
   - Now only requires `Endpoint` and `ProjectName`

3. **Unit Tests**
   - Updated tests to reflect removal of ApiKey requirement
   - All 169 tests pass ✅

---

## What You Need To Do

The error you encountered (`No such host is known (your-ai-foundry-project.cognitiveservices.azure.com)`) means the endpoint is still set to the placeholder value.

### Step 1: Get Your Actual Azure AI Foundry Endpoint

1. Go to [Azure Portal](https://portal.azure.com)
2. Find your **Azure AI Foundry project**
3. Copy the full endpoint URL (e.g., `https://my-project.cognitiveservices.azure.com/`)

### Step 2: Configure for Local Development

Use **user secrets** to keep credentials out of source control:

```powershell
cd src/backend

# Replace with your actual endpoint
dotnet user-secrets set "AzureAI:Endpoint" "https://your-actual-project.cognitiveservices.azure.com/"

# Replace with your actual project/deployment name
dotnet user-secrets set "AzureAI:ProjectName" "your-deployment-name"

# Verify it was set
dotnet user-secrets list
```

### Step 3: Authenticate Locally

Ensure you're logged in to Azure:

```bash
az login
```

This allows `DefaultAzureCredential` to use your Azure credentials automatically.

### Step 4: Test

1. Start the backend: `dotnet run` from `src/backend`
2. Go to the registrations upload page
3. Try uploading a test CSV
4. If you see a different error (API-related) instead of "No such host", authentication is working ✅

---

## For Production

See [`docs/setup-azure-ai-foundry.md`](../docs/setup-azure-ai-foundry.md) for details on:
- Setting up Managed Identity on Azure App Service
- Configuring environment variables for production
- Troubleshooting authentication failures

---

## Files Changed

- ✅ `src/backend/Features/Sessions/RegistrationParsingService.cs` — Updated to use DefaultAzureCredential
- ✅ `src/backend/appsettings.json` — Removed ApiKey placeholder
- ✅ `tests/backend/.../RegistrationParsingServiceUnitTests.cs` — Updated tests
- ✅ `readme.md` — Added reference to Azure AI Foundry setup guide
- ✅ `docs/setup-azure-ai-foundry.md` — New comprehensive setup guide (created)

---

## Next Steps

1. **Immediately**: Configure your endpoint using user secrets (Step 2 above)
2. **Test locally**: Verify registration CSV upload works
3. **For production**: Follow the guide in `docs/setup-azure-ai-foundry.md` for managed identity setup
