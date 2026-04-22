<#
.SYNOPSIS
    AZD postdeploy hook - runs after `azd deploy`
    
.DESCRIPTION
    Verifies application health by testing frontend and backend endpoints.
    Displays deployment completion status and troubleshooting guidance.
    Idempotent - safe to run multiple times.
#>

param(
    [string]$EnvironmentName = $env:AZURE_ENV_NAME,
    [int]$TimeoutSeconds = 30,
    [int]$RetryAttempts = 3,
    [int]$RetryDelaySeconds = 5,
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
            TenantId = $account.tenantId
        }
    }
    catch {
        return $null
    }
}

function Get-DeployedResourceUrls {
    param([string]$ResourceGroupName)
    
    $urls = @{}
    
    try {
        # Get frontend Static Web App
        $swa = az staticwebapp list --resource-group $ResourceGroupName --output json 2>$null | ConvertFrom-Json
        if ($swa -and $swa.Count -gt 0) {
            $urls.Frontend = "https://$($swa[0].defaultHostname)"
        }
        
        # Get backend App Service
        $appService = az appservice list --resource-group $ResourceGroupName --output json 2>$null | ConvertFrom-Json
        if ($appService -and $appService.Count -gt 0) {
            $urls.Backend = "https://$($appService[0].defaultHostName)"
        }
    }
    catch {
        # Continue if resources not found
    }
    
    return $urls
}

function Test-HealthEndpoint {
    param(
        [string]$Url,
        [string]$EndpointName,
        [int]$TimeoutSeconds = 30,
        [int]$RetryAttempts = 3,
        [int]$RetryDelaySeconds = 5
    )
    
    $attempt = 1
    $maxAttempts = $RetryAttempts
    
    while ($attempt -le $maxAttempts) {
        try {
            Write-Host "  Attempt $attempt/$maxAttempts: Testing $EndpointName..."
            
            $response = Invoke-WebRequest -Uri $Url `
                -UseBasicParsing `
                -TimeoutSec $TimeoutSeconds `
                -ErrorAction Stop
            
            if ($response.StatusCode -eq 200 -or $response.StatusCode -eq 302) {
                Write-Host "  ✓ $EndpointName is responding ($($response.StatusCode))"
                return $true
            }
            else {
                Write-Host "  ⚠ $EndpointName returned status $($response.StatusCode)"
                if ($attempt -lt $maxAttempts) {
                    Write-Host "    Retrying in ${RetryDelaySeconds}s..."
                    Start-Sleep -Seconds $RetryDelaySeconds
                }
            }
        }
        catch {
            Write-Host "  ⚠ $EndpointName not responding (attempt $attempt/$maxAttempts)"
            if ($attempt -lt $maxAttempts) {
                Write-Host "    Retrying in ${RetryDelaySeconds}s..."
                Start-Sleep -Seconds $RetryDelaySeconds
            }
            else {
                Write-Host "  ! $EndpointName failed all retry attempts"
                return $false
            }
        }
        $attempt++
    }
    
    return $false
}

function Test-BackendHealthApi {
    param(
        [string]$BackendUrl,
        [int]$TimeoutSeconds = 30,
        [int]$RetryAttempts = 3,
        [int]$RetryDelaySeconds = 5
    )
    
    $healthUrl = "$BackendUrl/health"
    $attempt = 1
    $maxAttempts = $RetryAttempts
    
    while ($attempt -le $maxAttempts) {
        try {
            Write-Host "  Attempt $attempt/$maxAttempts: Testing backend health API..."
            
            $response = Invoke-WebRequest -Uri $healthUrl `
                -UseBasicParsing `
                -TimeoutSec $TimeoutSeconds `
                -ErrorAction Stop
            
            if ($response.StatusCode -eq 200) {
                Write-Host "  ✓ Backend health API is healthy"
                return $true
            }
            else {
                Write-Host "  ⚠ Backend health API returned status $($response.StatusCode)"
                if ($attempt -lt $maxAttempts) {
                    Write-Host "    Retrying in ${RetryDelaySeconds}s..."
                    Start-Sleep -Seconds $RetryDelaySeconds
                }
            }
        }
        catch {
            Write-Host "  ⚠ Backend health API not responding (attempt $attempt/$maxAttempts)"
            if ($attempt -lt $maxAttempts) {
                Write-Host "    Retrying in ${RetryDelaySeconds}s..."
                Start-Sleep -Seconds $RetryDelaySeconds
            }
            else {
                Write-Host "  ! Backend health API failed all retry attempts"
                if ($Verbose) {
                    Write-Host "  Error: $($_.Exception.Message)"
                }
                return $false
            }
        }
        $attempt++
    }
    
    return $false
}

