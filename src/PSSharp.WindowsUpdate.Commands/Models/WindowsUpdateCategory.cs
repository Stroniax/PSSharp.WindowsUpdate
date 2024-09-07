using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed record WindowsUpdateCategory
{
    internal WindowsUpdateCategory(ICategory category)
    {
        _category = category;
    }

    private readonly ICategory _category;

    public string Name => _category.Name;
    public string Description => _category.Description;
    public Guid? CategoryID =>
        _category.CategoryID is null ? null : Guid.Parse(_category.CategoryID);
    public string Type => _category.Type;
    public int Order => _category.Order;

    private IEnumerable<WindowsUpdateCategory>? _children;
    public IEnumerable<WindowsUpdateCategory>? Children => _children ??= _category.Children?.Map();

    private WindowsUpdateCategory? _parent;
    public WindowsUpdateCategory? Parent => _parent ??= _category.Parent.Map();

    private IEnumerable<WindowsUpdate>? _updates;
    public IEnumerable<WindowsUpdate>? Updates => _updates ??= _category.Updates.Map();
    private WindowsUpdateImage? _image;
    public WindowsUpdateImage Image => _image ??= _category.Image.Map();
}
