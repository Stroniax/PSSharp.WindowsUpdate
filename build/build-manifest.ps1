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
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

$Contents = Get-ChildItem -Path $OutputPath -Recurse -File -Name

$Manifest = @{
    Path                     = Join-Path $OutputPath PSSharp.WindowsUpdate.psd1

    # Contents
    RootModule               = 'PSSharp.WindowsUpdate.Commands.dll'
    NestedModules            = @('PSSharp.WindowsUpdate.psm1')
    TypesToProcess           = $Contents | Where-Object { $_.EndsWith('.types.ps1xml') }
    FormatsToProcess         = $Contents | Where-Object { $_.EndsWith('.formats.ps1xml') }
    RequiredAssemblies       = $Contents | Where-Object { $_.EndsWith('.dll') }
    ScriptsToProcess         = @()
    FileList                 = $Contents
    ModuleList               = @()

    # Public API
    FunctionsToExport        = @(
    )
    CmdletsToExport          = @(
    )
    AliasesToExport          = @()
    VariablesToExport        = @()
    PowerShellVersion        = '5.1'

    # Requirements
    RequiredModules          = @()
    # ExternalModuleDependencies = @()
    CompatiblePSEditions     = @('Desktop', 'Core')

    # package metadata
    Guid                     = New-Guid
    Author                   = 'Caleb Frederickson'
    CompanyName              = 'PSSharp'
    Copyright                = 'Copyright (c) PSSharp 2024'
    Description              = 'Manage downloading and installing Windows Updates with PowerShell.'
    ModuleVersion            = $Version
    ProjectUri               = 'https://github.com/KPBSDInfoSvcs/PSSharp.WindowsUpdate.PowerShell'
    HelpInfoUri              = 'https://github.com/KPBSDInfoSvcs/PSSharp.WindowsUpdate.PowerShell'
    LicenseUri               = 'http://opensource.org/licenses/MIT'
    ReleaseNotes             = 'Find the release notes on GitHub'
    Tags                     = @('PSSharp', 'Windows', 'Updates', 'Install', 'Download', 'PSWindowsUpdate')
    RequireLicenseAcceptance = $false
}

if ($Version.PreReleaseLabel) {
    $Manifest.Prerelease = $Version.PreReleaseLabel
}

New-ModuleManifest @Manifest