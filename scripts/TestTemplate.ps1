[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$configuration = Join-Path $root '.template.config/template.json'
if (!(Test-Path -LiteralPath $configuration)) { throw 'Run this script from the template source repository.' }
$sourceName = (Get-Content -LiteralPath $configuration -Raw | ConvertFrom-Json).sourceName
$workspace = Join-Path $root ('artifacts/template-tests/' + [Guid]::NewGuid().ToString('N'))
$hive = Join-Path $workspace 'hive'
$output = Join-Path $workspace 'Audit.Sales'
$example = Join-Path $workspace 'Audit.Support'
New-Item -ItemType Directory -Path $workspace -Force | Out-Null

function Assert-LastCommand([string]$description) {
    if ($LASTEXITCODE -ne 0) { throw "$description failed (exit $LASTEXITCODE)." }
}
function Assert-CleanOutput([string]$directory) {
    $files = @(Get-ChildItem -LiteralPath $directory -Recurse -File -Force)
    $forbidden = @($files | Where-Object {
        $_.Extension -in @('.snk', '.pfx', '.p12', '.dll', '.exe', '.binlog') -or
        $_.FullName.Substring($directory.Length) -match '[\\/](bin|obj|artifacts|node_modules|[.]vs|[.]git)[\\/]' -or
        ($_.FullName.Substring($directory.Length) -match '[\\/]Generated[\\/]' -and $_.Extension -eq '.cs' -and $_.FullName -notlike '*EarlyBound\Generated\Entities\account.cs')
    })
    if ($forbidden.Count) { throw ('Unexpected local/generated files: ' + ($forbidden.FullName -join ', ')) }
    $solutions = @($files | Where-Object { $_.Extension -eq '.sln' })
    if ($solutions.Count -ne 1) { throw 'Expected exactly one generated solution.' }
    $models = @($files | Where-Object { $_.FullName -like '*EarlyBound\Generated\Entities\account.cs' })
    foreach ($model in $models) {
        $content = Get-Content -LiteralPath $model.FullName -Raw
        if ($content -notmatch 'Minimal Account model for template examples and tests' -or $content -match 'dhc_') { throw 'Template contains environment-specific Account metadata.' }
    }
    $textFiles = @($files | Where-Object { $_.Extension -in @('.cs', '.csproj', '.sln', '.json', '.props', '.targets', '.ps1', '.cjs', '.md', '.yml') })
    $leaks = @($textFiles | Select-String -SimpleMatch $sourceName)
    if ($leaks.Count) { throw ('Unreplaced source prefix: ' + ($leaks.Path -join ', ')) }
}

& dotnet new install $root --debug:custom-hive $hive
Assert-LastCommand 'Template installation'
& dotnet new dataverse-starter -n Audit.Sales -o $output --debug:custom-hive $hive
Assert-LastCommand 'Default template generation'
Assert-CleanOutput $output
if (Test-Path -LiteralPath (Join-Path $output 'src/Audit.Sales.Plugins/Entities/Accounts')) { throw 'Default output contains Account examples.' }
if (!(Test-Path -LiteralPath (Join-Path $output 'Audit.Sales.sln'))) { throw 'Solution name was not replaced.' }
$originalGuids = (Get-Content -LiteralPath $configuration -Raw | ConvertFrom-Json).guids
$solution = Get-Content -LiteralPath (Join-Path $output 'Audit.Sales.sln') -Raw
foreach ($guid in $originalGuids) { if ($solution -match [regex]::Escape($guid)) { throw 'Solution retained a template GUID.' } }

& dotnet new dataverse-starter -n Audit.Support -o $example --include-account-example --debug:custom-hive $hive
Assert-LastCommand 'Account example generation'
Assert-CleanOutput $example
if (!(Test-Path -LiteralPath (Join-Path $example 'src/Audit.Support.Plugins/Entities/Accounts/PreCreateAccountPlugin.cs'))) { throw 'Optional Account plugin is missing.' }
$exampleProps = Get-Content -LiteralPath (Join-Path $example 'Directory.Build.props') -Raw
if ($exampleProps -notmatch '>true</IncludeAccountExample>') { throw 'Optional example is not enabled.' }
$defaultProps = Get-Content -LiteralPath (Join-Path $output 'Directory.Build.props') -Raw
if ($defaultProps -notmatch '>false</IncludeAccountExample>' -or $defaultProps -match '>true</IncludeAccountExample>') { throw 'Default example setting is incorrect.' }

Push-Location $output
try {
    & npm.cmd run setup
    Assert-LastCommand 'Generated project setup'
    $key = Join-Path $output 'build/Plugin.snk'
    $keyHash = (Get-FileHash -LiteralPath $key).Hash
    & npm.cmd run plugins:init
    Assert-LastCommand 'Repeated signing initialization'
    if ((Get-FileHash -LiteralPath $key).Hash -ne $keyHash) { throw 'Initialization changed the existing signing key.' }
    & npm.cmd run check
    Assert-LastCommand 'Generated project checks'
    $spkl = Get-Content -LiteralPath (Join-Path $output 'src/Audit.Sales.Plugins/spkl.json') -Raw | ConvertFrom-Json
    $pluginDirectory = Join-Path $output 'src/Audit.Sales.Plugins'
    $artifact = [IO.Path]::GetFullPath((Join-Path $pluginDirectory $spkl.plugins[0].assemblypath))
    if (!(Test-Path -LiteralPath $artifact)) { throw 'Sparkle does not point to the generated DLL.' }
} finally { Pop-Location }
Write-Host "Template verified without CRM authentication. Artifacts: $workspace"
Push-Location $example
try {
    & npm.cmd run setup
    Assert-LastCommand 'Account example setup'
    & npm.cmd run check
    Assert-LastCommand 'Account example with minimal OOTB model'
} finally { Pop-Location }
Write-Host 'Both template variants fully built and tested without CRM authentication.'
