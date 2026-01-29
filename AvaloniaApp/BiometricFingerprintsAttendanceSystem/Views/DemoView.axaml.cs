using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Globalization;
using System.IO;

namespace BiometricFingerprintsAttendanceSystem.Views;

public partial class DemoView : UserControl
{
    public DemoView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

public sealed class StatusColorConverter : IValueConverter
{
    public static readonly StatusColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string statusType)
        {
            return statusType switch
            {
                "success" => new SolidColorBrush(Color.FromArgb(40, 34, 197, 94)),   // Green tint
                "error" => new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)),     // Red tint
                _ => new SolidColorBrush(Color.FromArgb(40, 59, 130, 246))           // Blue tint (info)
            };
        }
        return new SolidColorBrush(Color.FromArgb(40, 59, 130, 246));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class EnrollmentBrushConverter : IValueConverter
{
    public static readonly EnrollmentBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool enrolled && enrolled)
        {
            return new SolidColorBrush(Color.FromRgb(34, 197, 94));
        }

        return new SolidColorBrush(Color.FromRgb(148, 163, 184));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public sealed class ImagePathToBitmapConverter : IValueConverter
{
    public static readonly ImagePathToBitmapConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return new Bitmap(path);
        }
        catch
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
