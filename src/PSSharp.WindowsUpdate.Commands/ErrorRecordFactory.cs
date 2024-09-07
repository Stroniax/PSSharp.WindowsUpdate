using System.Diagnostics;
using System.Management.Automation;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace PSSharp.WindowsUpdate.Commands;

/// <remarks>
///
/// More error codes can be found at:
/// <a href="https://learn.microsoft.com/en-us/windows/win32/wua_sdk/wua-success-and-error-codes-">WUA Success and Error Codes</a>
/// </remarks>
internal static class ErrorRecordFactory
{
    /// <summary>
    /// Terminal failure when searching for updates.
    /// </summary>
    internal const string UpdateSearchFailure = "UpdateSearchFailure";

    /// <summary>
    /// Non-terminal error when searching for updates.
    /// </summary>
    internal const string UpdateSearchError = "UpdateSearchError";

    internal static Exception CreateBestMatchException(Exception e)
    {
        if (e is COMException com)
        {
            return CreateBestMatchException(com);
        }
        else
        {
            return e;
        }
    }

    internal static Exception CreateBestMatchException(COMException comException)
    {
        if (comException.HResult == unchecked((int)0x80244011))
        {
            return new WUServerPolicyValueMissingException(comException);
        }
        else if (comException.HResult == unchecked((int)0x80240438))
        {
            return new WindowsUpdateSourceNotFoundException(comException);
        }
        else if (comException.HResult == unchecked((int)0x80240004))
        {
            return new WindowsUpdateNotInitializedException(comException);
        }
        else if (comException.HResult == unchecked((int)0x802B0002))
        {
            return new WindowsUpdateLegacyServerException(null, comException);
        }
        else if (comException.HResult == unchecked((int)0x80240022))
        {
            return new WindowsUpdateAllDownloadsFailedException(null, comException);
        }
        else
        {
            return comException;
        }
    }

    internal static ErrorRecord CreateErrorRecord(
        WUServerPolicyValueMissingException e,
        object? targetObject
    )
    {
        SetCurrentStackTrace(e);

        return new ErrorRecord(
            e,
            "WUServerPolicyValueMissing",
            ErrorCategory.ResourceUnavailable,
            targetObject
        )
        {
            ErrorDetails = new("The Windows Update server policy value is missing in the registry.")
            {
                RecommendedAction = "Use the 'WindowsUpdate' or 'Default' server selection."
            }
        };
    }

    internal static ErrorRecord CreateErrorRecord(
        WindowsUpdateSourceNotFoundException e,
        object? targetObject
    )
    {
        SetCurrentStackTrace(e);

        return new ErrorRecord(
            e,
            "WindowsUpdateSourceNotFound",
            ErrorCategory.ResourceUnavailable,
            targetObject
        )
        {
            ErrorDetails = new(
                "The Windows Update source was not found. Is there a working network connection?"
            )
            {
                RecommendedAction = "Check the network connection and try again."
            }
        };
    }

    internal static ErrorRecord CreateErrorRecord(
        WindowsUpdateNotInitializedException e,
        object? targetObject
    )
    {
        SetCurrentStackTrace(e);
        return new ErrorRecord(
            e,
            "WindowsUpdateNotInitialized",
            ErrorCategory.ProtocolError,
            targetObject
        )
        {
            ErrorDetails = new(
                "A component of the Windows Update Agent has not been initialized correctly."
            )
            {
                RecommendedAction =
                    "Report the exact command and parameters you provided on GitHub. Try repeating the operation using a different parameter set."
            }
        };
    }

    internal static ErrorRecord CreateErrorRecord(
        WindowsUpdateLegacyServerException e,
        object? targetObject
    )
    {
        SetCurrentStackTrace(e);

        return new ErrorRecord(
            e,
            "WindowsUpdateServerLegacy",
            ErrorCategory.ConnectionError,
            targetObject
        );
    }

    internal static ErrorRecord CreateErrorRecord(
        WindowsUpdateAllDownloadsFailedException e,
        object? targetObject
    )
    {
        SetCurrentStackTrace(e);

        return new ErrorRecord(
            e,
            "WindowsUpdateAllDownloadsFailed",
            ErrorCategory.ResourceUnavailable,
            targetObject
        )
        {
            ErrorDetails = new(
                "All downloads failed. The Windows Update Agent was unable to download any updates."
            )
            {
                RecommendedAction = "Check the network connection and try again."
            }
        };
    }

    internal static ErrorRecord CreateErrorRecord(
        Exception e,
        object? targetObject,
        string fallbackErrorId
    )
    {
        if (e is WUServerPolicyValueMissingException wuServerPolicyValueMissing)
        {
            return CreateErrorRecord(wuServerPolicyValueMissing, targetObject);
        }
        else if (e is WindowsUpdateSourceNotFoundException sourceNotFound)
        {
            return CreateErrorRecord(sourceNotFound, targetObject);
        }
        else if (e is WindowsUpdateNotInitializedException notInitialized)
        {
            return CreateErrorRecord(notInitialized, targetObject);
        }
        else if (e is WindowsUpdateLegacyServerException legacy)
        {
            return CreateErrorRecord(legacy, targetObject);
        }
        else if (e is WindowsUpdateAllDownloadsFailedException allDownloadsFailed)
        {
            return CreateErrorRecord(allDownloadsFailed, targetObject);
        }
        else
        {
            return new ErrorRecord(e, fallbackErrorId, ErrorCategory.NotSpecified, targetObject);
        }
    }

    /// <summary>
    /// Produces a <see cref="ErrorCategory.ObjectNotFound"/> error containing a <see cref="ItemNotFoundException"/>.
    /// </summary>
    /// <param name="resource">The name of the missing resource. Ex: 'WindowsUpdate'</param>
    /// <param name="name">The property attempted to match by. Ex: 'Name'</param>
    /// <param name="targetObject"></param>
    /// <returns></returns>
    internal static ErrorRecord NotFound(string resource, string name, object? targetObject)
    {
        var exn = new ItemNotFoundException($"The requested {resource} does not exist.");

        SetCurrentStackTrace(exn);

        var err = new ErrorRecord(
            exn,
            $"{resource}NotFound.{name}",
            ErrorCategory.ObjectNotFound,
            targetObject
        )
        {
            ErrorDetails = new($"The {resource} with {name} '{targetObject}' was not found.")
            {
                RecommendedAction = "Verify the spelling of the request and try again."
            }
        };
        return err;
    }

    internal static ErrorRecord AdministratorRequired(string operation)
    {
        var exn = new UnauthorizedAccessException(
            $"Administrator privileges are required to perform the operation."
        );

        SetCurrentStackTrace(exn);

        var err = new ErrorRecord(
            exn,
            "AdministratorRequired",
            ErrorCategory.PermissionDenied,
            operation
        )
        {
            ErrorDetails = new(
                $"Administrator privileges are required to perform the operation '{operation}'."
            )
            {
                RecommendedAction = "Run the command with elevated permissions."
            }
        };
        return err;
    }

    internal static ErrorRecord ServiceNotRegisteredWithAU(WindowsUpdateService service)
    {
        var exn = new InvalidOperationException(
            "The service is not registered with Automatic Updates."
        );
        SetCurrentStackTrace(exn);
        var err = new ErrorRecord(
            exn,
            "ServiceNotRegisteredWithAU",
            ErrorCategory.InvalidOperation,
            service
        )
        {
            ErrorDetails = new(
                $"The service '{service.Name}' ({service.ServiceID}) is not registered with Automatic Updates."
            )
        };
        return err;
    }

    internal static ErrorRecord ServiceRegisteredWithAU(WindowsUpdateService service)
    {
        var exn = new InvalidOperationException(
            "The service is already registered with Automatic Updates."
        );
        SetCurrentStackTrace(exn);
        var err = new ErrorRecord(
            exn,
            "ServiceRegisteredWithAU",
            ErrorCategory.InvalidOperation,
            service
        )
        {
            ErrorDetails = new(
                $"The service '{service.Name}' ({service.ServiceID}) is already registered with Automatic Updates."
            )
        };
        return err;
    }

    internal static ErrorRecord ServiceCannotRegisterWithAU(WindowsUpdateService service)
    {
        var exn = new InvalidOperationException(
            "The service cannot be registered with Automatic Updates."
        );
        SetCurrentStackTrace(exn);
        var err = new ErrorRecord(
            exn,
            "ServiceCannotRegisterWithAU",
            ErrorCategory.InvalidOperation,
            service
        )
        {
            ErrorDetails = new(
                $"The service '{service.Name}' ({service.ServiceID}) cannot be registered with Automatic Updates."
            )
        };
        return err;
    }

