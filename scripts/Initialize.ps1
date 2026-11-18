# Run once per new backend. The key establishes assembly identity; keep it stable.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$keyPath = Join-Path $root 'build/Plugin.snk'
if (Test-Path -LiteralPath $keyPath) { Write-Host 'Signing key already exists.'; return }
$rsa = New-Object System.Security.Cryptography.RSACryptoServiceProvider(2048)
try {
    $rsa.PersistKeyInCsp = $false
    [IO.File]::WriteAllBytes($keyPath, $rsa.ExportCspBlob($true))
} finally { $rsa.Dispose() }
Write-Host "Created signing key: $keyPath"
