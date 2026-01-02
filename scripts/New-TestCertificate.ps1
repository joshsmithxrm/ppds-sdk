<#
.SYNOPSIS
    Creates a self-signed certificate for PPDS integration testing.

.DESCRIPTION
    Generates a certificate for Azure App Registration authentication testing.
    Outputs:
    - .cer file (public key) - upload to Azure App Registration
    - .pfx file (private key) - store as GitHub secret (base64 encoded)
    - Base64 text file - copy to PPDS_TEST_CERT_BASE64 secret

.PARAMETER CertName
    Name for the certificate. Default: "PPDS-IntegrationTests"

.PARAMETER Password
    Password to protect the PFX file. If not provided, prompts for input.

.PARAMETER OutputPath
    Directory to export files. Default: current directory.

.PARAMETER ValidYears
    How many years the certificate is valid. Default: 2

.EXAMPLE
    .\New-TestCertificate.ps1
    # Prompts for password, creates cert in current directory

.EXAMPLE
    .\New-TestCertificate.ps1 -Password "MySecurePass123!" -OutputPath C:\certs
    # Creates cert with specified password in C:\certs

.NOTES
    After running this script:
    1. Upload the .cer file to your Azure App Registration
    2. Add PPDS_TEST_CERT_BASE64 secret (contents of -base64.txt file)
    3. Add PPDS_TEST_CERT_PASSWORD secret (the password you used)
    4. Delete the local files for security
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string]$CertName = "PPDS-IntegrationTests",

    [Parameter()]
    [string]$Password,

    [Parameter()]
    [string]$OutputPath = (Get-Location).Path,

    [Parameter()]
    [int]$ValidYears = 2
)

$ErrorActionPreference = "Stop"

# Prompt for password if not provided
if (-not $Password) {
    $securePassword = Read-Host -Prompt "Enter password for PFX file" -AsSecureString
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    $Password = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
}

if ([string]::IsNullOrWhiteSpace($Password)) {
    Write-Error "Password cannot be empty"
    exit 1
}

$securePass = ConvertTo-SecureString -String $Password -Force -AsPlainText

# Create output directory if needed
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

# File paths
$pfxPath = Join-Path $OutputPath "$CertName.pfx"
$cerPath = Join-Path $OutputPath "$CertName.cer"
$base64Path = Join-Path $OutputPath "$CertName-base64.txt"

Write-Host "Creating self-signed certificate..." -ForegroundColor Cyan

# Create the certificate
$cert = New-SelfSignedCertificate `
    -Subject "CN=$CertName" `
    -DnsName "$CertName.local" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears($ValidYears) `
    -KeySpec KeyExchange `
    -KeyExportPolicy Exportable `
    -KeyLength 2048 `
    -HashAlgorithm SHA256

Write-Host "Certificate created with thumbprint: $($cert.Thumbprint)" -ForegroundColor Green

# Export PFX (with private key)
Write-Host "Exporting PFX (private key)..." -ForegroundColor Cyan
Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $securePass | Out-Null
Write-Host "  -> $pfxPath" -ForegroundColor Gray

# Export CER (public key only)
Write-Host "Exporting CER (public key)..." -ForegroundColor Cyan
Export-Certificate -Cert $cert -FilePath $cerPath | Out-Null
Write-Host "  -> $cerPath" -ForegroundColor Gray

# Convert PFX to Base64
Write-Host "Converting PFX to Base64..." -ForegroundColor Cyan
$pfxBytes = [System.IO.File]::ReadAllBytes($pfxPath)
$pfxBase64 = [System.Convert]::ToBase64String($pfxBytes)
[System.IO.File]::WriteAllText($base64Path, $pfxBase64)
Write-Host "  -> $base64Path" -ForegroundColor Gray

# Copy to clipboard
$pfxBase64 | Set-Clipboard
Write-Host "Base64 copied to clipboard!" -ForegroundColor Green

# Remove from cert store (optional - keep if you want to test locally)
$removeFromStore = Read-Host "Remove certificate from local store? (y/N)"
if ($removeFromStore -eq 'y') {
    Remove-Item "Cert:\CurrentUser\My\$($cert.Thumbprint)" -Force
    Write-Host "Removed from cert store" -ForegroundColor Gray
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Certificate created successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Upload to Azure:" -ForegroundColor White
Write-Host "   Azure Portal -> App Registrations -> Your App -> Certificates & secrets"
Write-Host "   Upload: $cerPath" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Add GitHub Secrets:" -ForegroundColor White
Write-Host "   PPDS_TEST_CERT_BASE64  = contents of $base64Path" -ForegroundColor Gray
Write-Host "   PPDS_TEST_CERT_PASSWORD = $Password" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Delete local files (security):" -ForegroundColor White
Write-Host "   Remove-Item '$pfxPath', '$cerPath', '$base64Path'" -ForegroundColor Gray
Write-Host ""
Write-Host "Thumbprint: $($cert.Thumbprint)" -ForegroundColor Cyan
