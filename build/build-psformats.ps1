# Build type files

param(
    [System.Management.Automation.SemanticVersion]
    $Version = '0.1.0-dev',

    [string]
    $SourcePath,

    [string]
    $OutputPath,

    [switch]
    $Force
)

if (-not $SourcePath) {
    $SourcePath = Resolve-Path "$PSScriptRoot/../src/PSSharp.WindowsUpdate" -ErrorAction Stop
}
if (-not $OutputPath) {
    $OutputPath = "$PSScriptRoot/../artifacts/PSSharp.WindowsUpdate/$Version"
}

if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath | Out-Null
}

$FormatsFilePath = Join-Path $OutputPath 'PSSharp.WindowsUpdate.formats.ps1xml'
$FormatFiles = Get-ChildItem $SourcePath -Recurse -Include '*.formats.ps1xml'

if (-not ($FormatFiles)) {
    Write-Host "No format files found in $SourcePath" -ForegroundColor Cyan
    return
}

if (!$Force -and (Test-Path $FormatsFilePath)) {
    $LastModified = (Get-Item $FormatsFilePath).LastWriteTimeUtc
    $UpdateFormats = $false
    foreach ($Child in $FormatFiles) {
        if ($Child.LastWriteTimeUtc -gt $LastModified) {
            $UpdateFormats = $true
            break
        }
    }
}
else {
    $UpdateFormats = $true
}

if ($UpdateFormats) {
    foreach ($FormatFile in $FormatFiles) {
        $CurrentXml = [xml](Get-Content -LiteralPath $FormatFile.FullName)
        if ($FormatXml) {
            foreach ($Node in $CurrentXml.Configuration.ViewDefinitions.ChildNodes) {
                [void]$FormatXml.Configuration.SelectSingleNode('ViewDefinitions').AppendChild($FormatXml.ImportNode($Node, $true))
            }
            foreach ($Node in $CurrentXml.Configuration.SelectionSets.ChildNodes) {
                [void]$FormatXml.Configuration.SelectSingleNode('SelectionSets').AppendChild($FormatXml.ImportNode($Node, $true))
            }
            foreach ($Node in $CurrentXml.Configuration.Controls.ChildNodes) {
                [void]$FormatXml.Configuration.SelectSingleNode('Controls').AppendChild($FormatXml.ImportNode($Node, $true))
            }
        }
        else {
            $FormatXml = $CurrentXml
            if (!$FormatXml.Configuration.ViewDefinitions) {
                [void]$FormatXml.Configuration.AppendChild($FormatXml.CreateElement('ViewDefinitions'))
            }
            if (!$FormatXml.Configuration.SelectionSets) {
                [void]$FormatXml.Configuration.AppendChild($FormatXml.CreateElement('SelectionSets'))
            }
            if (!$FormatXml.Configuration.Controls) {
                [void]$FormatXml.Configuration.AppendChild($FormatXml.CreateElement('Controls'))
            }
        }
    }

    Write-Host "$FormatsFilePath is being updated" -ForegroundColor Magenta
    $FormatXml.Save($FormatsFilePath)
}
else {
    Write-Host "$FormatsFilePath is up to date" -ForegroundColor Cyan
}

Get-Item $FormatsFilePath