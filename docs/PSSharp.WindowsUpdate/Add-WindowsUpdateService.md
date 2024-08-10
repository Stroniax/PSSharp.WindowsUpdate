---
external help file: PSSharp.WindowsUpdate.Commands.dll-Help.xml
Module Name: PSSharp.WindowsUpdate
online version:
schema: 2.0.0
---

# Add-WindowsUpdateService

## SYNOPSIS

Adds a service endpoint from which this computer may retrieve Windows Updates.

## SYNTAX

### Online (Default)

```
Add-WindowsUpdateService [-ServiceID] <String> [-AllowScheduledRetry] [-RegisterServiceWithAU]
 [-SkipImmediateRegistration] [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

### Local

```
Add-WindowsUpdateService [-ServiceID] <String> [-AuthorizationCabPath] <String> [-RegisterServiceWithAU]
 [-ProgressAction <ActionPreference>] [-WhatIf] [-Confirm] [<CommonParameters>]
```

## DESCRIPTION

{{ Fill in the Description }}

## EXAMPLES

### Example 1

```powershell
PS C:\> Add-WindowsUpdateService -ServiceID $ServiceID -AllowScheduledRetry

# This is equivalent to the following
PS C:\> $WUSM = New-Object -ComObject Microsoft.Update.ServiceManager
PS C:\> $Flags = 3 # AllowPendingRegistration | AllowOnlineRegistration
PS C:\> $WUSM.AddService($ServiceID, $Flags, $null)
```

{{ Add example description here }}

### Example 2

```powershell
PS C:\> Add-WindowsUpdateService -ServiceID $ServiceID -AllowScheduledRetry -SkipImmediateRegistration

# This is equivalent to the following
PS C:\> $WUSM = New-Object -ComObject Microsoft.Update.ServiceManager
PS C:\> $Flags = 1 # AllowPendingRegistration
PS C:\> $WUSM.AddService($ServiceID, $Flags, $null)
```

### Example 3

```powershell
PS C:\Full\Path\To> Add-WindowsUpdateService -ServiceID $ServiceID -AuthorizationCabPath ./service.cab

# This is equivalent to the following
PS C:\full\path\to\> $WUSM = New-Object -ComObject Microsoft.Update.ServiceManager
PS C:\> $WUSM.AddService($ServiceID, 0, 'C:\full\path\to\service.cab')
```

## PARAMETERS

### -AllowScheduledRetry

This flag allows an online Windows Update Service to be added when the next Automatic Update scan happens.
Normally, the service will be added immediately, and if it fails this flag will allow it to remain 'Pending'
until a later Automatic Update scan.

It is equivalent to the 'AllowPendingRegistration' flag in the Windows Update API.

```yaml
Type: System.Management.Automation.SwitchParameter
Parameter Sets: Online
Aliases: AllowPendingRegistration

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -SkipImmediateRegistration

This flag allows an online Windows Update Service to _not_ be registered immediately. Instead, it will only
(attempt to) be added to the service manager when the next Automatic Updates scan happens.

This is a dynamic parameter that is only available in conjunction with `AllowPendingRegistration`.

When this parameter is present, there is no indication of whether or not the service has been registered
successfully barring its presence on the machine later on.

```yaml
Type: System.Management.Automation.SwitchParameter
Parameter Sets: Online

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -AuthorizationCabPath

A path (relative or qualified) to the authorization CAB file.

```yaml
Type: System.String
Parameter Sets: Local
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Confirm

Prompts you for confirmation before running the cmdlet.

```yaml
Type: System.Management.Automation.SwitchParameter
Parameter Sets: (All)
Aliases: cf

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -RegisterServiceWithAU

Registers the newly created service with Windows Automatic Updates such that, when an Automatic Updates
scan occurs, this service endpoint will be queried for any available Windows Updates.

```yaml
Type: System.Management.Automation.SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -ServiceID

A unique identifier of the service to be added.

```yaml
Type: System.Guid
Parameter Sets: (All)
Aliases:

Required: True
Position: 0
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -WhatIf

Shows what would happen if the cmdlet runs.
The cmdlet is not run.

```yaml
Type: System.Management.Automation.SwitchParameter
Parameter Sets: (All)
Aliases: wi

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### PSSharp.WindowsUpdate.Commands.WindowsUpdateServiceRegistration

## NOTES

## RELATED LINKS

https://learn.microsoft.com/en-us/windows/win32/api/wuapi/nf-wuapi-iupdateservicemanager2-addservice2
