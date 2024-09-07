using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public enum OperationResultCode
{
    NotStarted = tagOperationResultCode.orcNotStarted,
    InProgress = tagOperationResultCode.orcInProgress,
    Succeeded = tagOperationResultCode.orcSucceeded,
    SucceededWithErrors = tagOperationResultCode.orcSucceededWithErrors,
    Failed = tagOperationResultCode.orcFailed,
    Aborted = tagOperationResultCode.orcAborted,
}