function Show-DeploymentSummary {
    param(
        [string]$EnvironmentName,
        [object]$Urls,
        [object]$HealthResults,
        [object]$AzureContext
    )
    
    Write-Header "Deployment Verification Complete"
    
    Write-Host "Environment: $EnvironmentName"
    Write-Host ""
    
    Write-Host "Health Check Results:"
    Write-Host "---------------------"
    
    if ($HealthResults.FrontendHealthy) {
        Write-Host "✓ Frontend: Healthy"
        Write-Host "  URL: $($Urls.Frontend)"
    }
    elseif ($Urls.Frontend) {
        Write-Host "⚠ Frontend: Not responding or unhealthy"
        Write-Host "  URL: $($Urls.Frontend)"
        Write-Host "  Status: May still be deploying, check later"
    }
    else {
        Write-Host "⚠ Frontend: Not deployed"
    }
    
    if ($HealthResults.BackendHealthy) {
        Write-Host "✓ Backend API: Healthy"
        Write-Host "  URL: $($Urls.Backend)"
    }
    elseif ($HealthResults.BackendAvailable) {
        Write-Host "⚠ Backend API: Responding but health check failed"
        Write-Host "  URL: $($Urls.Backend)"
        Write-Host "  Status: Check logs in App Service diagnostic tools"
    }
    elseif ($Urls.Backend) {
        Write-Host "⚠ Backend API: Not responding"
        Write-Host "  URL: $($Urls.Backend)"
        Write-Host "  Status: May still be deploying, check deployment center"
    }
    else {
        Write-Host "⚠ Backend API: Not deployed"
    }
    
    Write-Host ""
    Write-Host "Deployment Status:"
    Write-Host "------------------"
    
    if ($HealthResults.FrontendHealthy -and $HealthResults.BackendHealthy) {
        Write-Host "✓ All systems operational!"
    }
    elseif ($HealthResults.FrontendHealthy -or $HealthResults.BackendHealthy) {
        Write-Host "⚠ Partial deployment - some services not yet responding"
    }
    else {
        Write-Host "! Services not yet responding - deployment may still be in progress"
    }
    
    Write-Host ""
    Write-Host "Troubleshooting:"
    Write-Host "---------------"
    Write-Host "• If endpoints are not responding, they may still be starting up"
    Write-Host "• Check deployment status: az deployment group list -g edgefront-builder-$EnvironmentName"
    Write-Host "• View App Service logs: az webapp log tail -g edgefront-builder-$EnvironmentName -n <app-name>"
    Write-Host "• View Static Web App: az staticwebapp show -g edgefront-builder-$EnvironmentName -n <app-name>"
    
    if ($AzureContext) {
        Write-Host ""
        Write-Host "Azure Portal:"
        Write-Host "• Resource Group: https://portal.azure.com/#@$($AzureContext.TenantId)/resource/subscriptions/$($AzureContext.SubscriptionId)/resourceGroups/edgefront-builder-$EnvironmentName"
    }
    
    Write-Host ""
}

# Main execution
Write-Header "AZD Postdeploy Hook"

# Get environment
if (-not $EnvironmentName) {
    $EnvironmentName = "dev"
}

$ResourceGroupName = "edgefront-builder-$EnvironmentName"

Write-Host "Verifying application health..."
Write-Host ""

$context = Get-AzureContext
$urls = Get-DeployedResourceUrls -ResourceGroupName $ResourceGroupName

$healthResults = @{
    FrontendHealthy = $false
    BackendHealthy = $false
    BackendAvailable = $false
}

if ($urls.Frontend) {
    Write-Host "Testing Frontend:"
    $healthResults.FrontendHealthy = Test-HealthEndpoint `
        -Url $urls.Frontend `
        -EndpointName "Frontend" `
        -TimeoutSeconds $TimeoutSeconds `
        -RetryAttempts $RetryAttempts `
        -RetryDelaySeconds $RetryDelaySeconds
}
else {
    Write-Host "Frontend URL not available in resource group"
}

Write-Host ""

if ($urls.Backend) {
    Write-Host "Testing Backend:"
    # First check if backend is available at all
    $healthResults.BackendAvailable = Test-HealthEndpoint `
        -Url $urls.Backend `
        -EndpointName "Backend" `
        -TimeoutSeconds $TimeoutSeconds `
        -RetryAttempts 1 `
        -RetryDelaySeconds $RetryDelaySeconds
    
    # Then test the health endpoint specifically
    if ($healthResults.BackendAvailable) {
        $healthResults.BackendHealthy = Test-BackendHealthApi `
            -BackendUrl $urls.Backend `
            -TimeoutSeconds $TimeoutSeconds `
            -RetryAttempts $RetryAttempts `
            -RetryDelaySeconds $RetryDelaySeconds
    }
}
else {
    Write-Host "Backend URL not available in resource group"
}

Write-Host ""
Show-DeploymentSummary `
    -EnvironmentName $EnvironmentName `
    -Urls $urls `
    -HealthResults $healthResults `
    -AzureContext $context

Write-Host "✓ Postdeploy verification completed."
Write-Host ""

exit 0
