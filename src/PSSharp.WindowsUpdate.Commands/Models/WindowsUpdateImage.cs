using WUApiLib;

namespace PSSharp.WindowsUpdate.Commands;

public sealed record WindowsUpdateImage
{
    internal WindowsUpdateImage(IImageInformation image)
    {
        _image = image;
    }

    private readonly IImageInformation _image;

    public string AltText => _image.AltText;
    public Uri Source => new(_image.Source);
    public int Height => _image.Height;
    public int Width => _image.Width;
}
