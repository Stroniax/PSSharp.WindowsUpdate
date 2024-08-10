param(
    # Path to module or manifest file
    [string]
    $Path = "$PSScriptRoot/../artifacts/PSSharp.WindowsUpdate/0.1.0-dev/PSSharp.WindowsUpdate.psd1",

    [switch]
    $NoExit
)

$script:Module = Import-Module $Path -Force -PassThru -ErrorAction Stop

function Prompt {
    $LastCommand = Get-History -Count 1
    [TimeSpan]$ElapsedTime = $LastCommand.EndExecutionTime - $LastCommand.StartExecutionTime
    $ModuleText = "$($script:Module.Name)/$($script:Module.Version)"
    if ($script:Module.PrivateData.PSData.Prerelease) {
        $ModuleText += "-$($script:Module.PrivateData.PSData.Prerelease)"
    } 
    "$($PSStyle.Foreground.BrightBlack)" +
    "$($ElapsedTime.ToString('mm\:ss\.fff')) - Finished at " +
    $LastCommand.EndExecutionTime.ToString('HH:mm:ss.fff') +
    "`r`n@ $pwd$($PSStyle.Reset)`n" +
    "[$($PSStyle.Foreground.Yellow)$PID$($PSStyle.Reset)] " +
    "$ModuleText> "
}

Set-PSReadLineOption -PredictionSource HistoryAndPlugin -PredictionViewStyle ListView
if ($NoExit) {
    $Host.EnterNestedPrompt()
}