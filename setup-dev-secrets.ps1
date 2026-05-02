param(
    [string]$ProjectPath = 'd:\Blog\Backend\SocialBlog.Api\SocialBlog.Api.csproj',
    [string]$ApiBaseUrl = 'http://localhost:5197',
    [string]$Username = 'admin',
    [string]$Password,
    [switch]$KeepApiRunning
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

function Ensure-ApiRunning([string]$baseUrl, [string]$projectPath) {
    $hashUrl = ($baseUrl.TrimEnd('/') + '/api/auth/hash')

    try {
        Invoke-RestMethod -Method Post -Uri $hashUrl -ContentType 'application/json' -Body (@{ password = 'ping' } | ConvertTo-Json) | Out-Null
        return $null
    }
    catch {
    }

    Write-Host 'API not running. Starting Development (http)...' -ForegroundColor Yellow
    $proc = Start-Process -FilePath 'dotnet' -ArgumentList @(
        'run',
        '--project', $projectPath,
        '--launch-profile', 'http'
    ) -PassThru -WindowStyle Minimized

    $timeoutSec = 60
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
        try {
            Invoke-RestMethod -Method Post -Uri $hashUrl -ContentType 'application/json' -Body (@{ password = 'ping' } | ConvertTo-Json) | Out-Null
            Write-Host ('API ready: ' + $baseUrl) -ForegroundColor Green
            return $proc
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    try { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue } catch {}
    throw ('API start timeout (' + $timeoutSec + 's). Run manually: dotnet run --project "' + $projectPath + '" --launch-profile http')
}

Write-Step 'Check dotnet'
& dotnet --version | Out-Null

Write-Step 'Generate JWT symmetric key (Base64, 64 random bytes)'
$jwtKey = New-SecureJwtKey 64
Write-Host 'JWT Key generated (hidden).' -ForegroundColor Green

Write-Step 'Write initial User Secrets (Jwt:Key / Auth:DemoUsername)'
& dotnet user-secrets set 'Jwt:Key' $jwtKey --project $ProjectPath | Out-Null
& dotnet user-secrets set 'Auth:DemoUsername' $Username --project $ProjectPath | Out-Null
Write-Host 'Initial User Secrets written.' -ForegroundColor Green

Write-Step 'Get demo password'
if ([string]::IsNullOrWhiteSpace($Password)) {
    $secure = Read-Host 'Enter demo password (hidden)' -AsSecureString
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try {
        $Password = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    }
}

Write-Step 'Ensure API is reachable and request PasswordHasher hash'
$apiProcess = Ensure-ApiRunning -baseUrl $ApiBaseUrl -projectPath $ProjectPath
try {
    $hashUrl = ($ApiBaseUrl.TrimEnd('/') + '/api/auth/hash')
    $hashResponse = Invoke-RestMethod -Method Post -Uri $hashUrl -ContentType 'application/json' -Body (@{
        username = $Username
        password = $Password
    } | ConvertTo-Json)

    $passwordHash = $hashResponse.data.passwordHash
    if ([string]::IsNullOrWhiteSpace($passwordHash)) {
        throw ('Cannot parse passwordHash from response: ' + ($hashResponse | ConvertTo-Json -Depth 10))
    }

    Write-Host 'Password hash generated (hidden).' -ForegroundColor Green
}
finally {
    if ($apiProcess -and -not $KeepApiRunning) {
        Write-Step 'Stop temporary API process'
        try { Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue } catch {}
    }
}

Write-Step 'Write User Secrets (Auth:DemoPasswordHash)'
& dotnet user-secrets set 'Auth:DemoPasswordHash' $passwordHash --project $ProjectPath | Out-Null
& dotnet user-secrets remove 'Auth:DemoPassword' --project $ProjectPath 2>$null | Out-Null
Write-Host 'User Secrets updated.' -ForegroundColor Green

Write-Step 'Update appsettings.Development.json (non-secret values only)'
$devSettingsPath = Join-Path (Split-Path $ProjectPath -Parent) 'appsettings.Development.json'
if (Test-Path $devSettingsPath) {
    $json = Get-Content -Raw -Path $devSettingsPath | ConvertFrom-Json
    if (-not $json.Jwt) { $json | Add-Member -MemberType NoteProperty -Name 'Jwt' -Value ([pscustomobject]@{}) }
    if (-not $json.Auth) { $json | Add-Member -MemberType NoteProperty -Name 'Auth' -Value ([pscustomobject]@{}) }

    $json.Jwt.Issuer = 'SocialBlog'
    $json.Jwt.Audience = 'SocialBlog.Client'
    $json.Jwt.AccessTokenMinutes = 60
    $json.Auth.DemoUsername = $Username

    $json | ConvertTo-Json -Depth 10 | Set-Content -Path $devSettingsPath -Encoding UTF8
    Write-Host ('Updated: ' + $devSettingsPath) -ForegroundColor Green
}
else {
    Write-Host ('Missing: ' + $devSettingsPath + ' (skipped)') -ForegroundColor Yellow
}

Write-Step 'Done'
Write-Host ('Next: dotnet run --project "' + $ProjectPath + '" --launch-profile http') -ForegroundColor Green
Write-Host ('Then: POST ' + $ApiBaseUrl.TrimEnd('/') + '/api/auth/login') -ForegroundColor Green
