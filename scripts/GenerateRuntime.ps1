[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (!(Get-Command pac -ErrorAction SilentlyContinue)) {
    throw 'Install Power Platform CLI to regenerate the platform runtime.'
}
# The final directory name determines the generated namespace.
$output = Join-Path $root ('artifacts/generation/' + [Guid]::NewGuid().ToString('N') + '/Company.Crm.Plugins')
& pac plugin init --outputDirectory $output --author Company --skip-signing
if ($LASTEXITCODE -ne 0) { throw 'pac plugin init failed.' }
$source = Join-Path $output 'PluginBase.cs'
if (!(Test-Path -LiteralPath $source)) { throw 'PAC did not produce PluginBase.cs.' }
$destination = Join-Path $root 'src/Company.Crm.Plugins/Shared/Generated'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item -LiteralPath $source -Destination (Join-Path $destination 'PluginBase.cs') -Force
$manifest = [ordered]@{
    generator = 'Microsoft Power Platform CLI'
    command = 'pac plugin init --outputDirectory <temporary>/Company.Crm.Plugins --author Company --skip-signing'
    file = 'PluginBase.cs'
    sha256 = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
    policy = 'Unmodified generator output. Template extensions are outside Generated.'
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $destination 'provenance.json') -Encoding UTF8
Write-Host 'Updated unmodified PAC runtime. Review generator changes and run npm test and npm run build.'
