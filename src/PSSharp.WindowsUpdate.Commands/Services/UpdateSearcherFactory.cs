using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed class UpdateSearcherFactory(
    WindowsUpdateSessionAccessor session,
    WindowsUpdateCache cache
) : IUpdateSearcherFactory
{
    private readonly WindowsUpdateSessionAccessor _session = session;
    private readonly WindowsUpdateCache _cache = cache;

    public IUpdateSearcher CreateUpdateSearcher()
    {
        var searcher = _session.GetSession().CreateUpdateSearcher();
        searcher.ClientApplicationID = "PSSharp.WindowsUpdate";
        var decorator = new UpdateSearcherWithCache((IUpdateSearcher3)searcher, _cache);
        return decorator;
    }
}

file sealed class UpdateSearcherWithCache(IUpdateSearcher3 inner, WindowsUpdateCache cache)
    : IUpdateSearcher3
{
    private IUpdateSearcher3 _inner = inner;
    private WindowsUpdateCache _cache = cache;

    public ISearchJob BeginSearch(string criteria, object onCompleted, object state)
    {
        var job = _inner.BeginSearch(criteria, onCompleted, state);
        return job;
    }

    public ISearchResult EndSearch(ISearchJob searchJob)
    {
        var result = _inner.EndSearch(searchJob);
        foreach (IUpdate update in result.Updates)
        {
            _cache.Set(new WindowsUpdate(update));
        }
        return result;
    }

    public string EscapeString(string unescaped)
    {
        return _inner.EscapeString(unescaped);
    }

    public IUpdateHistoryEntryCollection QueryHistory(int startIndex, int Count)
    {
        return _inner.QueryHistory(startIndex, Count);
    }

    public ISearchResult Search(string criteria)
    {
        var result = _inner.Search(criteria);
        foreach (IUpdate update in result.Updates)
        {
            _cache.Set(new WindowsUpdate(update));
        }
        return result;
    }

    public int GetTotalHistoryCount()
    {
        return _inner.GetTotalHistoryCount();
    }

    public bool CanAutomaticallyUpgradeService
    {
        get => _inner.CanAutomaticallyUpgradeService;
        set => _inner.CanAutomaticallyUpgradeService = value;
    }
    public string ClientApplicationID
    {
        get => _inner.ClientApplicationID;
        set => _inner.ClientApplicationID = value;
    }
    public bool IncludePotentiallySupersededUpdates
    {
        get => _inner.IncludePotentiallySupersededUpdates;
        set => _inner.IncludePotentiallySupersededUpdates = value;
    }
    public WUApiLib.ServerSelection ServerSelection
    {
        get => _inner.ServerSelection;
        set => _inner.ServerSelection = value;
    }
    public bool Online
    {
        get => _inner.Online;
        set => _inner.Online = value;
    }
    public string ServiceID
    {
        get => _inner.ServiceID;
        set => _inner.ServiceID = value;
    }
    public bool IgnoreDownloadPriority
    {
        get => _inner.IgnoreDownloadPriority;
        set => _inner.IgnoreDownloadPriority = value;
    }
    public SearchScope SearchScope
    {
        get => _inner.SearchScope;
        set => _inner.SearchScope = value;
    }
}
