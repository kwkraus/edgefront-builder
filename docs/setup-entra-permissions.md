# Entra ID App Registration — Permission Setup

This document describes the required Entra ID (Azure AD) app registration permissions for EdgeFront Builder and how to configure them.

## Required Delegated Permissions

| Permission | Type | Spec | Purpose |
|---|---|---|---|
| `openid` | Delegated | SPEC-200 | OIDC sign-in |
| `profile` | Delegated | SPEC-200 | User profile claims |
| `email` | Delegated | SPEC-200 | Email claim |
| `offline_access` | Delegated | SPEC-200 | Refresh token for silent renewal |
| `VirtualEvent.ReadWrite` | Delegated | SPEC-200 | Create, read, update, delete Teams webinars |
| `OnlineMeetingArtifact.Read.All` | Delegated | SPEC-200 | Read attendance reports |
| `User.ReadBasic.All` | Delegated | SPEC-210 | Search Entra directory users for people picker |

## Exposed API Scope

| Scope | Purpose |
|---|---|
| `api://{ClientId}/access_as_user` | Frontend requests this scope; backend validates and uses OBO to exchange for Graph token |

## Option 1: Automated Setup (az CLI)

### Prerequisites
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed
- Admin user (`kwkraus@6gsx0j.onmicrosoft.com`) with Application Admin or Global Admin role

### Run the Script

```powershell
cd tools/

# Replace with your actual Application (client) ID from appsettings.json → AzureAd:ClientId
.\update-app-registration.ps1 -AppId "YOUR_CLIENT_ID"
```

The script will:
1. Log in to Azure CLI with `--allow-no-subscriptions` (required for tenants without an Azure subscription)
2. Show current permissions
3. Add `User.ReadBasic.All` delegated permission
4. Grant admin consent
5. Verify the final permission set

### Troubleshooting

If `az login` fails:
- Try device code flow: `az login --tenant 6gsx0j.onmicrosoft.com --allow-no-subscriptions --use-device-code`
- Or use Option 2 (Graph PowerShell) or Option 3 (portal) below

## Option 2: Microsoft Graph PowerShell SDK

```powershell
Install-Module Microsoft.Graph -Scope CurrentUser
Connect-MgGraph -TenantId "6gsx0j.onmicrosoft.com" -Scopes "Application.ReadWrite.All"

$AppId = "YOUR_CLIENT_ID"
$App = Get-MgApplication -Filter "appId eq '$AppId'"

# See the commented section at the bottom of tools/update-app-registration.ps1
# for the full PowerShell SDK commands
```

## Option 3: Manual Setup (Entra Portal)

1. Go to [Entra ID Portal](https://entra.microsoft.com)
2. Navigate to **Identity** → **Applications** → **App registrations**
3. Select your EdgeFront Builder app
4. Go to **API permissions**
5. Click **Add a permission** → **Microsoft Graph** → **Delegated permissions**
6. Search for and add: `User.ReadBasic.All`
7. Click **Grant admin consent for {tenant}**
8. Verify all permissions show ✅ green checkmarks under "Status"

## Verification

After setup, the API permissions page should show:

| Permission | Type | Status |
|---|---|---|
| `email` | Delegated | ✅ Granted |
| `offline_access` | Delegated | ✅ Granted |
| `OnlineMeetingArtifact.Read.All` | Delegated | ✅ Granted |
| `openid` | Delegated | ✅ Granted |
| `profile` | Delegated | ✅ Granted |
| `User.ReadBasic.All` | Delegated | ✅ Granted |
| `VirtualEvent.ReadWrite` | Delegated | ✅ Granted |
