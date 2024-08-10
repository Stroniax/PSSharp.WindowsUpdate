#requires -Version 7.0

[CmdletBinding()]
param(
    [System.Management.Automation.SemanticVersion]
    $Version = '0.1.0-dev',

    [string]
    $OutputPath
)

if (-not $OutputPath) {
    $OutputPath = "$PSScriptRoot/../artifacts/PSSharp.WindowsUpdate/$Version"
}

if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath
}
$ResolvedOutputPath = Resolve-Path $OutputPath -ErrorAction Stop

$DotNetPublish = @(
    'publish'
    '--configuration'
    'Release'
    '--output'
    $ResolvedOutputPath.ProviderPath
    "/p:Version=$Version"
    "$PSScriptRoot/../src/PSSharp.WindowsUpdate.Commands/PSSharp.WindowsUpdate.Commands.csproj"
)

dotnet $DotNetPublish