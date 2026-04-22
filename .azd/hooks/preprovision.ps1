<#
.SYNOPSIS
    AZD preprovision hook - runs before `azd provision`
    
.DESCRIPTION
    Validates prerequisites, ensures resource group exists, and displays deployment configuration.
    Idempotent - safe to run multiple times. Non-blocking warnings only.
#>

param(
    [string]$EnvironmentName = $env:AZURE_ENV_NAME,
    [switch]$Verbose
)

$ErrorActionPreference = "Continue"
$WarningPreference = "Continue"

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "=" * 60
    Write-Host $Message
    Write-Host "=" * 60
}

function Test-AzureCliInstalled {
    try {
        $version = az version --output json 2>$null | ConvertFrom-Json
        $azVersion = $version.'azure-cli'
        Write-Host "✓ Azure CLI is installed (v$azVersion)"
        return $true
    }
    catch {
        Write-Warning "⚠ Azure CLI not found or not in PATH. Please install: https://learn.microsoft.com/en-us/cli/azure/install-azure-cli"
        return $false
    }
}

function Test-AzureAuthentication {
    try {
        $account = az account show --output json 2>$null | ConvertFrom-Json
        Write-Host "✓ Authenticated as: $($account.user.name)"
        Write-Host "  Subscription: $($account.name) ($($account.id))"
        return $true
    }
    catch {
        Write-Warning "⚠ Not authenticated to Azure. Run 'az login' to authenticate."
        return $false
    }
}

function Get-ResourceGroup {
    param([string]$ResourceGroupName)
    
    try {
        $rg = az group show --name $ResourceGroupName --output json 2>$null | ConvertFrom-Json
        return $rg
    }
    catch {
        return $null
    }
}

function Ensure-ResourceGroupExists {
    param([string]$ResourceGroupName, [string]$Location = "eastus")
    
    $rg = Get-ResourceGroup -ResourceGroupName $ResourceGroupName
    
    if ($rg) {
        Write-Host "✓ Resource group exists: $ResourceGroupName (Region: $($rg.location))"
        return $true
    }
    else {
        Write-Host "ℹ Creating resource group: $ResourceGroupName in $Location..."
        try {
            $newRg = az group create --name $ResourceGroupName --location $Location --output json 2>&1 | ConvertFrom-Json
            Write-Host "✓ Resource group created: $ResourceGroupName"
            return $true
        }
        catch {
            Write-Warning "⚠ Failed to create resource group. You may need to create it manually or check permissions."
            return $false
        }
    }
}

function Get-DeploymentConfig {
    if (Test-Path "infra/main.bicepparam") {
        Write-Host "✓ Infrastructure definition: infra/main.bicep"
        Write-Host "  Parameters file: infra/main.bicepparam"
        
        if ($EnvironmentName -eq "prod" -and (Test-Path "infra/main.prod.bicepparam")) {
            Write-Host "  Production overrides: infra/main.prod.bicepparam"
        }
        elseif ($EnvironmentName -eq "dev" -and (Test-Path "infra/main.dev.bicepparam")) {
            Write-Host "  Dev overrides: infra/main.dev.bicepparam"
        }
        return $true
    }
    else {
        Write-Warning "⚠ Bicep parameters file not found at infra/main.bicepparam"
        return $false
    }
}

function Show-PredeploymentSummary {
    param(
        [string]$ResourceGroupName,
        [string]$Location,
        [string]$EnvironmentName
    )
    
    Write-Header "Deployment Configuration Summary"
    Write-Host "Environment: $EnvironmentName"
    Write-Host "Resource Group: $ResourceGroupName"
    Write-Host "Region: $Location"
    Write-Host "Azure CLI: Ready"
    Write-Host ""
}

# Main execution
Write-Header "AZD Preprovision Hook"

# Get resource group name from environment or use default
if (-not $EnvironmentName) {
    $EnvironmentName = "dev"
}

# Derive resource group name (convention: edgefront-builder-{environment})
$ResourceGroupName = "edgefront-builder-$EnvironmentName"
$Location = "eastus"

Write-Host "Validating prerequisites for deployment..."
Write-Host ""

# Validation steps
$cliReady = Test-AzureCliInstalled
$authReady = if ($cliReady) { Test-AzureAuthentication } else { $false }
$configReady = Get-DeploymentConfig

Write-Host ""

if ($authReady) {
    $rgReady = Ensure-ResourceGroupExists -ResourceGroupName $ResourceGroupName -Location $Location
    Show-PredeploymentSummary -ResourceGroupName $ResourceGroupName -Location $Location -EnvironmentName $EnvironmentName
}
else {
    Write-Warning "⚠ Cannot verify resource group due to authentication issues."
}

if (-not $authReady) {
    Write-Host ""
    Write-Warning "⚠ Prerequisites check incomplete. Please authenticate and retry."
}
else {
    Write-Host "✓ Preprovision validation complete. Ready to provision."
    Write-Host ""
}

exit 0
