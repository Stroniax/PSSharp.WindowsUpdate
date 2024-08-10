
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

$TypesFilePath = Join-Path $OutputPath 'PSSharp.WindowsUpdate.types.ps1xml'
$TypeFiles = Get-ChildItem $SourcePath -Recurse -Include '*.types.ps1xml'

if (!$Force -and (Test-Path $TypesFilePath)) {
    $UpdateTypes = $false
    $LastModified = (Get-Item $TypesFilePath).LastWriteTimeUtc
    foreach ($Child in $TypesFiles) {
        if ($Child.LastWriteTimeUtc -gt $LastModified) {
            $UpdateTypes = $true
            break
        }
    }
}
else {
    $UpdateTypes = $true
}

if ($UpdateTypes) {
    foreach ($TypeFile in $TypeFiles) {
        $CurrentXml = [xml](Get-Content -LiteralPath $TypeFile.FullName)
        if ($TypeXml) {
            $TypeXml.Types.InnerXml += $CurrentXml.Types.InnerXml
        }
        else {
            $TypeXml = $CurrentXml
        }
    }

    Write-Host "$TypesFilePath is being updated" -ForegroundColor Magenta
    $TypeXml.Save($TypesFilePath)
}
else {
    Write-Host "$TypesFilePath is up to date" -ForegroundColor Cyan
}

Get-Item $TypesFilePath