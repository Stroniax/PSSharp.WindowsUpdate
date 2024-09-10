# PSSharp.WindowsUpdate

PowerShell module for management of Windows Updates. Open source with PowerShell best practices
and ease-of-use in mind. This module has all the bells and whistles:

- native `-AsJob` support to execute long-running operations in the background
- argument completion everywhere the program can suggest a value
- cancellation (Ctrl+C) to stop long-running cmdlets at the terminal, and timeouts to limit long-running cmdlets in a script
- aliases for those that script at the prompt
- common verbs for discoverable features
- fully integrated pipeline support to pass results from one command to the next

This module was brought about due to a desire to improve upon the existing PowerShell module to manage
windows updates, (PSWindowsUpdate)[https://www.powershellgallery.com/packages/PSWindowsUpdate],
which is closed source. Using the module can be confusing, and documentation is slim. In addition,
it lacks many features a user may desire such as native job (multi-threading) support.

## Framework

Utilizes the Windows Update Agent through COM.

https://learn.microsoft.com/en-us/windows/win32/wua_sdk/windows-update-agent-object-model

## Help

I have tried to retain original HResult codes in exceptions raised by the Windows Update Agent API.
If you get an error that needs further clarification, research the HResult.

Error Codes:

- [WUA Success and Error Codes](https://learn.microsoft.com/en-us/windows/win32/wua_sdk/wua-success-and-error-codes-)
- [Windows Update error codes by component](https://learn.microsoft.com/en-us/windows/deployment/update/windows-update-error-reference)

## Command Documentation & Examples

## Test

## Contributing

All pull requests will be considered. Please ensure you follow PowerShell standards; that was one of the
biggest motives in my creating this module in the first place.

If you're looking for an area to contribute, look at [todo](./todo.md) or consider writing tests.
