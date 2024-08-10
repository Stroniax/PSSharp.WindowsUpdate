#requires -Version 7.0
#requires -Module PlatyPS

[CmdletBinding()]
param(
    [System.Management.Automation.SemanticVersion]
    $Version = '0.1.0-dev',

    [string]
    $OutputPath
)

if (-not $OutputPath) {
    $OutputPath = "$PSScriptRoot/../artifacts/PSSharp.WindowsUpdate.Commands/$Version"
}

New-ExternalHelp -Path $PSScriptRoot/../docs/PSSharp.WindowsUpdate.Commands -OutputPath $OutputPath -Force