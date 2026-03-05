<#
.SYNOPSIS
    Updates the EdgeFront Builder Entra ID app registration with required delegated permissions for SPEC-210.

.DESCRIPTION
    Adds User.ReadBasic.All delegated permission to the app registration and grants admin consent.
    This permission enables the people search feature (searching Entra directory users by displayName/email).

    Requires:
    - Azure CLI installed (az)
    - Admin user with Application Admin or Global Admin role
    - The --allow-no-subscriptions flag is used because this tenant has no Azure subscription

.PARAMETER AppId
    The Application (client) ID of the EdgeFront Builder app registration.

.EXAMPLE
    .\update-app-registration.ps1 -AppId "your-client-id-here"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$AppId,

    [Parameter(Mandatory = $false)]
    [string]$Tenant = "6gsx0j.onmicrosoft.com"
)

$ErrorActionPreference = "Stop"

# Microsoft Graph API ID (well-known)
$GraphApiId = "00000003-0000-0000-c000-000000000000"

# Permission IDs (well-known GUIDs from Microsoft Graph)
$Permissions = @{
    "User.ReadBasic.All" = "a154be20-db9c-4678-8ab7-66f6cc099a59"
}

Write-Host ""
Write-Host "=== EdgeFront Builder — Entra App Registration Update ===" -ForegroundColor Cyan
Write-Host "App ID:  $AppId"
Write-Host "Tenant:  $Tenant"
Write-Host ""

# Step 1: Login to Azure CLI
Write-Host "[1/4] Logging in to Azure CLI (tenant: $Tenant)..." -ForegroundColor Yellow
Write-Host "       Using --allow-no-subscriptions (tenant has no Azure subscription)"
az login --tenant $Tenant --allow-no-subscriptions
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: az login failed. Fallback options:" -ForegroundColor Red
    Write-Host "  1. Try: az login --tenant $Tenant --allow-no-subscriptions --use-device-code"
    Write-Host "  2. Use Microsoft Graph PowerShell SDK (see comments at bottom of this script)"
    Write-Host "  3. Add permissions manually via Entra portal (see docs/setup-entra-permissions.md)"
    exit 1
}
Write-Host "       Login successful." -ForegroundColor Green

# Step 2: Show current permissions
Write-Host ""
Write-Host "[2/4] Current permissions on app registration..." -ForegroundColor Yellow
az ad app permission list --id $AppId --output table
if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: Could not list current permissions. Continuing..." -ForegroundColor DarkYellow
}

# Step 3: Add User.ReadBasic.All delegated permission
Write-Host ""
Write-Host "[3/4] Adding User.ReadBasic.All delegated permission..." -ForegroundColor Yellow
foreach ($perm in $Permissions.GetEnumerator()) {
    Write-Host "       Adding $($perm.Key) (Scope/Delegated)..."
    az ad app permission add `
        --id $AppId `
        --api $GraphApiId `
        --api-permissions "$($perm.Value)=Scope"
    if ($LASTEXITCODE -ne 0) {
        Write-Host "WARNING: Permission may already exist or add failed. Continuing..." -ForegroundColor DarkYellow
    }
    else {
        Write-Host "       $($perm.Key) added successfully." -ForegroundColor Green
    }
}

# Step 4: Grant admin consent
Write-Host ""
Write-Host "[4/4] Granting admin consent..." -ForegroundColor Yellow
az ad app permission admin-consent --id $AppId
if ($LASTEXITCODE -ne 0) {
    Write-Host "WARNING: Admin consent grant may have partially failed." -ForegroundColor DarkYellow
    Write-Host "         You may need to consent manually in the Entra portal."
}
else {
    Write-Host "       Admin consent granted successfully." -ForegroundColor Green
}

# Verify
Write-Host ""
Write-Host "=== Verification ===" -ForegroundColor Cyan
Write-Host "Updated permissions:"
az ad app permission list --id $AppId --output table

Write-Host ""
Write-Host "Done! The following permissions should now be configured:" -ForegroundColor Green
Write-Host "  - VirtualEvent.ReadWrite (delegated) — existing"
Write-Host "  - OnlineMeetingArtifact.Read.All (delegated) — existing"
Write-Host "  - User.ReadBasic.All (delegated) — NEW (SPEC-210)"
Write-Host ""

<#
=== FALLBACK: Microsoft Graph PowerShell SDK ===

If az CLI doesn't work, use these commands instead:

Install-Module Microsoft.Graph -Scope CurrentUser
Connect-MgGraph -TenantId "6gsx0j.onmicrosoft.com" -Scopes "Application.ReadWrite.All"

$AppId = "your-client-id-here"
$GraphApiId = "00000003-0000-0000-c000-000000000000"
$UserReadBasicAll = "a154be20-db9c-4678-8ab7-66f6cc099a59"

# Get the application
$App = Get-MgApplication -Filter "appId eq '$AppId'"

# Get current required resource access
$CurrentAccess = $App.RequiredResourceAccess

# Find or create the Microsoft Graph entry
$GraphAccess = $CurrentAccess | Where-Object { $_.ResourceAppId -eq $GraphApiId }
if (-not $GraphAccess) {
    $GraphAccess = @{
        ResourceAppId = $GraphApiId
        ResourceAccess = @()
    }
    $CurrentAccess += $GraphAccess
}

# Add User.ReadBasic.All if not already present
$Existing = $GraphAccess.ResourceAccess | Where-Object { $_.Id -eq $UserReadBasicAll }
if (-not $Existing) {
    $GraphAccess.ResourceAccess += @{
        Id = $UserReadBasicAll
        Type = "Scope"
    }
}

# Update the application
Update-MgApplication -ApplicationId $App.Id -RequiredResourceAccess $CurrentAccess

Write-Host "Permission added. Grant admin consent in Entra portal or use:"
Write-Host "  New-MgOauth2PermissionGrant ..."
#>
