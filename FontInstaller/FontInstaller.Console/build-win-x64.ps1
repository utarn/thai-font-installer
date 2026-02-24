# Build script for FontInstaller - Windows AMD64
# Builds self-contained executable for Windows x64
# Includes code signing with USB token certificate auto-detection

$ErrorActionPreference = "Stop"

$Platform = "win-x64"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$OutputDir = Join-Path $ScriptDir "bin\publish\$Platform"
$DistDir = Join-Path $ScriptDir "dist"
$AppName = "fontinstaller"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Building FontInstaller for $Platform" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# Load .env file if it exists
$EnvFile = Join-Path $ScriptDir ".env"
if (-not (Test-Path $EnvFile)) {
    $EnvFile = Join-Path $ScriptDir "..\.env"
}
if (Test-Path $EnvFile) {
    Write-Host "Loading environment variables from .env" -ForegroundColor Yellow
    Get-Content $EnvFile | ForEach-Object {
        if ($_ -match '^([^#][^=]+)=(.*)$') {
            [Environment]::SetEnvironmentVariable($matches[1].Trim(), $matches[2].Trim(), "Process")
        }
    }
}

# Function to find NTI code signing certificate
function Find-CodeSigningCertificate {
    Write-Host "Searching for code signing certificate..." -ForegroundColor Yellow
    
    # Search in CurrentUser store first (includes USB tokens)
    $certs = Get-ChildItem -Path Cert:\CurrentUser\My -CodeSigningCert | Where-Object {
        $_.NotAfter -gt (Get-Date) -and $_.NotBefore -lt (Get-Date)
    }
    
    if ($certs.Count -eq 0) {
        # Search in LocalMachine store
        $certs = Get-ChildItem -Path Cert:\LocalMachine\My -CodeSigningCert | Where-Object {
            $_.NotAfter -gt (Get-Date) -and $_.NotBefore -lt (Get-Date)
        }
    }
    
    if ($certs.Count -eq 0) {
        Write-Host "No valid code signing certificates found." -ForegroundColor Yellow
        return $null
    }
    
    # Look specifically for common certificate subjects
    $targetCerts = $certs | Where-Object {
        $_.Subject -match 'CN="?FONT INSTALLER SIGNING CERTIFICATE?"?' -or
        $_.Subject -match 'CN="?YOUR COMPANY NAME?"?' -or
        $_.Subject -like '*FONT*' -or
        $_.Subject -like '*INSTALLER*'
    } | Select-Object -First 1
    
    if ($targetCerts) {
        return $targetCerts
    }
    
    Write-Host "Specific font installer certificate not found. Using first available code signing certificate." -ForegroundColor Yellow
    
    # Return the first valid certificate as fallback
    return $certs | Select-Object -First 1
}

# Function to sign executable
function Sign-Executable {
    param (
        [string]$FilePath,
        [System.Security.Cryptography.X509Certificates.X509Certificate2]$Certificate
    )
    
    Write-Host "Signing $FilePath..." -ForegroundColor Yellow

    # Try to use signtool from Windows SDK
    $signtoolPaths = @(
        "c:\signtool\signtool.exe",
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe",
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.22000.0\x64\signtool.exe",
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe",
        "${env:ProgramFiles(x86)}\Windows Kits\10\bin\x64\signtool.exe",
        "${env:ProgramFiles(x86)}\Microsoft SDKs\ClickOnce\SignTool\signtool.exe"
    )
    
    $signtool = $null
    foreach ($path in $signtoolPaths) {
        if (Test-Path $path) {
            $signtool = $path
            break
        }
    }
    
    # Also check PATH
    if (-not $signtool) {
        $signtoolFromPath = Get-Command signtool.exe -ErrorAction SilentlyContinue
        if ($signtoolFromPath) {
            $signtool = $signtoolFromPath.Source
        }
    }
    
    if (-not $signtool) {
        Write-Host "Warning: signtool.exe not found. Please install Windows SDK." -ForegroundColor Yellow
        Write-Host "Download from: https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/" -ForegroundColor Yellow
        return $false
    }
    
    Write-Host "Using signtool: $signtool" -ForegroundColor Gray
    Write-Host "Certificate: $($Certificate.Subject)" -ForegroundColor Gray
    
    # Extract subject name for signing
    $certSubject = $Certificate.Subject.Split(',')[0].Replace('CN=', '').Replace('"', '').Trim()
    
    # Sign with SHA256 and timestamp using /n flag (subject name) for better USB token compatibility
    $timestampServers = @(
        "http://timestamp.digicert.com",
        "http://timestamp.sectigo.com",
        "http://timestamp.globalsign.com/scripts/timstamp.dll",
        "http://tsa.starfieldtech.com"
    )
    
    $signed = $false
    foreach ($tsServer in $timestampServers) {
        Write-Host "Trying timestamp server: $tsServer" -ForegroundColor Gray
        
        # Build command line using /n flag (subject name) instead of /sha1 for better USB token compatibility
        $signArgs = "sign /fd SHA256 /n `"$certSubject`" /t `"$tsServer`" /d `"Font Installer`" /du `"https://font-installer.example.com`" `"$FilePath`""
        
        $process = Start-Process -FilePath $signtool -ArgumentList $signArgs -Wait -PassThru -NoNewWindow
        
        if ($process.ExitCode -eq 0) {
            $signed = $true
            Write-Host "Successfully signed with timestamp from $tsServer" -ForegroundColor Green
            break
        }
    }
    
    if (-not $signed) {
        Write-Host "Warning: Failed to sign with timestamp. Signing without timestamp..." -ForegroundColor Yellow
        
        # Build command line using /n flag (subject name) without timestamp
        $signArgs = "sign /fd SHA256 /n `"$certSubject`" /d `"Font Installer`" `"$FilePath`""
        
        $process = Start-Process -FilePath $signtool -ArgumentList $signArgs -Wait -PassThru -NoNewWindow
        
        if ($process.ExitCode -eq 0) {
            $signed = $true
            Write-Host "Signed successfully (without timestamp)" -ForegroundColor Yellow
        }
    }
    
    return $signed
}

