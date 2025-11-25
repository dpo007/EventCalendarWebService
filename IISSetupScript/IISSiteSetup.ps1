<#
.SYNOPSIS
    Deploys Event Calendar Web Service to IIS
.DESCRIPTION
    Automates IIS configuration for Event Calendar Web Service:
    - Creates dedicated application pool (No Managed Code for .NET 9)
    - Sets folder permissions for IIS_IUSRS
    - Creates IIS website with HTTP binding
.PARAMETER AppPath
    Physical path where application files are located
.PARAMETER SiteName
    Name for the IIS website
.PARAMETER AppPoolName
    Name for the application pool
.PARAMETER Port
    HTTP port number (default: 12320)
.PARAMETER HostName
    Optional host name for binding (e.g., eventcalendar.company.com)
.EXAMPLE
    .\IISSiteSetup.ps1 -AppPath "C:\inetpub\wwwroot\EventCalendarWebService"
.EXAMPLE
    .\IISSiteSetup.ps1 -AppPath "C:\inetpub\wwwroot\EventCalendarWebService" -Port 8080 -HostName "eventcalendar.company.com"
#>

#Requires -Version 5.1
#Requires -RunAsAdministrator

param(
    [Parameter(Mandatory=$true)]
    [string]$AppPath,
    
    [Parameter(Mandatory=$false)]
    [string]$SiteName = "EventCalendarWebService",
    
    [Parameter(Mandatory=$false)]
    [string]$AppPoolName = "EventCalendarWSAppPool",
    
    [Parameter(Mandatory=$false)]
    [int]$Port = 12320,
    
    [Parameter(Mandatory=$false)]
    [string]$HostName = ""
)

Import-Module WebAdministration

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Event Calendar Web Service - IIS Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Validate application path
Write-Host "[1/5] Validating application path..." -ForegroundColor Yellow
if (-not (Test-Path $AppPath)) {
    Write-Host "ERROR: Application path does not exist: $AppPath" -ForegroundColor Red
    Write-Host "Please publish the application first or specify correct path." -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Application path validated: $AppPath" -ForegroundColor Green
Write-Host ""

# Step 2: Set folder permissions
Write-Host "[2/5] Configuring folder permissions..." -ForegroundColor Yellow
try {
    $acl = Get-Acl $AppPath
    
    # Add IIS_IUSRS with Read & Execute
    $iisUsersRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "IIS_IUSRS",
        "ReadAndExecute",
        "ContainerInherit,ObjectInherit",
        "None",
        "Allow"
    )
    $acl.AddAccessRule($iisUsersRule)
    
    # Add Application Pool Identity (will be created in next step)
    $appPoolIdentity = "IIS AppPool\$AppPoolName"
    $appPoolRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        $appPoolIdentity,
        "Read",
        "ContainerInherit,ObjectInherit",
        "None",
        "Allow"
    )
    $acl.AddAccessRule($appPoolRule)
    
    Set-Acl $AppPath $acl
    Write-Host "[OK] Folder permissions configured for IIS_IUSRS and $appPoolIdentity" -ForegroundColor Green
}
catch {
    Write-Host "WARNING: Could not set permissions. You may need to configure manually." -ForegroundColor Yellow
    Write-Host "Error: $_" -ForegroundColor Yellow
}
Write-Host ""

# Step 3: Create Application Pool
Write-Host "[3/5] Creating Application Pool..." -ForegroundColor Yellow
if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Write-Host "WARNING: Application Pool '$AppPoolName' already exists. Updating configuration..." -ForegroundColor Yellow
    Remove-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

