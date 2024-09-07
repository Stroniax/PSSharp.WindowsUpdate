#requires -RunAsAdministrator

[CmdletBinding()]
param()
process {

    Stop-Service -Name wuauserv -Force

    Remove-Item -Path 'C:\Windows\SoftwareDistribution' -Recurse -Force

    Start-Service -Name wuauserv
}