# Clean previous build for this platform
Write-Host "`nCleaning previous build for $Platform..." -ForegroundColor Yellow
if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}

# Build as single file executable
Write-Host "`nBuilding as single file executable..." -ForegroundColor Yellow
$CsprojPath = Join-Path $ScriptDir "FontInstaller.Console.csproj"

dotnet publish $CsprojPath `
    -c Release `
    -r $Platform `
    -o $OutputDir `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

$ExePath = Join-Path $OutputDir "$AppName.exe"

if (-not (Test-Path $ExePath)) {
    Write-Host "Error: Executable not found at $ExePath" -ForegroundColor Red
    exit 1
}

# Code signing
$SigningCert = Find-CodeSigningCertificate
$SignedSuccessfully = $false

if ($SigningCert) {
    Write-Host "`nFound code signing certificate:" -ForegroundColor Green
    Write-Host "  Subject: $($SigningCert.Subject)" -ForegroundColor White
    Write-Host "  Issuer: $($SigningCert.Issuer)" -ForegroundColor White
    Write-Host "  Valid until: $($SigningCert.NotAfter)" -ForegroundColor White
    Write-Host "  Thumbprint: $($SigningCert.Thumbprint)" -ForegroundColor White
    
    # Check if this is a hardware token certificate
    if ($SigningCert.PrivateKey -eq $null) {
        Write-Host "`nNote: This appears to be a hardware token certificate (USB token/HSM)." -ForegroundColor Cyan
        Write-Host "You may be prompted to enter your PIN." -ForegroundColor Cyan
    }
    
    $SignedSuccessfully = Sign-Executable -FilePath $ExePath -Certificate $SigningCert
} else {
    Write-Host "`nNo code signing certificate found. Executable will not be signed." -ForegroundColor Yellow
    Write-Host "To sign, insert your USB token or install a code signing certificate." -ForegroundColor Yellow
}

# Copy to dist folder with platform suffix
Write-Host "`nCopying to dist folder..." -ForegroundColor Yellow
if (-not (Test-Path $DistDir)) {
    New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
}
$DistFile = Join-Path $DistDir "$AppName.$Platform.exe"
Copy-Item -Path $ExePath -Destination $DistFile -Force

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "Build complete!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""

Write-Host "Build output: $ExePath" -ForegroundColor White
Write-Host "Dist output:  $DistFile" -ForegroundColor White
Write-Host ""

Write-Host "Note: This is a self-contained Windows binary." -ForegroundColor Gray
Write-Host "      It does NOT require .NET runtime to be installed." -ForegroundColor Gray
Write-Host ""

# Show file size
if (Test-Path $DistFile) {
    $FileInfo = Get-Item $DistFile
    $SizeMB = [math]::Round($FileInfo.Length / 1MB, 2)
    Write-Host "File size: $SizeMB MB" -ForegroundColor White
}

if ($SignedSuccessfully) {
    Write-Host "$(([char]0x2713)) Code signed" -ForegroundColor Green
} elseif ($SigningCert) {
    Write-Host "$(([char]0x2717)) Code signing failed" -ForegroundColor Red
} else {
    Write-Host "- Not code signed (no certificate found)" -ForegroundColor Yellow
}