    internal static ErrorRecord AutomaticUpdatesEnabled()
    {
        var exn = new InvalidOperationException("Automatic Updates is already enabled.");
        SetCurrentStackTrace(exn);
        var err = new ErrorRecord(
            exn,
            "AutomaticUpdatesEnabled",
            ErrorCategory.InvalidOperation,
            null
        );
        return err;
    }

#if NET8_0_OR_GREATER
    [StackTraceHidden]
#endif
    private static Exception SetCurrentStackTrace(Exception e)
    {
#if NET8_0_OR_GREATER
        return ExceptionDispatchInfo.SetCurrentStackTrace(e);
#else
        return e;
#endif
    }

    public static ErrorRecord ErrorRecordForHResult(
        int hresult,
        object? targetObject,
        Exception? innerException
    )
    {
        switch (hresult)
        {
            case unchecked((int)0x80243FFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "There was a user interface error not covered by another  WU_E_AUCLIENT_*  error code.",
                        "WU_E_AUCLIENT_UNEXPECTED",
                        0x80243FFF,
                        innerException
                    ),
                    "WU_E_AUCLIENT_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024A000):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Automatic Updates was unable to service incoming requests.",
                        "WU_E_AU_NOSERVICE",
                        0x8024A000,
                        innerException
                    ),
                    "WU_E_AU_NOSERVICE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024A002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The old version of the Automatic Updates client has stopped because the WSUS server has been upgraded.",
                        "WU_E_AU_NONLEGACYSERVER",
                        0x8024A002,
                        innerException
                    ),
                    "WU_E_AU_NONLEGACYSERVER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024A003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The old version of the Automatic Updates client was disabled.",
                        "WU_E_AU_LEGACYCLIENTDISABLED",
                        0x8024A003,
                        innerException
                    ),
                    "WU_E_AU_LEGACYCLIENTDISABLED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024A004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Automatic Updates was unable to process incoming requests because it was paused.",
                        "WU_E_AU_PAUSED",
                        0x8024A004,
                        innerException
                    ),
                    "WU_E_AU_PAUSED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024A005):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "No unmanaged service is registered with  AU .",
                        "WU_E_AU_NO_REGISTERED_SERVICE",
                        0x8024A005,
                        innerException
                    ),
                    "WU_E_AU_NO_REGISTERED_SERVICE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024AFFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An Automatic Updates error not covered by another  WU_E_AU*  code.",
                        "WU_E_AU_UNEXPECTED",
                        0x8024AFFF,
                        innerException
                    ),
                    "WU_E_AU_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80243001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The results of download and installation couldn't be read from the registry due to an unrecognized data format version.",
                        "WU_E_INSTALLATION_RESULTS_UNKNOWN_VERSION",
                        0x80243001,
                        innerException
                    ),
                    "WU_E_INSTALLATION_RESULTS_UNKNOWN_VERSION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80243002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The results of download and installation couldn't be read from the registry due to an invalid data format.",
                        "WU_E_INSTALLATION_RESULTS_INVALID_DATA",
                        0x80243002,
                        innerException
                    ),
                    "WU_E_INSTALLATION_RESULTS_INVALID_DATA",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80243003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The results of download and installation aren't available; the operation may have failed to start.",
                        "WU_E_INSTALLATION_RESULTS_NOT_FOUND",
                        0x80243003,
                        innerException
                    ),
                    "WU_E_INSTALLATION_RESULTS_NOT_FOUND",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80243004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A failure occurred when trying to create an icon in the taskbar notification area.",
                        "WU_E_TRAYICON_FAILURE",
                        0x80243004,
                        innerException
                    ),
                    "WU_E_TRAYICON_FAILURE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80243FFD):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Unable to show UI when in non-UI mode; Windows Update client UI modules may not be installed.",
                        "WU_E_NON_UI_MODE",
                        0x80243FFD,
                        innerException
                    ),
                    "WU_E_NON_UI_MODE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80243FFE):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Unsupported version of Windows Update client UI exported functions.",
                        "WU_E_WUCLTUI_UNSUPPORTED_VERSION",
                        0x80243FFE,
                        innerException
                    ),
                    "WU_E_WUCLTUI_UNSUPPORTED_VERSION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024043D):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The requested service property isn't available.",
                        "WU_E_SERVICEPROP_NOTAVAIL",
                        0x8024043D,
                        innerException
                    ),
                    "WU_E_SERVICEPROP_NOTAVAIL",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80249001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Parsing of the rule file failed.",
                        "WU_E_INVENTORY_PARSEFAILED",
                        0x80249001,
                        innerException
                    ),
                    "WU_E_INVENTORY_PARSEFAILED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80249002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Failed to get the requested inventory type from the server.",
                        "WU_E_INVENTORY_GET_INVENTORY_TYPE_FAILED",
                        0x80249002,
                        innerException
                    ),
                    "WU_E_INVENTORY_GET_INVENTORY_TYPE_FAILED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80249003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Failed to upload inventory result to the server.",
                        "WU_E_INVENTORY_RESULT_UPLOAD_FAILED",
                        0x80249003,
                        innerException
                    ),
                    "WU_E_INVENTORY_RESULT_UPLOAD_FAILED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80249004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "There was an inventory error not covered by another error code.",
                        "WU_E_INVENTORY_UNEXPECTED",
                        0x80249004,
                        innerException
                    ),
                    "WU_E_INVENTORY_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80249005):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A WMI error occurred when enumerating the instances for a particular class.",
                        "WU_E_INVENTORY_WMI_ERROR",
                        0x80249005,
                        innerException
                    ),
                    "WU_E_INVENTORY_WMI_ERROR",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024E001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An expression evaluator operation couldn't be completed because an expression was unrecognized.",
                        "WU_E_EE_UNKNOWN_EXPRESSION",
                        0x8024E001,
                        innerException
                    ),
                    "WU_E_EE_UNKNOWN_EXPRESSION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024E002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An expression evaluator operation couldn't be completed because an expression was invalid.",
                        "WU_E_EE_INVALID_EXPRESSION",
                        0x8024E002,
                        innerException
                    ),
                    "WU_E_EE_INVALID_EXPRESSION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024E003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An expression evaluator operation couldn't be completed because an expression contains an incorrect number of metadata nodes.",
                        "WU_E_EE_MISSING_METADATA",
                        0x8024E003,
                        innerException
                    ),
                    "WU_E_EE_MISSING_METADATA",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024E004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An expression evaluator operation couldn't be completed because the version of the serialized expression data is invalid.",
                        "WU_E_EE_INVALID_VERSION",
                        0x8024E004,
                        innerException
                    ),
                    "WU_E_EE_INVALID_VERSION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024E005):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The expression evaluator couldn't be initialized.",
                        "WU_E_EE_NOT_INITIALIZED",
                        0x8024E005,
                        innerException
                    ),
                    "WU_E_EE_NOT_INITIALIZED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024E006):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An expression evaluator operation couldn't be completed because there was an invalid attribute.",
                        "WU_E_EE_INVALID_ATTRIBUTEDATA",
                        0x8024E006,
                        innerException
                    ),
                    "WU_E_EE_INVALID_ATTRIBUTEDATA",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024E007):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An expression evaluator operation couldn't be completed because the cluster state of the computer couldn't be determined.",
                        "WU_E_EE_CLUSTER_ERROR",
                        0x8024E007,
                        innerException
                    ),
                    "WU_E_EE_CLUSTER_ERROR",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024EFFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "There was an expression evaluator error not covered by another  WU_E_EE_*  error code.",
                        "WU_E_EE_UNEXPECTED",
                        0x8024EFFF,
                        innerException
                    ),
                    "WU_E_EE_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80247001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An operation couldn't be completed because the scan package was invalid.",
                        "WU_E_OL_INVALID_SCANFILE",
                        0x80247001,
                        innerException
                    ),
                    "WU_E_OL_INVALID_SCANFILE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80247002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An operation couldn't be completed because the scan package requires a greater version of the Windows Update Agent.",
                        "WU_E_OL_NEWCLIENT_REQUIRED",
                        0x80247002,
                        innerException
                    ),
                    "WU_E_OL_NEWCLIENT_REQUIRED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80247FFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Search using the scan package failed.",
                        "WU_E_OL_UNEXPECTED",
                        0x80247FFF,
                        innerException
                    ),
                    "WU_E_OL_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024F001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The event cache file was defective.",
                        "WU_E_REPORTER_EVENTCACHECORRUPT",
                        0x8024F001,
                        innerException
                    ),
                    "WU_E_REPORTER_EVENTCACHECORRUPT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024F002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The XML in the event namespace descriptor couldn't be parsed.",
                        "WU_E_REPORTER_EVENTNAMESPACEPARSEFAILED",
                        0x8024F002,
                        innerException
                    ),
                    "WU_E_REPORTER_EVENTNAMESPACEPARSEFAILED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024F003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The XML in the event namespace descriptor couldn't be parsed.",
                        "WU_E_INVALID_EVENT",
                        0x8024F003,
                        innerException
                    ),
                    "WU_E_INVALID_EVENT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024F004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The server rejected an event because the server was too busy.",
                        "WU_E_SERVER_BUSY",
                        0x8024F004,
                        innerException
                    ),
                    "WU_E_SERVER_BUSY",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024FFFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "There was a reporter error not covered by another error code.",
                        "WU_E_REPORTER_UNEXPECTED",
                        0x8024FFFF,
                        innerException
                    ),
                    "WU_E_REPORTER_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80245001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The redirector XML document couldn't be loaded into the DOM class.",
                        "WU_E_REDIRECTOR_LOAD_XML",
                        0x80245001,
                        innerException
                    ),
                    "WU_E_REDIRECTOR_LOAD_XML",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80245002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The redirector XML document is missing some required information.",
                        "WU_E_REDIRECTOR_S_FALSE",
                        0x80245002,
                        innerException
                    ),
                    "WU_E_REDIRECTOR_S_FALSE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80245003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The redirectorId in the downloaded redirector cab is less than in the cached cab.",
                        "WU_E_REDIRECTOR_ID_SMALLER",
                        0x80245003,
                        innerException
                    ),
                    "WU_E_REDIRECTOR_ID_SMALLER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80245FFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The redirector failed for reasons not covered by another  WU_E_REDIRECTOR_*  error code.",
                        "WU_E_REDIRECTOR_UNEXPECTED",
                        0x80245FFF,
                        innerException
                    ),
                    "WU_E_REDIRECTOR_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244000):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "WU_E_PT_SOAPCLIENT_*  error codes map to the  SOAPCLIENT_ERROR  enum of the ATL Server Library.",
                        "WU_E_PT_SOAPCLIENT_BASE",
                        0x80244000,
                        innerException
                    ),
                    "WU_E_PT_SOAPCLIENT_BASE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAPCLIENT_INITIALIZE_ERROR  - initialization of the  SOAP  client failed possibly because of an MSXML installation failure.",
                        "WU_E_PT_SOAPCLIENT_INITIALIZE",
                        0x80244001,
                        innerException
                    ),
                    "WU_E_PT_SOAPCLIENT_INITIALIZE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAPCLIENT_OUTOFMEMORY  -  SOAP  client failed because it ran out of memory.",
                        "WU_E_PT_SOAPCLIENT_OUTOFMEMORY",
                        0x80244002,
                        innerException
                    ),
                    "WU_E_PT_SOAPCLIENT_OUTOFMEMORY",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAPCLIENT_GENERATE_ERROR  -  SOAP  client failed to generate the request.",
                        "WU_E_PT_SOAPCLIENT_GENERATE",
                        0x80244003,
                        innerException
                    ),
                    "WU_E_PT_SOAPCLIENT_GENERATE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAPCLIENT_CONNECT_ERROR  -  SOAP  client failed to connect to the server.",
                        "WU_E_PT_SOAPCLIENT_CONNECT",
                        0x80244004,
                        innerException
                    ),
                    "WU_E_PT_SOAPCLIENT_CONNECT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244005):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAPCLIENT_SEND_ERROR  -  SOAP  client failed to send a message for reasons of  WU_E_WINHTTP_*  error codes.",
                        "WU_E_PT_SOAPCLIENT_SEND",
                        0x80244005,
                        innerException
                    ),
                    "WU_E_PT_SOAPCLIENT_SEND",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244006):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAPCLIENT_SERVER_ERROR  -  SOAP  client failed because there was a server error.",
                        "WU_E_PT_SOAPCLIENT_SERVER",
                        0x80244006,
                        innerException
                    ),
                    "WU_E_PT_SOAPCLIENT_SERVER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244007):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAPCLIENT_SOAPFAULT  -  SOAP  client failed because there was a SOAP fault for reasons of  WU_E_PT_SOAP_*  error codes.",
                        "WU_E_PT_SOAPCLIENT_SOAPFAULT",
                        0x80244007,
                        innerException
                    ),
                    "WU_E_PT_SOAPCLIENT_SOAPFAULT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244008):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAPCLIENT_PARSEFAULT_ERROR  -  SOAP  client failed to parse a  SOAP  fault.",
                        "WU_E_PT_SOAPCLIENT_PARSEFAULT",
                        0x80244008,
                        innerException
                    ),
                    "WU_E_PT_SOAPCLIENT_PARSEFAULT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244009):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAPCLIENT_READ_ERROR  -  SOAP  client failed while reading the response from the server.",
                        "WU_E_PT_SOAPCLIENT_READ",
                        0x80244009,
                        innerException
                    ),
                    "WU_E_PT_SOAPCLIENT_READ",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024400A):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAPCLIENT_PARSE_ERROR  -  SOAP  client failed to parse the response from the server.",
                        "WU_E_PT_SOAPCLIENT_PARSE",
                        0x8024400A,
                        innerException
                    ),
                    "WU_E_PT_SOAPCLIENT_PARSE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024400B):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAP_E_VERSION_MISMATCH  -  SOAP  client found an unrecognizable namespace for the  SOAP  envelope.",
                        "WU_E_PT_SOAP_VERSION",
                        0x8024400B,
                        innerException
                    ),
                    "WU_E_PT_SOAP_VERSION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024400C):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAP_E_MUST_UNDERSTAND  -  SOAP  client was unable to understand a header.",
                        "WU_E_PT_SOAP_MUST_UNDERSTAND",
                        0x8024400C,
                        innerException
                    ),
                    "WU_E_PT_SOAP_MUST_UNDERSTAND",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024400D):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAP_E_CLIENT  -  SOAP  client found the message was malformed; fix before resending.",
                        "WU_E_PT_SOAP_CLIENT",
                        0x8024400D,
                        innerException
                    ),
                    "WU_E_PT_SOAP_CLIENT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024400E):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as  SOAP_E_SERVER  - The  SOAP  message couldn't be processed due to a server error; resend later.",
                        "WU_E_PT_SOAP_SERVER",
                        0x8024400E,
                        innerException
                    ),
                    "WU_E_PT_SOAP_SERVER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024400F):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "There was an unspecified Windows Management Instrumentation (WMI) error.",
                        "WU_E_PT_WMI_ERROR",
                        0x8024400F,
                        innerException
                    ),
                    "WU_E_PT_WMI_ERROR",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244010):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The number of round trips to the server exceeded the maximum limit.",
                        "WU_E_PT_EXCEEDED_MAX_SERVER_TRIPS",
                        0x80244010,
                        innerException
                    ),
                    "WU_E_PT_EXCEEDED_MAX_SERVER_TRIPS",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244011):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "WUServer policy value is missing in the registry.",
                        "WU_E_PT_SUS_SERVER_NOT_SET",
                        0x80244011,
                        innerException
                    ),
                    "WU_E_PT_SUS_SERVER_NOT_SET",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244012):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Initialization failed because the object was already initialized.",
                        "WU_E_PT_DOUBLE_INITIALIZATION",
                        0x80244012,
                        innerException
                    ),
                    "WU_E_PT_DOUBLE_INITIALIZATION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244013):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The computer name couldn't be determined.",
                        "WU_E_PT_INVALID_COMPUTER_NAME",
                        0x80244013,
                        innerException
                    ),
                    "WU_E_PT_INVALID_COMPUTER_NAME",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244015):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The reply from the server indicates that the server was changed or the cookie was invalid; refresh the state of the internal cache and retry.",
                        "WU_E_PT_REFRESH_CACHE_REQUIRED",
                        0x80244015,
                        innerException
                    ),
                    "WU_E_PT_REFRESH_CACHE_REQUIRED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244016):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 400 - the server couldn't process the request due to invalid syntax.",
                        "WU_E_PT_HTTP_STATUS_BAD_REQUEST",
                        0x80244016,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_BAD_REQUEST",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244017):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 401 - the requested resource requires user authentication.",
                        "WU_E_PT_HTTP_STATUS_DENIED",
                        0x80244017,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_DENIED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244018):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 403 - server understood the request but declined to fulfill it.",
                        "WU_E_PT_HTTP_STATUS_FORBIDDEN",
                        0x80244018,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_FORBIDDEN",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244019):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 404 - the server can't find the requested URI (Uniform Resource Identifier).",
                        "WU_E_PT_HTTP_STATUS_NOT_FOUND",
                        0x80244019,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_NOT_FOUND",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024401A):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 405 - the HTTP method isn't allowed.",
                        "WU_E_PT_HTTP_STATUS_BAD_METHOD",
                        0x8024401A,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_BAD_METHOD",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024401B):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 407 - proxy authentication is required.",
                        "WU_E_PT_HTTP_STATUS_PROXY_AUTH_REQ",
                        0x8024401B,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_PROXY_AUTH_REQ",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024401C):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 408 - the server timed out waiting for the request.",
                        "WU_E_PT_HTTP_STATUS_REQUEST_TIMEOUT",
                        0x8024401C,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_REQUEST_TIMEOUT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024401D):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 409 - the request wasn't completed due to a conflict with the current state of the resource.",
                        "WU_E_PT_HTTP_STATUS_CONFLICT",
                        0x8024401D,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_CONFLICT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024401E):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 410 - requested resource is no longer available at the server.",
                        "WU_E_PT_HTTP_STATUS_GONE",
                        0x8024401E,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_GONE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024401F):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 500 - an error internal to the server prevented fulfilling the request.",
                        "WU_E_PT_HTTP_STATUS_SERVER_ERROR",
                        0x8024401F,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_SERVER_ERROR",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244020):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 500 - server doesn't support the functionality required to fulfill the request.",
                        "WU_E_PT_HTTP_STATUS_NOT_SUPPORTED",
                        0x80244020,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_NOT_SUPPORTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244021):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 502 - the server while acting as a gateway or a proxy received an invalid response from the upstream server it accessed in attempting to fulfill the request.",
                        "WU_E_PT_HTTP_STATUS_BAD_GATEWAY",
                        0x80244021,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_BAD_GATEWAY",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244022):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 503 - the service is temporarily overloaded.",
                        "WU_E_PT_HTTP_STATUS_SERVICE_UNAVAIL",
                        0x80244022,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_SERVICE_UNAVAIL",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244023):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 503 - the request was timed out waiting for a gateway.",
                        "WU_E_PT_HTTP_STATUS_GATEWAY_TIMEOUT",
                        0x80244023,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_GATEWAY_TIMEOUT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244024):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as HTTP status 505 - the server doesn't support the HTTP protocol version used for the request.",
                        "WU_E_PT_HTTP_STATUS_VERSION_NOT_SUP",
                        0x80244024,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_VERSION_NOT_SUP",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244025):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation failed due to a changed file location; refresh internal state and resend.",
                        "WU_E_PT_FILE_LOCATIONS_CHANGED",
                        0x80244025,
                        innerException
                    ),
                    "WU_E_PT_FILE_LOCATIONS_CHANGED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244026):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation failed because Windows Update Agent doesn't support registration with a non-WSUS server.",
                        "WU_E_PT_REGISTRATION_NOT_SUPPORTED",
                        0x80244026,
                        innerException
                    ),
                    "WU_E_PT_REGISTRATION_NOT_SUPPORTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244027):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The server returned an empty authentication information list.",
                        "WU_E_PT_NO_AUTH_PLUGINS_REQUESTED",
                        0x80244027,
                        innerException
                    ),
                    "WU_E_PT_NO_AUTH_PLUGINS_REQUESTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244028):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent was unable to create any valid authentication cookies.",
                        "WU_E_PT_NO_AUTH_COOKIES_CREATED",
                        0x80244028,
                        innerException
                    ),
                    "WU_E_PT_NO_AUTH_COOKIES_CREATED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244029):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A configuration property value was wrong.",
                        "WU_E_PT_INVALID_CONFIG_PROP",
                        0x80244029,
                        innerException
                    ),
                    "WU_E_PT_INVALID_CONFIG_PROP",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024402A):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A configuration property value was missing.",
                        "WU_E_PT_CONFIG_PROP_MISSING",
                        0x8024402A,
                        innerException
                    ),
                    "WU_E_PT_CONFIG_PROP_MISSING",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024402B):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The HTTP request couldn't be completed and the reason didn't correspond to any of the  WU_E_PT_HTTP_*  error codes.",
                        "WU_E_PT_HTTP_STATUS_NOT_MAPPED",
                        0x8024402B,
                        innerException
                    ),
                    "WU_E_PT_HTTP_STATUS_NOT_MAPPED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024402C):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Same as ERROR_WINHTTP_NAME_NOT_RESOLVED - the proxy server or target server name can't be resolved.",
                        "WU_E_PT_WINHTTP_NAME_NOT_RESOLVED",
                        0x8024402C,
                        innerException
                    ),
                    "WU_E_PT_WINHTTP_NAME_NOT_RESOLVED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024402F):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "External cab file processing completed with some errors.",
                        "WU_E_PT_ECP_SUCCEEDED_WITH_ERRORS",
                        0x8024402F,
                        innerException
                    ),
                    "WU_E_PT_ECP_SUCCEEDED_WITH_ERRORS",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244030):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The external cab processor initialization didn't complete.",
                        "WU_E_PT_ECP_INIT_FAILED",
                        0x80244030,
                        innerException
                    ),
                    "WU_E_PT_ECP_INIT_FAILED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244031):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The format of a metadata file was invalid.",
                        "WU_E_PT_ECP_INVALID_FILE_FORMAT",
                        0x80244031,
                        innerException
                    ),
                    "WU_E_PT_ECP_INVALID_FILE_FORMAT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244032):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "External cab processor found invalid metadata.",
                        "WU_E_PT_ECP_INVALID_METADATA",
                        0x80244032,
                        innerException
                    ),
                    "WU_E_PT_ECP_INVALID_METADATA",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244033):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The file digest couldn't be extracted from an external cab file.",
                        "WU_E_PT_ECP_FAILURE_TO_EXTRACT_DIGEST",
                        0x80244033,
                        innerException
                    ),
                    "WU_E_PT_ECP_FAILURE_TO_EXTRACT_DIGEST",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244034):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An external cab file couldn't be decompressed.",
                        "WU_E_PT_ECP_FAILURE_TO_DECOMPRESS_CAB_FILE",
                        0x80244034,
                        innerException
                    ),
                    "WU_E_PT_ECP_FAILURE_TO_DECOMPRESS_CAB_FILE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244035):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "External cab processor was unable to get file locations.",
                        "WU_E_PT_ECP_FILE_LOCATION_ERROR",
                        0x80244035,
                        innerException
                    ),
                    "WU_E_PT_ECP_FILE_LOCATION_ERROR",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80244FFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A communication error not covered by another  WU_E_PT_*  error code.",
                        "WU_E_PT_UNEXPECTED",
                        0x80244FFF,
                        innerException
                    ),
                    "WU_E_PT_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024502D):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent failed to download a redirector cabinet file with a new redirectorId value from the server during the recovery.",
                        "WU_E_PT_SAME_REDIR_ID",
                        0x8024502D,
                        innerException
                    ),
                    "WU_E_PT_SAME_REDIR_ID",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024502E):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A redirector recovery action didn't complete because the server is managed.",
                        "WU_E_PT_NO_MANAGED_RECOVER",
                        0x8024502E,
                        innerException
                    ),
                    "WU_E_PT_NO_MANAGED_RECOVER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80246001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A download manager operation couldn't be completed because the requested file doesn't have a URL.",
                        "WU_E_DM_URLNOTAVAILABLE",
                        0x80246001,
                        innerException
                    ),
                    "WU_E_DM_URLNOTAVAILABLE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80246002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A download manager operation couldn't be completed because the file digest wasn't recognized.",
                        "WU_E_DM_INCORRECTFILEHASH",
                        0x80246002,
                        innerException
                    ),
                    "WU_E_DM_INCORRECTFILEHASH",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80246003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A download manager operation couldn't be completed because the file metadata requested an unrecognized hash algorithm.",
                        "WU_E_DM_UNKNOWNALGORITHM",
                        0x80246003,
                        innerException
                    ),
                    "WU_E_DM_UNKNOWNALGORITHM",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80246004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An operation couldn't be completed because a download request is required from the download handler.",
                        "WU_E_DM_NEEDDOWNLOADREQUEST",
                        0x80246004,
                        innerException
                    ),
                    "WU_E_DM_NEEDDOWNLOADREQUEST",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80246005):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A download manager operation couldn't be completed because the network connection was unavailable.",
                        "WU_E_DM_NONETWORK",
                        0x80246005,
                        innerException
                    ),
                    "WU_E_DM_NONETWORK",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80246006):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A download manager operation couldn't be completed because the version of Background Intelligent Transfer Service (BITS) is incompatible.",
                        "WU_E_DM_WRONGBITSVERSION",
                        0x80246006,
                        innerException
                    ),
                    "WU_E_DM_WRONGBITSVERSION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80246007):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update hasn't been downloaded.",
                        "WU_E_DM_NOTDOWNLOADED",
                        0x80246007,
                        innerException
                    ),
                    "WU_E_DM_NOTDOWNLOADED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80246008):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A download manager operation failed because the download manager was unable to connect the Background Intelligent Transfer Service (BITS).",
                        "WU_E_DM_FAILTOCONNECTTOBITS",
                        0x80246008,
                        innerException
                    ),
                    "WU_E_DM_FAILTOCONNECTTOBITS",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80246009):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A download manager operation failed because there was an unspecified Background Intelligent Transfer Service (BITS) transfer error.",
                        "WU_E_DM_BITSTRANSFERERROR",
                        0x80246009,
                        innerException
                    ),
                    "WU_E_DM_BITSTRANSFERERROR",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024600A):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A download must be restarted because the location of the source of the download has changed.",
                        "WU_E_DM_DOWNLOADLOCATIONCHANGED",
                        0x8024600A,
                        innerException
                    ),
                    "WU_E_DM_DOWNLOADLOCATIONCHANGED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024600B):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A download must be restarted because the update content changed in a new revision.",
                        "WU_E_DM_CONTENTCHANGED",
                        0x8024600B,
                        innerException
                    ),
                    "WU_E_DM_CONTENTCHANGED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80246FFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "There was a download manager error not covered by another  WU_E_DM_*  error code.",
                        "WU_E_DM_UNEXPECTED",
                        0x80246FFF,
                        innerException
                    ),
                    "WU_E_DM_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242000):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A request for a remote update handler couldn't be completed because no remote process is available.",
                        "WU_E_UH_REMOTEUNAVAILABLE",
                        0x80242000,
                        innerException
                    ),
                    "WU_E_UH_REMOTEUNAVAILABLE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A request for a remote update handler couldn't be completed because the handler is local only.",
                        "WU_E_UH_LOCALONLY",
                        0x80242001,
                        innerException
                    ),
                    "WU_E_UH_LOCALONLY",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A request for an update handler couldn't be completed because the handler couldn't be recognized.",
                        "WU_E_UH_UNKNOWNHANDLER",
                        0x80242002,
                        innerException
                    ),
                    "WU_E_UH_UNKNOWNHANDLER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A remote update handler couldn't be created because one already exists.",
                        "WU_E_UH_REMOTEALREADYACTIVE",
                        0x80242003,
                        innerException
                    ),
                    "WU_E_UH_REMOTEALREADYACTIVE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A request for the handler to install (uninstall) an update couldn't be completed because the update doesn't support install (uninstall).",
                        "WU_E_UH_DOESNOTSUPPORTACTION",
                        0x80242004,
                        innerException
                    ),
                    "WU_E_UH_DOESNOTSUPPORTACTION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242005):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An operation didn't complete because the wrong handler was specified.",
                        "WU_E_UH_WRONGHANDLER",
                        0x80242005,
                        innerException
                    ),
                    "WU_E_UH_WRONGHANDLER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242006):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A handler operation couldn't be completed because the update contains invalid metadata.",
                        "WU_E_UH_INVALIDMETADATA",
                        0x80242006,
                        innerException
                    ),
                    "WU_E_UH_INVALIDMETADATA",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242007):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An operation couldn't be completed because the installer exceeded the time limit.",
                        "WU_E_UH_INSTALLERHUNG",
                        0x80242007,
                        innerException
                    ),
                    "WU_E_UH_INSTALLERHUNG",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242008):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An operation being done by the update handler was canceled.",
                        "WU_E_UH_OPERATIONCANCELLED",
                        0x80242008,
                        innerException
                    ),
                    "WU_E_UH_OPERATIONCANCELLED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242009):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An operation couldn't be completed because the handler-specific metadata is invalid.",
                        "WU_E_UH_BADHANDLERXML",
                        0x80242009,
                        innerException
                    ),
                    "WU_E_UH_BADHANDLERXML",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024200A):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A request to the handler to install an update couldn't be completed because the update requires user input.",
                        "WU_E_UH_CANREQUIREINPUT",
                        0x8024200A,
                        innerException
                    ),
                    "WU_E_UH_CANREQUIREINPUT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024200B):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The installer failed to install (uninstall) one or more updates.",
                        "WU_E_UH_INSTALLERFAILURE",
                        0x8024200B,
                        innerException
                    ),
                    "WU_E_UH_INSTALLERFAILURE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024200C):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update handler should download self-contained content rather than delta-compressed content for the update.",
                        "WU_E_UH_FALLBACKTOSELFCONTAINED",
                        0x8024200C,
                        innerException
                    ),
                    "WU_E_UH_FALLBACKTOSELFCONTAINED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024200D):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update handler didn't install the update because it needs to be downloaded again.",
                        "WU_E_UH_NEEDANOTHERDOWNLOAD",
                        0x8024200D,
                        innerException
                    ),
                    "WU_E_UH_NEEDANOTHERDOWNLOAD",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024200E):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update handler failed to send notification of the status of the install (uninstall) operation.",
                        "WU_E_UH_NOTIFYFAILURE",
                        0x8024200E,
                        innerException
                    ),
                    "WU_E_UH_NOTIFYFAILURE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024200F):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The file names contained in the update metadata and in the update package are inconsistent.",
                        "WU_E_UH_INCONSISTENT_FILE_NAMES",
                        0x8024200F,
                        innerException
                    ),
                    "WU_E_UH_INCONSISTENT_FILE_NAMES",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242010):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update handler failed to fall back to the self-contained content.",
                        "WU_E_UH_FALLBACKERROR",
                        0x80242010,
                        innerException
                    ),
                    "WU_E_UH_FALLBACKERROR",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242011):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update handler has exceeded the maximum number of download requests.",
                        "WU_E_UH_TOOMANYDOWNLOADREQUESTS",
                        0x80242011,
                        innerException
                    ),
                    "WU_E_UH_TOOMANYDOWNLOADREQUESTS",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242012):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update handler has received an unexpected response from CBS.",
                        "WU_E_UH_UNEXPECTEDCBSRESPONSE",
                        0x80242012,
                        innerException
                    ),
                    "WU_E_UH_UNEXPECTEDCBSRESPONSE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242013):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update metadata contains an invalid CBS package identifier.",
                        "WU_E_UH_BADCBSPACKAGEID",
                        0x80242013,
                        innerException
                    ),
                    "WU_E_UH_BADCBSPACKAGEID",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242014):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The post-reboot operation for the update is still in progress.",
                        "WU_E_UH_POSTREBOOTSTILLPENDING",
                        0x80242014,
                        innerException
                    ),
                    "WU_E_UH_POSTREBOOTSTILLPENDING",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242015):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The result of the post-reboot operation for the update couldn't be determined.",
                        "WU_E_UH_POSTREBOOTRESULTUNKNOWN",
                        0x80242015,
                        innerException
                    ),
                    "WU_E_UH_POSTREBOOTRESULTUNKNOWN",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242016):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The state of the update after its post-reboot operation has completed is unexpected.",
                        "WU_E_UH_POSTREBOOTUNEXPECTEDSTATE",
                        0x80242016,
                        innerException
                    ),
                    "WU_E_UH_POSTREBOOTUNEXPECTEDSTATE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242017):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The OS servicing stack must be updated before this update is downloaded or installed.",
                        "WU_E_UH_NEW_SERVICING_STACK_REQUIRED",
                        0x80242017,
                        innerException
                    ),
                    "WU_E_UH_NEW_SERVICING_STACK_REQUIRED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80242FFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An update handler error not covered by another  WU_E_UH_*  code.",
                        "WU_E_UH_UNEXPECTED",
                        0x80242FFF,
                        innerException
                    ),
                    "WU_E_UH_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248000):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An operation failed because Windows Update Agent is shutting down.",
                        "WU_E_DS_SHUTDOWN",
                        0x80248000,
                        innerException
                    ),
                    "WU_E_DS_SHUTDOWN",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An operation failed because the data store was in use.",
                        "WU_E_DS_INUSE",
                        0x80248001,
                        innerException
                    ),
                    "WU_E_DS_INUSE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The current and expected states of the data store don't match.",
                        "WU_E_DS_INVALID",
                        0x80248002,
                        innerException
                    ),
                    "WU_E_DS_INVALID",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The data store is missing a table.",
                        "WU_E_DS_TABLEMISSING",
                        0x80248003,
                        innerException
                    ),
                    "WU_E_DS_TABLEMISSING",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The data store contains a table with unexpected columns.",
                        "WU_E_DS_TABLEINCORRECT",
                        0x80248004,
                        innerException
                    ),
                    "WU_E_DS_TABLEINCORRECT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248005):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A table couldn't be opened because the table isn't in the data store.",
                        "WU_E_DS_INVALIDTABLENAME",
                        0x80248005,
                        innerException
                    ),
                    "WU_E_DS_INVALIDTABLENAME",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248006):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The current and expected versions of the data store don't match.",
                        "WU_E_DS_BADVERSION",
                        0x80248006,
                        innerException
                    ),
                    "WU_E_DS_BADVERSION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248007):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The information requested isn't in the data store.",
                        "WU_E_DS_NODATA",
                        0x80248007,
                        innerException
                    ),
                    "WU_E_DS_NODATA",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248008):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The data store is missing required information or has a NULL in a table column that requires a non-null value.",
                        "WU_E_DS_MISSINGDATA",
                        0x80248008,
                        innerException
                    ),
                    "WU_E_DS_MISSINGDATA",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248009):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The data store is missing required information or has a reference to missing license terms file localized property or linked row.",
                        "WU_E_DS_MISSINGREF",
                        0x80248009,
                        innerException
                    ),
                    "WU_E_DS_MISSINGREF",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024800A):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update wasn't processed because its update handler couldn't be recognized.",
                        "WU_E_DS_UNKNOWNHANDLER",
                        0x8024800A,
                        innerException
                    ),
                    "WU_E_DS_UNKNOWNHANDLER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024800B):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update wasn't deleted because it's still referenced by one or more services.",
                        "WU_E_DS_CANTDELETE",
                        0x8024800B,
                        innerException
                    ),
                    "WU_E_DS_CANTDELETE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024800C):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The data store section couldn't be locked within the allotted time.",
                        "WU_E_DS_LOCKTIMEOUTEXPIRED",
                        0x8024800C,
                        innerException
                    ),
                    "WU_E_DS_LOCKTIMEOUTEXPIRED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024800D):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The category wasn't added because it contains no parent categories and isn't a top-level category itself.",
                        "WU_E_DS_NOCATEGORIES",
                        0x8024800D,
                        innerException
                    ),
                    "WU_E_DS_NOCATEGORIES",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024800E):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The row wasn't added because an existing row has the same primary key.",
                        "WU_E_DS_ROWEXISTS",
                        0x8024800E,
                        innerException
                    ),
                    "WU_E_DS_ROWEXISTS",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024800F):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The data store couldn't be initialized because it was locked by another process.",
                        "WU_E_DS_STOREFILELOCKED",
                        0x8024800F,
                        innerException
                    ),
                    "WU_E_DS_STOREFILELOCKED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248010):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The data store isn't allowed to be registered with COM in the current process.",
                        "WU_E_DS_CANNOTREGISTER",
                        0x80248010,
                        innerException
                    ),
                    "WU_E_DS_CANNOTREGISTER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248011):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Couldn't create a data store object in another process.",
                        "WU_E_DS_UNABLETOSTART",
                        0x80248011,
                        innerException
                    ),
                    "WU_E_DS_UNABLETOSTART",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248013):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The server sent the same update to the client with two different revision IDs.",
                        "WU_E_DS_DUPLICATEUPDATEID",
                        0x80248013,
                        innerException
                    ),
                    "WU_E_DS_DUPLICATEUPDATEID",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248014):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An operation didn't complete because the service isn't in the data store.",
                        "WU_E_DS_UNKNOWNSERVICE",
                        0x80248014,
                        innerException
                    ),
                    "WU_E_DS_UNKNOWNSERVICE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248015):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An operation didn't complete because the registration of the service has expired.",
                        "WU_E_DS_SERVICEEXPIRED",
                        0x80248015,
                        innerException
                    ),
                    "WU_E_DS_SERVICEEXPIRED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248016):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A request to hide an update was declined because it's a mandatory update or because it was deployed with a deadline.",
                        "WU_E_DS_DECLINENOTALLOWED",
                        0x80248016,
                        innerException
                    ),
                    "WU_E_DS_DECLINENOTALLOWED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248017):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A table wasn't closed because it isn't associated with the session.",
                        "WU_E_DS_TABLESESSIONMISMATCH",
                        0x80248017,
                        innerException
                    ),
                    "WU_E_DS_TABLESESSIONMISMATCH",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248018):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A table wasn't closed because it isn't associated with the session.",
                        "WU_E_DS_SESSIONLOCKMISMATCH",
                        0x80248018,
                        innerException
                    ),
                    "WU_E_DS_SESSIONLOCKMISMATCH",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248019):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A request to remove the Windows Update service or to unregister it with Automatic Updates was declined because it's a built-in service and/or Automatic Updates can't fall back to another service.",
                        "WU_E_DS_NEEDWINDOWSSERVICE",
                        0x80248019,
                        innerException
                    ),
                    "WU_E_DS_NEEDWINDOWSSERVICE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024801A):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A request was declined because the operation isn't allowed.",
                        "WU_E_DS_INVALIDOPERATION",
                        0x8024801A,
                        innerException
                    ),
                    "WU_E_DS_INVALIDOPERATION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024801B):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The schema of the current data store and the schema of a table in a backup XML document don't match.",
                        "WU_E_DS_SCHEMAMISMATCH",
                        0x8024801B,
                        innerException
                    ),
                    "WU_E_DS_SCHEMAMISMATCH",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024801C):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The data store requires a session reset; release the session and retry with a new session.",
                        "WU_E_DS_RESETREQUIRED",
                        0x8024801C,
                        innerException
                    ),
                    "WU_E_DS_RESETREQUIRED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024801D):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A data store operation didn't complete because it was requested with an impersonated identity.",
                        "WU_E_DS_IMPERSONATED",
                        0x8024801D,
                        innerException
                    ),
                    "WU_E_DS_IMPERSONATED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80248FFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A data store error not covered by another  WU_E_DS_*  code.",
                        "WU_E_DS_UNEXPECTED",
                        0x80248FFF,
                        innerException
                    ),
                    "WU_E_DS_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024C001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A driver was skipped.",
                        "WU_E_DRV_PRUNED",
                        0x8024C001,
                        innerException
                    ),
                    "WU_E_DRV_PRUNED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024C002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A property for the driver couldn't be found. It may not conform with required specifications.",
                        "WU_E_DRV_NOPROP_OR_LEGACY",
                        0x8024C002,
                        innerException
                    ),
                    "WU_E_DRV_NOPROP_OR_LEGACY",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024C003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The registry type read for the driver doesn't match the expected type.",
                        "WU_E_DRV_REG_MISMATCH",
                        0x8024C003,
                        innerException
                    ),
                    "WU_E_DRV_REG_MISMATCH",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024C004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The driver update is missing metadata.",
                        "WU_E_DRV_NO_METADATA",
                        0x8024C004,
                        innerException
                    ),
                    "WU_E_DRV_NO_METADATA",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024C005):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The driver update is missing a required attribute.",
                        "WU_E_DRV_MISSING_ATTRIBUTE",
                        0x8024C005,
                        innerException
                    ),
                    "WU_E_DRV_MISSING_ATTRIBUTE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024C006):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Driver synchronization failed.",
                        "WU_E_DRV_SYNC_FAILED",
                        0x8024C006,
                        innerException
                    ),
                    "WU_E_DRV_SYNC_FAILED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024C007):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Information required for the synchronization of applicable printers is missing.",
                        "WU_E_DRV_NO_PRINTER_CONTENT",
                        0x8024C007,
                        innerException
                    ),
                    "WU_E_DRV_NO_PRINTER_CONTENT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024CFFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A driver error not covered by another  WU_E_DRV_*  code.",
                        "WU_E_DRV_UNEXPECTED",
                        0x8024CFFF,
                        innerException
                    ),
                    "WU_E_DRV_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent was unable to provide the service.",
                        "WU_E_NO_SERVICE",
                        0x80240001,
                        innerException
                    ),
                    "WU_E_NO_SERVICE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The maximum capacity of the service was exceeded.",
                        "WU_E_MAX_CAPACITY_REACHED",
                        0x80240002,
                        innerException
                    ),
                    "WU_E_MAX_CAPACITY_REACHED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An ID can't be found.",
                        "WU_E_UNKNOWN_ID",
                        0x80240003,
                        innerException
                    ),
                    "WU_E_UNKNOWN_ID",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The object couldn't be initialized.",
                        "WU_E_NOT_INITIALIZED",
                        0x80240004,
                        innerException
                    ),
                    "WU_E_NOT_INITIALIZED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240005):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update handler requested a byte range overlapping a previously requested range.",
                        "WU_E_RANGEOVERLAP",
                        0x80240005,
                        innerException
                    ),
                    "WU_E_RANGEOVERLAP",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240006):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The requested number of byte ranges exceeds the maximum number (2 ^ 31 - 1).",
                        "WU_E_TOOMANYRANGES",
                        0x80240006,
                        innerException
                    ),
                    "WU_E_TOOMANYRANGES",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240007):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The index to a collection was invalid.",
                        "WU_E_INVALIDINDEX",
                        0x80240007,
                        innerException
                    ),
                    "WU_E_INVALIDINDEX",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240008):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The key for the item queried couldn't be found.",
                        "WU_E_ITEMNOTFOUND",
                        0x80240008,
                        innerException
                    ),
                    "WU_E_ITEMNOTFOUND",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240009):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Another conflicting operation was in progress. Some operations such as installation can't be performed twice simultaneously.",
                        "WU_E_OPERATIONINPROGRESS",
                        0x80240009,
                        innerException
                    ),
                    "WU_E_OPERATIONINPROGRESS",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024000A):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Cancellation of the operation wasn't allowed.",
                        "WU_E_COULDNOTCANCEL",
                        0x8024000A,
                        innerException
                    ),
                    "WU_E_COULDNOTCANCEL",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024000B):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation was canceled.",
                        "WU_E_CALL_CANCELLED",
                        0x8024000B,
                        innerException
                    ),
                    "WU_E_CALL_CANCELLED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024000C):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "No operation was required.",
                        "WU_E_NOOP",
                        0x8024000C,
                        innerException
                    ),
                    "WU_E_NOOP",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024000D):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't find required information in the update's XML data.",
                        "WU_E_XML_MISSINGDATA",
                        0x8024000D,
                        innerException
                    ),
                    "WU_E_XML_MISSINGDATA",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024000E):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent found invalid information in the update's XML data.",
                        "WU_E_XML_INVALID",
                        0x8024000E,
                        innerException
                    ),
                    "WU_E_XML_INVALID",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024000F):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Circular update relationships were detected in the metadata.",
                        "WU_E_CYCLE_DETECTED",
                        0x8024000F,
                        innerException
                    ),
                    "WU_E_CYCLE_DETECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240010):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Update relationships too deep to evaluate were evaluated.",
                        "WU_E_TOO_DEEP_RELATION",
                        0x80240010,
                        innerException
                    ),
                    "WU_E_TOO_DEEP_RELATION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240011):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An invalid update relationship was detected.",
                        "WU_E_INVALID_RELATIONSHIP",
                        0x80240011,
                        innerException
                    ),
                    "WU_E_INVALID_RELATIONSHIP",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240012):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An invalid registry value was read.",
                        "WU_E_REG_VALUE_INVALID",
                        0x80240012,
                        innerException
                    ),
                    "WU_E_REG_VALUE_INVALID",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240013):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation tried to add a duplicate item to a list.",
                        "WU_E_DUPLICATE_ITEM",
                        0x80240013,
                        innerException
                    ),
                    "WU_E_DUPLICATE_ITEM",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240016):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation tried to install while another installation was in progress or the system was pending a mandatory restart.",
                        "WU_E_INSTALL_NOT_ALLOWED",
                        0x80240016,
                        innerException
                    ),
                    "WU_E_INSTALL_NOT_ALLOWED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240017):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation wasn't performed because there are no applicable updates.",
                        "WU_E_NOT_APPLICABLE",
                        0x80240017,
                        innerException
                    ),
                    "WU_E_NOT_APPLICABLE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240018):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation failed because a required user token is missing.",
                        "WU_E_NO_USERTOKEN",
                        0x80240018,
                        innerException
                    ),
                    "WU_E_NO_USERTOKEN",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240019):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An exclusive update can't be installed with other updates at the same time.",
                        "WU_E_EXCLUSIVE_INSTALL_CONFLICT",
                        0x80240019,
                        innerException
                    ),
                    "WU_E_EXCLUSIVE_INSTALL_CONFLICT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024001A):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A policy value wasn't set.",
                        "WU_E_POLICY_NOT_SET",
                        0x8024001A,
                        innerException
                    ),
                    "WU_E_POLICY_NOT_SET",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024001B):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The operation couldn't be performed because the Windows Update Agent is self-updating.",
                        "WU_E_SELFUPDATE_IN_PROGRESS",
                        0x8024001B,
                        innerException
                    ),
                    "WU_E_SELFUPDATE_IN_PROGRESS",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024001D):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An update contains invalid metadata.",
                        "WU_E_INVALID_UPDATE",
                        0x8024001D,
                        innerException
                    ),
                    "WU_E_INVALID_UPDATE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024001E):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation didn't complete because the service or system was being shut down.",
                        "WU_E_SERVICE_STOP",
                        0x8024001E,
                        innerException
                    ),
                    "WU_E_SERVICE_STOP",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024001F):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation didn't complete because the network connection was unavailable.",
                        "WU_E_NO_CONNECTION",
                        0x8024001F,
                        innerException
                    ),
                    "WU_E_NO_CONNECTION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240020):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation didn't complete because there's no logged-on interactive user.",
                        "WU_E_NO_INTERACTIVE_USER",
                        0x80240020,
                        innerException
                    ),
                    "WU_E_NO_INTERACTIVE_USER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240021):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation didn't complete because it timed out.",
                        "WU_E_TIME_OUT",
                        0x80240021,
                        innerException
                    ),
                    "WU_E_TIME_OUT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240022):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation failed for all the updates.",
                        "WU_E_ALL_UPDATES_FAILED",
                        0x80240022,
                        innerException
                    ),
                    "WU_E_ALL_UPDATES_FAILED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240023):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The license terms for all updates were declined.",
                        "WU_E_EULAS_DECLINED",
                        0x80240023,
                        innerException
                    ),
                    "WU_E_EULAS_DECLINED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240024):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "There are no updates.",
                        "WU_E_NO_UPDATE",
                        0x80240024,
                        innerException
                    ),
                    "WU_E_NO_UPDATE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240025):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Group Policy settings prevented access to Windows Update.",
                        "WU_E_USER_ACCESS_DISABLED",
                        0x80240025,
                        innerException
                    ),
                    "WU_E_USER_ACCESS_DISABLED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240026):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The type of update is invalid.",
                        "WU_E_INVALID_UPDATE_TYPE",
                        0x80240026,
                        innerException
                    ),
                    "WU_E_INVALID_UPDATE_TYPE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240027):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The URL exceeded the maximum length.",
                        "WU_E_URL_TOO_LONG",
                        0x80240027,
                        innerException
                    ),
                    "WU_E_URL_TOO_LONG",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240028):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update couldn't be uninstalled because the request didn't originate from a WSUS server.",
                        "WU_E_UNINSTALL_NOT_ALLOWED",
                        0x80240028,
                        innerException
                    ),
                    "WU_E_UNINSTALL_NOT_ALLOWED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240029):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Search may have missed some updates before there's an unlicensed application on the system.",
                        "WU_E_INVALID_PRODUCT_LICENSE",
                        0x80240029,
                        innerException
                    ),
                    "WU_E_INVALID_PRODUCT_LICENSE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024002A):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A component required to detect applicable updates was missing.",
                        "WU_E_MISSING_HANDLER",
                        0x8024002A,
                        innerException
                    ),
                    "WU_E_MISSING_HANDLER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024002B):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An operation didn't complete because it requires a newer version of server.",
                        "WU_E_LEGACYSERVER",
                        0x8024002B,
                        innerException
                    ),
                    "WU_E_LEGACYSERVER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024002C):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A delta-compressed update couldn't be installed because it required the source.",
                        "WU_E_BIN_SOURCE_ABSENT",
                        0x8024002C,
                        innerException
                    ),
                    "WU_E_BIN_SOURCE_ABSENT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024002D):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A full-file update couldn't be installed because it required the source.",
                        "WU_E_SOURCE_ABSENT",
                        0x8024002D,
                        innerException
                    ),
                    "WU_E_SOURCE_ABSENT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024002E):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Access to an unmanaged server isn't allowed.",
                        "WU_E_WU_DISABLED",
                        0x8024002E,
                        innerException
                    ),
                    "WU_E_WU_DISABLED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024002F):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation didn't complete because the DisableWindowsUpdateAccess policy was set.",
                        "WU_E_CALL_CANCELLED_BY_POLICY",
                        0x8024002F,
                        innerException
                    ),
                    "WU_E_CALL_CANCELLED_BY_POLICY",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240030):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The format of the proxy list was invalid.",
                        "WU_E_INVALID_PROXY_SERVER",
                        0x80240030,
                        innerException
                    ),
                    "WU_E_INVALID_PROXY_SERVER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240031):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The file is in the wrong format.",
                        "WU_E_INVALID_FILE",
                        0x80240031,
                        innerException
                    ),
                    "WU_E_INVALID_FILE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240032):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The search criteria string was invalid.",
                        "WU_E_INVALID_CRITERIA",
                        0x80240032,
                        innerException
                    ),
                    "WU_E_INVALID_CRITERIA",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240033):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "License terms couldn't be downloaded.",
                        "WU_E_EULA_UNAVAILABLE",
                        0x80240033,
                        innerException
                    ),
                    "WU_E_EULA_UNAVAILABLE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240034):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Update failed to download.",
                        "WU_E_DOWNLOAD_FAILED",
                        0x80240034,
                        innerException
                    ),
                    "WU_E_DOWNLOAD_FAILED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240035):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update wasn't processed.",
                        "WU_E_UPDATE_NOT_PROCESSED",
                        0x80240035,
                        innerException
                    ),
                    "WU_E_UPDATE_NOT_PROCESSED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240036):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The object's current state didn't allow the operation.",
                        "WU_E_INVALID_OPERATION",
                        0x80240036,
                        innerException
                    ),
                    "WU_E_INVALID_OPERATION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240037):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The functionality for the operation isn't supported.",
                        "WU_E_NOT_SUPPORTED",
                        0x80240037,
                        innerException
                    ),
                    "WU_E_NOT_SUPPORTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240038):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The downloaded file has an unexpected content type.",
                        "WU_E_WINHTTP_INVALID_FILE",
                        0x80240038,
                        innerException
                    ),
                    "WU_E_WINHTTP_INVALID_FILE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240039):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Agent is asked by server to resync too many times.",
                        "WU_E_TOO_MANY_RESYNC",
                        0x80240039,
                        innerException
                    ),
                    "WU_E_TOO_MANY_RESYNC",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240040):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "WUA API  method doesn't run on Server Core installation.",
                        "WU_E_NO_SERVER_CORE_SUPPORT",
                        0x80240040,
                        innerException
                    ),
                    "WU_E_NO_SERVER_CORE_SUPPORT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240041):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Service isn't available while sysprep is running.",
                        "WU_E_SYSPREP_IN_PROGRESS",
                        0x80240041,
                        innerException
                    ),
                    "WU_E_SYSPREP_IN_PROGRESS",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240042):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update service is no longer registered with  AU .",
                        "WU_E_UNKNOWN_SERVICE",
                        0x80240042,
                        innerException
                    ),
                    "WU_E_UNKNOWN_SERVICE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240043):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "There's no support for  WUA UI .",
                        "WU_E_NO_UI_SUPPORT",
                        0x80240043,
                        innerException
                    ),
                    "WU_E_NO_UI_SUPPORT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80240FFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An operation failed due to reasons not covered by another error code.",
                        "WU_E_UNEXPECTED",
                        0x80240FFF,
                        innerException
                    ),
                    "WU_E_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80070422):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update service stopped working or isn't running.",
                        "",
                        0x80070422,
                        innerException
                    ),
                    "",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x00240001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent was stopped successfully.",
                        "WU_S_SERVICE_STOP",
                        0x00240001,
                        innerException
                    ),
                    "WU_S_SERVICE_STOP",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x00240002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent updated itself.",
                        "WU_S_SELFUPDATE",
                        0x00240002,
                        innerException
                    ),
                    "WU_S_SELFUPDATE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x00240003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Operation completed successfully but there were errors applying the updates.",
                        "WU_S_UPDATE_ERROR",
                        0x00240003,
                        innerException
                    ),
                    "WU_S_UPDATE_ERROR",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x00240004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "A callback was marked to be disconnected later because the request to disconnect the operation came while a callback was executing.",
                        "WU_S_MARKED_FOR_DISCONNECT",
                        0x00240004,
                        innerException
                    ),
                    "WU_S_MARKED_FOR_DISCONNECT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x00240005):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The system must be restarted to complete installation of the update.",
                        "WU_S_REBOOT_REQUIRED",
                        0x00240005,
                        innerException
                    ),
                    "WU_S_REBOOT_REQUIRED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x00240006):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update to be installed is already installed on the system.",
                        "WU_S_ALREADY_INSTALLED",
                        0x00240006,
                        innerException
                    ),
                    "WU_S_ALREADY_INSTALLED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x00240007):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update to be removed isn't installed on the system.",
                        "WU_S_ALREADY_UNINSTALLED",
                        0x00240007,
                        innerException
                    ),
                    "WU_S_ALREADY_UNINSTALLED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x00240008):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "The update to be downloaded has already been downloaded.",
                        "WU_S_ALREADY_DOWNLOADED",
                        0x00240008,
                        innerException
                    ),
                    "WU_S_ALREADY_DOWNLOADED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80241001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Search may have missed some updates because the Windows Installer is less than version 3.1.",
                        "WU_E_MSI_WRONG_VERSION",
                        0x80241001,
                        innerException
                    ),
                    "WU_E_MSI_WRONG_VERSION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80241002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Search may have missed some updates because the Windows Installer isn't configured.",
                        "WU_E_MSI_NOT_CONFIGURED",
                        0x80241002,
                        innerException
                    ),
                    "WU_E_MSI_NOT_CONFIGURED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80241003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Search may have missed some updates because policy has disabled Windows Installer patching.",
                        "WU_E_MSP_DISABLED",
                        0x80241003,
                        innerException
                    ),
                    "WU_E_MSP_DISABLED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80241004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An update couldn't be applied because the application is installed per-user.",
                        "WU_E_MSI_WRONG_APP_CONTEXT",
                        0x80241004,
                        innerException
                    ),
                    "WU_E_MSI_WRONG_APP_CONTEXT",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x80241FFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Search may have missed some updates because there was a failure of the Windows Installer.",
                        "WU_E_MSP_UNEXPECTED",
                        0x80241FFF,
                        innerException
                    ),
                    "WU_E_MSP_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D001):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because an INF file contains invalid information.",
                        "WU_E_SETUP_INVALID_INFDATA",
                        0x8024D001,
                        innerException
                    ),
                    "WU_E_SETUP_INVALID_INFDATA",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D002):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because the  wuident.cab  file contains invalid information.",
                        "WU_E_SETUP_INVALID_IDENTDATA",
                        0x8024D002,
                        innerException
                    ),
                    "WU_E_SETUP_INVALID_IDENTDATA",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D003):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because of an internal error that caused setup initialization to be performed twice.",
                        "WU_E_SETUP_ALREADY_INITIALIZED",
                        0x8024D003,
                        innerException
                    ),
                    "WU_E_SETUP_ALREADY_INITIALIZED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D004):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because setup initialization never completed successfully.",
                        "WU_E_SETUP_NOT_INITIALIZED",
                        0x8024D004,
                        innerException
                    ),
                    "WU_E_SETUP_NOT_INITIALIZED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D005):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because the versions specified in the INF don't match the actual source file versions.",
                        "WU_E_SETUP_SOURCE_VERSION_MISMATCH",
                        0x8024D005,
                        innerException
                    ),
                    "WU_E_SETUP_SOURCE_VERSION_MISMATCH",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D006):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because a WUA file on the target system is newer than the corresponding source file.",
                        "WU_E_SETUP_TARGET_VERSION_GREATER",
                        0x8024D006,
                        innerException
                    ),
                    "WU_E_SETUP_TARGET_VERSION_GREATER",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D007):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because  regsvr32.exe  returned an error.",
                        "WU_E_SETUP_REGISTRATION_FAILED",
                        0x8024D007,
                        innerException
                    ),
                    "WU_E_SETUP_REGISTRATION_FAILED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D009):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "An update to the Windows Update Agent was skipped due to a directive in the  wuident.cab  file.",
                        "WU_E_SETUP_SKIP_UPDATE",
                        0x8024D009,
                        innerException
                    ),
                    "WU_E_SETUP_SKIP_UPDATE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D00A):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because the current system configuration isn't supported.",
                        "WU_E_SETUP_UNSUPPORTED_CONFIGURATION",
                        0x8024D00A,
                        innerException
                    ),
                    "WU_E_SETUP_UNSUPPORTED_CONFIGURATION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D00B):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because the system is configured to block the update.",
                        "WU_E_SETUP_BLOCKED_CONFIGURATION",
                        0x8024D00B,
                        innerException
                    ),
                    "WU_E_SETUP_BLOCKED_CONFIGURATION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D00C):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because a restart of the system is required.",
                        "WU_E_SETUP_REBOOT_TO_FIX",
                        0x8024D00C,
                        innerException
                    ),
                    "WU_E_SETUP_REBOOT_TO_FIX",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D00D):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent setup is already running.",
                        "WU_E_SETUP_ALREADYRUNNING",
                        0x8024D00D,
                        innerException
                    ),
                    "WU_E_SETUP_ALREADYRUNNING",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D00E):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent setup package requires a reboot to complete installation.",
                        "WU_E_SETUP_REBOOTREQUIRED",
                        0x8024D00E,
                        innerException
                    ),
                    "WU_E_SETUP_REBOOTREQUIRED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D00F):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because the setup handler failed during execution.",
                        "WU_E_SETUP_HANDLER_EXEC_FAILURE",
                        0x8024D00F,
                        innerException
                    ),
                    "WU_E_SETUP_HANDLER_EXEC_FAILURE",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D010):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because the registry contains invalid information.",
                        "WU_E_SETUP_INVALID_REGISTRY_DATA",
                        0x8024D010,
                        innerException
                    ),
                    "WU_E_SETUP_INVALID_REGISTRY_DATA",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024D013):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because the server doesn't contain update information for this version.",
                        "WU_E_SETUP_WRONG_SERVER_VERSION",
                        0x8024D013,
                        innerException
                    ),
                    "WU_E_SETUP_WRONG_SERVER_VERSION",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            case unchecked((int)0x8024DFFF):
                return new ErrorRecord(
                    new WindowsUpdateException(
                        "Windows Update Agent couldn't be updated because of an error not covered by another  WU_E_SETUP_*  error code.",
                        "WU_E_SETUP_UNEXPECTED",
                        0x8024DFFF,
                        innerException
                    ),
                    "WU_E_SETUP_UNEXPECTED",
                    ErrorCategory.NotSpecified,
                    targetObject
                );
            default:
                return new ErrorRecord(
                    innerException
                        ?? new WindowsUpdateException(
                            "An unexpected error occurred while processing the Windows Update operation."
                        ),
                    "WindowsUpdateException",
                    ErrorCategory.NotSpecified,
                    targetObject
                )
                {
                    ErrorDetails = new(
                        $"An unexpected error occurred while processing the Windows Update Agent operation. HResult: 0x{unchecked((uint)hresult):X8}."
                    )
                };
        }
    }
}
