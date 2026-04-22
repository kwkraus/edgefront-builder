<#
.SYNOPSIS
    AZD postprovision hook - runs after `azd provision`
    
.DESCRIPTION
    Captures deployment outputs, displays resource URLs, managed identity status,
    and provides next steps guidance.
    Idempotent - safe to run multiple times.
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

function Get-AzureContext {
    try {
        $account = az account show --output json 2>$null | ConvertFrom-Json
        return @{
            SubscriptionId = $account.id
            SubscriptionName = $account.name
            TenantId = $account.tenantId
            User = $account.user.name
        }
    }
    catch {
        return $null
    }
}

function Get-DeploymentOutputs {
    param([string]$ResourceGroupName)
    
    try {
        # Get the last successful deployment
        $deployments = az deployment group list --resource-group $ResourceGroupName --query "[?properties.provisioningState=='Succeeded']" --output json 2>$null | ConvertFrom-Json
        
        if ($deployments -and $deployments.Count -gt 0) {
            # Sort by timestamp and get the most recent
            $latest = $deployments | Sort-Object -Property { $_.properties.timestamp } -Descending | Select-Object -First 1
            
            if ($latest.properties.outputs) {
                return $latest.properties.outputs
            }
        }
        return $null
    }
    catch {
        return $null
    }
}

function Get-ResourceUrls {
    param(
        [string]$ResourceGroupName,
        [object]$Outputs
    )
    
    $urls = @{}
    
    try {
        # Try to get frontend URL from Static Web App
        $swa = az staticwebapp list --resource-group $ResourceGroupName --output json 2>$null | ConvertFrom-Json
        if ($swa) {
            $urls.Frontend = $swa[0].defaultHostname
        }
        
        # Try to get backend API from App Service
        $appService = az appservice list --resource-group $ResourceGroupName --output json 2>$null | ConvertFrom-Json
        if ($appService) {
            $urls.Backend = $appService[0].defaultHostName
        }
        
        # Check for SQL Database
        $sqlServer = az sql server list --resource-group $ResourceGroupName --output json 2>$null | ConvertFrom-Json
        if ($sqlServer) {
            $urls.Database = $sqlServer[0].fullyQualifiedDomainName
        }
    }
    catch {
        # Silently continue if resources not found yet
    }
    
    return $urls
}

function Get-ManagedIdentities {
    param([string]$ResourceGroupName)
    
    try {
        $identities = az identity list --resource-group $ResourceGroupName --output json 2>$null | ConvertFrom-Json
        return $identities
    }
    catch {
        return @()
    }
}

function Get-KeyVaultSecrets {
    param([string]$ResourceGroupName)
    
    try {
        $vaults = az keyvault list --resource-group $ResourceGroupName --output json 2>$null | ConvertFrom-Json
        return $vaults
    }
    catch {
        return @()
    }
}

function Show-DeploymentSummary {
    param(
        [string]$ResourceGroupName,
        [string]$EnvironmentName,
        [object]$AzureContext,
        [object]$Urls,
        [object]$Identities,
        [object]$KeyVaults
    )
    
    Write-Header "Deployment Completed Successfully"
    
    Write-Host "Environment: $EnvironmentName"
    Write-Host "Resource Group: $ResourceGroupName"
    Write-Host "Subscription: $($AzureContext.SubscriptionName)"
    Write-Host "Authenticated as: $($AzureContext.User)"
    Write-Host ""
    
    Write-Host "Deployed Resources:"
    Write-Host "-------------------"
    
    if ($Urls.Frontend) {
        Write-Host "✓ Frontend (Static Web App)"
        Write-Host "  URL: https://$($Urls.Frontend)"
    }
    else {
        Write-Host "⚠ Frontend not found (may still be deploying)"
    }
    
    if ($Urls.Backend) {
        Write-Host "✓ Backend API (App Service)"
        Write-Host "  URL: https://$($Urls.Backend)"
        Write-Host "  Health endpoint: https://$($Urls.Backend)/health"
    }
    else {
        Write-Host "⚠ Backend not found (may still be deploying)"
    }
    
    if ($Urls.Database) {
        Write-Host "✓ SQL Database"
        Write-Host "  Server: $($Urls.Database)"
    }
    else {
        Write-Host "⚠ Database not found"
    }
    
    if ($Identities -and $Identities.Count -gt 0) {
        Write-Host ""
        Write-Host "Managed Identities:"
        Write-Host "------------------"
        foreach ($identity in $Identities) {
            Write-Host "✓ $($identity.name)"
            Write-Host "  ID: $($identity.principalId)"
        }
    }
    
    if ($KeyVaults -and $KeyVaults.Count -gt 0) {
        Write-Host ""
        Write-Host "Key Vaults:"
        Write-Host "-----------"
        foreach ($vault in $KeyVaults) {
            Write-Host "✓ $($vault.name)"
            Write-Host "  URI: $($vault.properties.vaultUri)"
        }
    }
    
    Write-Host ""
    Write-Host "Next Steps:"
    Write-Host "-----------"
    Write-Host "1. Verify resource creation in Azure Portal:"
    Write-Host "   https://portal.azure.com/#@$($AzureContext.TenantId)/resource/subscriptions/$($AzureContext.SubscriptionId)/resourceGroups/$ResourceGroupName"
    Write-Host ""
    Write-Host "2. Configure environment variables for frontend and backend"
    Write-Host "   Update .env.local files with deployed resource URLs"
    Write-Host ""
    Write-Host "3. Deploy application code:"
    Write-Host "   azd deploy"
    Write-Host ""
    Write-Host "4. Test endpoints:"
    if ($Urls.Backend) {
        Write-Host "   curl https://$($Urls.Backend)/health"
    }
    Write-Host ""
}

# Main execution
Write-Header "AZD Postprovision Hook"

# Get environment
if (-not $EnvironmentName) {
    $EnvironmentName = "dev"
}

$ResourceGroupName = "edgefront-builder-$EnvironmentName"

Write-Host "Gathering deployment information..."
Write-Host ""

$context = Get-AzureContext
if (-not $context) {
    Write-Warning "⚠ Could not retrieve Azure context. Some information may be unavailable."
}
else {
    $outputs = Get-DeploymentOutputs -ResourceGroupName $ResourceGroupName
    $urls = Get-ResourceUrls -ResourceGroupName $ResourceGroupName -Outputs $outputs
    $identities = Get-ManagedIdentities -ResourceGroupName $ResourceGroupName
    $vaults = Get-KeyVaultSecrets -ResourceGroupName $ResourceGroupName
    
    Show-DeploymentSummary `
        -ResourceGroupName $ResourceGroupName `
        -EnvironmentName $EnvironmentName `
        -AzureContext $context `
        -Urls $urls `
        -Identities $identities `
        -KeyVaults $vaults
}

Write-Host "✓ Postprovision hook completed successfully."
Write-Host ""

exit 0
