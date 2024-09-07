using System.Management.Automation;
using System.Text;
using PSValueWildcard;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(VerbsCommon.Get, "WindowsUpdate", DefaultParameterSetName = "Default")]
[Alias("gwu")]
[OutputType(typeof(WindowsUpdate))]
public sealed class GetWindowsUpdateCommand
    : WindowsUpdateCmdlet<WindowsUpdateCmdletContext>,
        IWindowsUpdateSessionCmdlet
{
    /// <summary>
    /// The title of the update to search for.
    /// </summary>
    [Parameter(Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "Default")]
    [WindowsUpdateCompletion]
    [SupportsWildcards]
    [Alias("Name")]
    public string[]? Title { get; set; }

    [Parameter(ParameterSetName = "Default")]
    public SwitchParameter IncludeInstalledUpdates { get; set; }

    [Parameter(ParameterSetName = "Default")]
    public bool? RebootRequired { get; set; }

    [Parameter(ParameterSetName = "Default")]
    public Guid CategoryID { get; set; }

    [Parameter(ParameterSetName = "Default")]
    public bool? AutoSelectOnWebSites { get; set; }

    [Parameter(ParameterSetName = "Default")]
    public bool? BrowseOnly { get; set; }

    /// <summary>
    /// Include hidden updates (and potentially superceded updates) in the search results.
    /// </summary>
    [Parameter]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// Raw searcher criteria.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = "Criteria")]
    public required string Criteria { get; set; }

    /// <summary>
    /// The session to use for the search.
    /// </summary>
    [Parameter]
    public WindowsUpdateSession? Session { get; init; }

    /// <summary>
    /// Perform the search as a background job. This job runs asynchronously and can be monitored with
    /// the <c>*-Job</c> cmdlets.
    /// </summary>
    [Parameter]
    public SwitchParameter AsJob { get; init; }

    /// <summary>
    /// Offline search. By default the command will search online; this switch instructs the
    /// searcher to only find updates known in the offline cache.
    /// </summary>
    [Parameter]
    public SwitchParameter Offline { get; set; }

    [Parameter]
    public SwitchParameter IgnoreDownloadPriority { get; set; }

    [Parameter(DontShow = true)]
    public SwitchParameter CanAutomaticallyUpgradeService { get; set; }

    [Parameter]
    public SearchScope SearchScope { get; set; }

    /// <summary>
    /// ServiceID of the server to search, or a well-known Windows Update server alias
    /// (Default, WindowsUpdate, Managed).
    /// </summary>
    [Parameter]
    [Alias("ServerSelection")]
    [WindowsUpdateServiceIdTransformation]
    [WindowsUpdateServiceCompletion]
    public Guid ServiceID { get; set; }

    protected override void ProcessRecord(
        WindowsUpdateCmdletContext context,
        CancellationToken cancellationToken
    )
    {
        var searcher = context.Searcher.CreateUpdateSearcher();
        searcher.Online = !Offline;
        ((IUpdateSearcher2)searcher).IgnoreDownloadPriority = IgnoreDownloadPriority;
        searcher.CanAutomaticallyUpgradeService = CanAutomaticallyUpgradeService;

        if (ServiceID == Guid.Empty)
        {
            searcher.ServerSelection = WUApiLib.ServerSelection.ssDefault;
        }
        else
        {
            searcher.ServerSelection = WUApiLib.ServerSelection.ssOthers;
            searcher.ServiceID = ServiceID.ToString();
        }

        var criteria = GetCriteria(searcher);

        ProcessSearch(searcher, criteria, cancellationToken);
    }

    private void ProcessSearch(
        IUpdateSearcher searcher,
        string criteria,
        CancellationToken cancellationToken
    )
    {
        WriteDebug(
            $"Search for Windows Updates on service '{searcher.ServiceID}' with Criteria: {criteria}"
        );
        if (AsJob)
        {
            ProcessJob(searcher, criteria);
        }
        else
        {
            ProcessSynchronous(searcher, criteria, cancellationToken);
        }
    }

    private void ProcessSynchronous(
        IUpdateSearcher searcher,
        string criteria,
        CancellationToken cancellationToken
    )
    {
        ISearchResult result;
        try
        {
            result = searcher.Search(criteria, cancellationToken);
        }
        catch (Exception e)
        {
            var error = ErrorRecordFactory.CreateErrorRecord(
                ErrorRecordFactory.CreateBestMatchException(e),
                searcher,
                ErrorRecordFactory.UpdateSearchFailure
            );
            WriteError(error);
            return;
        }
        foreach (IUpdateException error in result.Warnings)
        {
            WriteError(
                new ErrorRecord(
                    new WindowsUpdateException(error),
                    "UpdateSearchException",
                    ErrorCategory.NotSpecified,
                    error
                )
            );
        }

        foreach (IUpdate update in result.Updates)
        {
            if (
                Title is { Length: > 0 }
                && !Title.Any(n => ValueWildcardPattern.IsMatch(update.Title, n))
            )
            {
                continue;
            }

            WriteObject(new WindowsUpdate(update));
        }
    }

    private void ProcessJob(IUpdateSearcher searcher, string criteria)
    {
        var job = new WindowsUpdateSearcherJob(searcher, criteria, Title);

        JobRepository.Add(job);
        job.StartJob();
        WriteObject(job);
    }

    private string GetCriteria(IUpdateSearcher searcher)
    {
        if (ParameterSetName == "Criteria")
        {
            return Criteria;
        }

        var sb = new StringBuilder();

        if (!Force)
        {
            sb.Append("IsHidden=0");
        }
        else
        {
            searcher.IncludePotentiallySupersededUpdates = true;
        }

        if (!IncludeInstalledUpdates)
        {
            if (sb.Length > 0)
            {
                sb.Append(" and ");
            }
            sb.Append($"IsInstalled=0");
        }

        if (RebootRequired.HasValue)
        {
            if (sb.Length > 0)
            {
                sb.Append(" and ");
            }
            sb.Append($"RebootRequired=");
            sb.Append(RebootRequired.Value ? '1' : '0');
        }

        if (CategoryID != Guid.Empty)
        {
            if (sb.Length > 0)
            {
                sb.Append(" and ");
            }
            sb.Append($"CategoryIDs contains '{CategoryID}'");
        }

        if (AutoSelectOnWebSites.HasValue)
        {
            if (sb.Length > 0)
            {
                sb.Append(" and ");
            }
            sb.Append($"AutoSelectOnWebSites=");
            sb.Append(AutoSelectOnWebSites.Value ? '1' : '0');
        }

        if (BrowseOnly.HasValue)
        {
            if (sb.Length > 0)
            {
                sb.Append(" and ");
            }
            sb.Append($"BrowseOnly=");
            sb.Append(BrowseOnly.Value ? '1' : '0');
        }

        return sb.ToString();
    }
}