try {
    $appPool = New-WebAppPool -Name $AppPoolName
    
    # Configure for .NET 9 (No Managed Code)
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
    
    # Set to Integrated pipeline mode
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedPipelineMode" -Value "Integrated"
    
    # Performance optimizations
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "startMode" -Value "AlwaysRunning"
    Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "processModel.idleTimeout" -Value ([TimeSpan]::FromMinutes(0))
    
    Write-Host "[OK] Application Pool '$AppPoolName' created successfully" -ForegroundColor Green
    Write-Host "  - .NET CLR Version: No Managed Code (.NET 9)" -ForegroundColor Gray
    Write-Host "  - Pipeline Mode: Integrated" -ForegroundColor Gray
    Write-Host "  - Start Mode: AlwaysRunning" -ForegroundColor Gray
    Write-Host "  - Idle Timeout: Disabled (0)" -ForegroundColor Gray
}
catch {
    Write-Host "ERROR: Failed to create application pool" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 4: Create Website
Write-Host "[4/5] Creating IIS Website..." -ForegroundColor Yellow
if (Test-Path "IIS:\Sites\$SiteName") {
    Write-Host "WARNING: Website '$SiteName' already exists. Removing..." -ForegroundColor Yellow
    Remove-Website -Name $SiteName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

try {
    # Build binding information
    $bindingInfo = "*:${Port}:"
    if ($HostName -ne "") {
        $bindingInfo = "*:${Port}:$HostName"
    }
    
    # Create website
    $website = New-Website -Name $SiteName `
                          -PhysicalPath $AppPath `
                          -ApplicationPool $AppPoolName `
                          -BindingInformation $bindingInfo `
                          -Protocol "http"
    
    Write-Host "[OK] Website '$SiteName' created successfully" -ForegroundColor Green
    Write-Host "  - Physical Path: $AppPath" -ForegroundColor Gray
    Write-Host "  - Application Pool: $AppPoolName" -ForegroundColor Gray
    if ($HostName -ne "") {
        Write-Host "  - URL: http://${HostName}:${Port}" -ForegroundColor Gray
    } else {
        Write-Host "  - URL: http://localhost:${Port}" -ForegroundColor Gray
    }
}
catch {
    Write-Host "ERROR: Failed to create website" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 5: Verify and start website
Write-Host "[5/5] Starting website and verifying..." -ForegroundColor Yellow
try {
    Start-Website -Name $SiteName
    Start-WebAppPool -Name $AppPoolName
    Start-Sleep -Seconds 2
    
    $siteState = (Get-Website -Name $SiteName).State
    $appPoolState = (Get-WebAppPool -Name $AppPoolName).State
    
    if ($siteState -eq "Started" -and $appPoolState -eq "Started") {
        Write-Host "[OK] Website and Application Pool are running" -ForegroundColor Green
    } else {
        Write-Host "WARNING: Website State: $siteState | App Pool State: $appPoolState" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "WARNING: Could not start website automatically" -ForegroundColor Yellow
    Write-Host "Error: $_" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "[OK] Application Pool: $AppPoolName" -ForegroundColor Green
Write-Host "[OK] Website: $SiteName" -ForegroundColor Green
Write-Host "[OK] Path: $AppPath" -ForegroundColor Green

if ($HostName -ne "") {
    Write-Host "[OK] HTTP Binding: http://${HostName}:${Port}" -ForegroundColor Green
} else {
    Write-Host "[OK] HTTP Binding: http://localhost:${Port}" -ForegroundColor Green
}

Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Test the health endpoint: http://localhost:${Port}/health" -ForegroundColor White
Write-Host "2. Test appointments endpoint: http://localhost:${Port}/api/appointments" -ForegroundColor White
Write-Host "3. Configure HTTPS binding manually (ex: port 12321) with your SSL certificate" -ForegroundColor White
Write-Host "4. Review Event Viewer (Application logs) if issues occur" -ForegroundColor White
Write-Host "5. Check IIS logs at: C:\inetpub\logs\LogFiles\" -ForegroundColor White
Write-Host ""
Write-Host "HTTPS Configuration (Manual):" -ForegroundColor Yellow
Write-Host "- In IIS Manager: Right-click '$SiteName' > Edit Bindings" -ForegroundColor White
Write-Host "- Add HTTPS binding (ex: port 12321) with your SSL certificate" -ForegroundColor White
Write-Host ""
