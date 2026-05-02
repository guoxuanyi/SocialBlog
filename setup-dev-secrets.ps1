param(
    [string]$ProjectPath = 'd:\Blog\Backend\SocialBlog.Api\SocialBlog.Api.csproj'
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$message) {
    Write-Host ''
    Write-Host ('==> ' + $message) -ForegroundColor Cyan
}

function New-SecureJwtKey([int]$byteLength = 64) {
    $bytes = New-Object byte[] $byteLength
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($bytes)
    }
    finally {
        $rng.Dispose()
    }
    [Convert]::ToBase64String($bytes)
}

Write-Step 'Check dotnet'
& dotnet --version | Out-Null

Write-Step 'Generate JWT symmetric key (Base64, 64 random bytes)'
$jwtKey = New-SecureJwtKey 64
Write-Host 'JWT Key generated (hidden).' -ForegroundColor Green

Write-Step 'Write User Secrets (Jwt:Key)'
& dotnet user-secrets set 'Jwt:Key' $jwtKey --project $ProjectPath | Out-Null
Write-Host 'User Secrets updated.' -ForegroundColor Green

Write-Step 'Update appsettings.Development.json (non-secret values only)'
$devSettingsPath = Join-Path (Split-Path $ProjectPath -Parent) 'appsettings.Development.json'
if (Test-Path $devSettingsPath) {
    $json = Get-Content -Raw -Path $devSettingsPath | ConvertFrom-Json
    if (-not $json.Jwt) { $json | Add-Member -MemberType NoteProperty -Name 'Jwt' -Value ([pscustomobject]@{}) }
    $json.Jwt.Issuer = 'SocialBlog'
    $json.Jwt.Audience = 'SocialBlog.Client'
    $json.Jwt.AccessTokenMinutes = 60

    $json | ConvertTo-Json -Depth 10 | Set-Content -Path $devSettingsPath -Encoding UTF8
    Write-Host ('Updated: ' + $devSettingsPath) -ForegroundColor Green
}
else {
    Write-Host ('Missing: ' + $devSettingsPath + ' (skipped)') -ForegroundColor Yellow
}

Write-Step 'Done'
Write-Host ('Next: dotnet run --project "' + $ProjectPath + '" --launch-profile http') -ForegroundColor Green
