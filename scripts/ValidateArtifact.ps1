[CmdletBinding()]
param([ValidateSet('Debug','Release')][string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$dll = Join-Path $root "artifacts/plugins/$Configuration/Company.Crm.Plugins.dll"
if (!(Test-Path -LiteralPath $dll)) { throw "Missing artifact: $dll" }
if ((Get-Item -LiteralPath $dll).Length -gt 16MB) { throw 'Plugin assembly exceeds 16 MB.' }
$identity = [Reflection.AssemblyName]::GetAssemblyName($dll)
if ($identity.GetPublicKeyToken().Length -eq 0) { throw 'Plugin artifact is unsigned.' }

# Reflection-only metadata inspection: no dependency execution or file locking.
$assembly = [Reflection.Assembly]::ReflectionOnlyLoad([IO.File]::ReadAllBytes($dll))
$unmerged = @($assembly.GetReferencedAssemblies() | Where-Object {
    $_.Name -like 'Company.Crm.*' -or
    $_.Name -like 'Microsoft.Extensions.DependencyInjection*' -or
    $_.Name -in @('Microsoft.Bcl.AsyncInterfaces', 'System.Threading.Tasks.Extensions',
        'System.Runtime.CompilerServices.Unsafe')
} | ForEach-Object { $_.Name })
if ($unmerged.Count) { throw ('Unmerged dependencies: ' + ($unmerged -join ', ')) }

$runtime = Join-Path $root 'src/Company.Crm.Plugins/Shared/Generated'
$manifest = Get-Content -LiteralPath (Join-Path $runtime 'provenance.json') -Raw | ConvertFrom-Json
$actualHash = (Get-FileHash -LiteralPath (Join-Path $runtime 'PluginBase.cs') -Algorithm SHA256).Hash
if ($actualHash -ne $manifest.sha256) {
    throw 'PAC runtime differs from its generator manifest. Review changes or run npm run runtime:generate.'
}
Write-Host "Validated signed artifact and original PAC runtime: $dll"
