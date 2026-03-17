# Entra ID App Registration — Local-Only Auth Setup

> EdgeFront Builder currently ships as a local-first product. Microsoft Graph and Teams scopes below are archived for legacy code paths that are still present in the repository but are not part of the active API surface.

This document describes the Entra ID (Azure AD) app registration configuration for EdgeFront Builder and identifies which permissions are required for the active local-only experience versus retained only for legacy integration code.

## Active Delegated Permissions

| Permission | Type | Spec | Purpose |
|---|---|---|---|
| `openid` | Delegated | Current auth | OIDC sign-in |
| `profile` | Delegated | Current auth | User profile claims |
| `email` | Delegated | Current auth | Email claim |
| `offline_access` | Delegated | Current auth | Refresh token for silent renewal |

## Exposed API Scope

| Scope | Purpose |
|---|---|
| `api://{ClientId}/access_as_user` | Frontend requests this scope and the backend validates it for authenticated API access |

## Archived Legacy Scopes

These scopes are only needed if the dormant Teams/Graph integration code is intentionally reactivated:

| Permission | Type | Legacy Purpose |
|---|---|---|
| `VirtualEvent.ReadWrite` | Delegated | Create, read, update, delete Teams webinars |
| `OnlineMeetingArtifact.Read.All` | Delegated | Read attendance reports |
| `User.ReadBasic.All` | Delegated | Search Entra directory users for people picker |

## Option 1: Automated Setup (az CLI)

### Prerequisites
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) installed
- An admin user with Application Admin or Global Admin role in your tenant

### Run the Script

```powershell
cd tools/

# Replace with your actual Application (client) ID from appsettings.json → AzureAd:ClientId
.\update-app-registration.ps1 -AppId "YOUR_CLIENT_ID"
```

The script will:
1. Log in to Azure CLI with `--allow-no-subscriptions` (required for tenants without an Azure subscription)
2. Show current permissions
3. Add the archived legacy delegated permissions used by the dormant Graph integration code
4. Grant admin consent
5. Verify the final permission set

### Troubleshooting

If `az login` fails:
- Try device code flow: `az login --tenant YOUR_TENANT_ID --allow-no-subscriptions --use-device-code`
- Or use Option 2 (Graph PowerShell) or Option 3 (portal) below

## Option 2: Microsoft Graph PowerShell SDK

```powershell
Install-Module Microsoft.Graph -Scope CurrentUser
Connect-MgGraph -TenantId "YOUR_TENANT_ID" -Scopes "Application.ReadWrite.All"

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

At minimum, the API permissions page should show the active local-only permissions:

| Permission | Type | Status |
|---|---|---|
| `email` | Delegated | ✅ Granted |
| `openid` | Delegated | ✅ Granted |
| `offline_access` | Delegated | ✅ Granted |
| `profile` | Delegated | ✅ Granted |

If you are intentionally validating legacy Graph code paths, also grant:

| Permission | Type | Status |
|---|---|---|
| `OnlineMeetingArtifact.Read.All` | Delegated | Optional / legacy |
| `User.ReadBasic.All` | Delegated | Optional / legacy |
| `VirtualEvent.ReadWrite` | Delegated | Optional / legacy |
