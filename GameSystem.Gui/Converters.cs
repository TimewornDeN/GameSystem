using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace GameSystem.Gui;

/// <summary>Turns the bytes of a downloaded picture into an image the window can draw (null means: no picture).</summary>
public sealed class BytesToImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] { Length: > 0 } bytes)
            return null;

        try
        {
            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad; // Read everything now, so the stream can be closed.
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception)
        {
            // A broken or unsupported picture is shown as the placeholder.
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
