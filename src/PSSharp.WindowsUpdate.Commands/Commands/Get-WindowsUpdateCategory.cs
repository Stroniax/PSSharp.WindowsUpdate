using System.Management.Automation;
using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

[Cmdlet(VerbsCommon.Get, "WindowsUpdateCategory", DefaultParameterSetName = "Default")]
public sealed class GetWindowsUpdateCategory : WindowsUpdateCmdlet<WindowsUpdateCmdletContext>
{
    protected override void ProcessRecord(
        WindowsUpdateCmdletContext context,
        CancellationToken cancellationToken
    )
    {
        var searcher = context.Searcher.CreateUpdateSearcher();
        searcher.Online = false;
        searcher.ServerSelection = WUApiLib.ServerSelection.ssDefault;

        var categories = searcher.Search("").RootCategories;
        WriteCategoryAndChildren(categories);
    }

    private void WriteCategoryAndChildren(ICategoryCollection? categories)
    {
        if (categories is null)
        {
            return;
        }

        foreach (ICategory category in categories)
        {
            WriteObject(new WindowsUpdateCategory(category));
        }

        foreach (ICategory category in categories)
        {
            WriteCategoryAndChildren(category.Children);
        }
    }
}
