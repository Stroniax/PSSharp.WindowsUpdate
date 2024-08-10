if ($PSVersionTable.PSVersion.Major -ge 7) {
    $tfm = 'net8.0'
}
else {
    $tfm = 'netstandard2.0'

}

dotnet publish $PSScriptRoot/PSSharp.WindowsUpdate.Commands --framework $tfm --configuration Debug
Get-ChildItem -Path $PSScriptRoot/PSSharp.WindowsUpdate.Commands/bin/Debug/$tfm/Publish -Include '*.dll' -Exclude 'PSSharp.WindowsUpdate.Commands.dll' -Recurse | Import-Module
Import-Module $PSScriptRoot/PSSharp.WindowsUpdate.Commands/bin/Debug/$tfm/PSSharp.WindowsUpdate.Commands.dll

Get-ChildItem $PSScriptRoot/PSSharp.WindowsUpdate/* -Recurse -Include '*.psm1' | Import-Module

$Files = Get-ChildItem $PSScriptRoot/PSSharp.WindowsUpdate/* -Recurse -Include '*.ps1'
foreach ($File in $Files) {
    . $File.FullName
}

Get-ChildItem $PSScriptRoot/PSSharp.WindowsUpdate/* -Recurse -Include '*.types.ps1xml' | ForEach-Object {
    Update-TypeData -AppendPath $_.FullName
}

Get-ChildItem $PSScriptRoot/PSSharp.WindowsUpdate/* -Recurse -Include '*.formats.ps1xml' | ForEach-Object {
    Update-FormatData -AppendPath $_.FullName
}