using System.CodeDom;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

internal static partial class WindowsUpdateMapping
{
    public static StringCollection ToStringCollection(this IEnumerable<string> collection)
    {
        var coll = new StringCollection();
        foreach (var item in collection)
        {
            coll.Add(item);
        }
        return coll;
    }

    public static ServerSelection Map(this tagServerSelection serverSelection)
    {
        return serverSelection switch
        {
            tagServerSelection.ssDefault => ServerSelection.Default,
            tagServerSelection.ssManagedServer => ServerSelection.ManagedServer,
            tagServerSelection.ssWindowsUpdate => ServerSelection.WindowsUpdate,
            tagServerSelection.ssOthers => ServerSelection.Others,
            _ => (ServerSelection)(int)serverSelection,
        };
    }

    public static ServerSelection Map(this WUApiLib.ServerSelection serverSelection)
    {
        return serverSelection switch
        {
            WUApiLib.ServerSelection.ssDefault => ServerSelection.Default,
            WUApiLib.ServerSelection.ssManagedServer => ServerSelection.ManagedServer,
            WUApiLib.ServerSelection.ssWindowsUpdate => ServerSelection.WindowsUpdate,
            WUApiLib.ServerSelection.ssOthers => ServerSelection.Others,
            _ => (ServerSelection)(int)serverSelection,
        };
    }

    public static UpdateOperation Map(this tagUpdateOperation operation)
    {
        return operation switch
        {
            tagUpdateOperation.uoInstallation => UpdateOperation.Installation,
            tagUpdateOperation.uoUninstallation => UpdateOperation.Uninstallation,
            _ => (UpdateOperation)(int)operation,
        };
    }

    public static OperationResultCode Map(this tagOperationResultCode orc)
    {
        return orc switch
        {
            tagOperationResultCode.orcNotStarted => OperationResultCode.NotStarted,
            tagOperationResultCode.orcInProgress => OperationResultCode.InProgress,
            tagOperationResultCode.orcSucceeded => OperationResultCode.Succeeded,
            tagOperationResultCode.orcSucceededWithErrors
                => OperationResultCode.SucceededWithErrors,
            tagOperationResultCode.orcFailed => OperationResultCode.Failed,
            tagOperationResultCode.orcAborted => OperationResultCode.Aborted,
            _ => (OperationResultCode)(int)orc,
        };
    }

    public static OperationResultCode Map(this WUApiLib.OperationResultCode orc)
    {
        return orc switch
        {
            WUApiLib.OperationResultCode.orcNotStarted => OperationResultCode.NotStarted,
            WUApiLib.OperationResultCode.orcInProgress => OperationResultCode.InProgress,
            WUApiLib.OperationResultCode.orcSucceeded => OperationResultCode.Succeeded,
            WUApiLib.OperationResultCode.orcSucceededWithErrors
                => OperationResultCode.SucceededWithErrors,
            WUApiLib.OperationResultCode.orcFailed => OperationResultCode.Failed,
            WUApiLib.OperationResultCode.orcAborted => OperationResultCode.Aborted,
            _ => (OperationResultCode)(int)orc,
        };
    }

    [return: NotNullIfNotNull(nameof(collection))]
    public static IEnumerable<string>? Map(this IStringCollection? collection) =>
        collection?.Cast<string>().ToImmutableArray();

    [return: NotNullIfNotNull(nameof(identity))]
    public static WindowsUpdateIdentity? Map(this IUpdateIdentity? identity) =>
        identity is null ? null : new(identity);

    public static WindowsUpdateHistoryEntry Map(this IUpdateHistoryEntry2 entry) => new(entry);

    [return: NotNullIfNotNull(nameof(category))]
    public static WindowsUpdateCategory? Map(this ICategory? category) =>
        category is null ? null : new(category);

    [return: NotNullIfNotNull(nameof(collection))]
    public static IEnumerable<WindowsUpdateCategory>? Map(this ICategoryCollection? collection) =>
        collection is null ? null : collection.Cast<ICategory>().Select(Map).ToImmutableArray();

    [return: NotNullIfNotNull(nameof(update))]
    public static WindowsUpdate? Map(this IUpdate? update) => update is null ? null : new(update);

    [return: NotNullIfNotNull(nameof(collection))]
    public static IEnumerable<WindowsUpdate>? Map(this IUpdateCollection? collection) =>
        collection is null ? null : collection.Cast<IUpdate>().Select(Map).ToImmutableArray();

    [return: NotNullIfNotNull(nameof(image))]
    public static WindowsUpdateImage? Map(this IImageInformation? image) =>
        image is null ? null : new(image);

    [return: NotNullIfNotNull(nameof(service))]
    public static WindowsUpdateService? Map(this IUpdateService? service) =>
        service is null ? null : new(service);
}
