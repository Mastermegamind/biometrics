using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace BiometricFingerprintsAttendanceSystem.Converters;

/// <summary>
/// Converts boolean to success/error background color (green/red tint).
/// </summary>
public class BoolToSuccessBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSuccess)
        {
            return isSuccess
                ? new SolidColorBrush(Color.Parse("#E8F5E9"))  // Light green
                : new SolidColorBrush(Color.Parse("#FFEBEE")); // Light red
        }
        return new SolidColorBrush(Color.Parse("#F5F5F5"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to success/error solid color (green/red).
/// </summary>
public class BoolToSuccessColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSuccess)
        {
            return isSuccess
                ? new SolidColorBrush(Color.Parse("#4CAF50"))  // Green
                : new SolidColorBrush(Color.Parse("#F44336")); // Red
        }
        return new SolidColorBrush(Color.Parse("#9E9E9E"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to checkmark (✓) or cross (✕) character.
/// </summary>
public class BoolToCheckmarkConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isSuccess)
        {
            return isSuccess ? "✓" : "✕";
        }
        return "?";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean online status to color (green for online, gray for offline).
/// </summary>
public class BoolToOnlineColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOnline)
        {
            return isOnline
                ? new SolidColorBrush(Color.Parse("#4CAF50"))  // Green
                : new SolidColorBrush(Color.Parse("#9E9E9E")); // Gray
        }
        return new SolidColorBrush(Color.Parse("#9E9E9E"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean online status to text (Online/Offline).
/// </summary>
public class BoolToOnlineTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isOnline)
        {
            return isOnline ? "Online" : "Offline";
        }
        return "Unknown";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean to status background color for enrollment status.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isEnrolled)
        {
            return isEnrolled
                ? new SolidColorBrush(Color.Parse("#22C55E"))
                : new SolidColorBrush(Color.Parse("#9CA3AF"));
        }
        return new SolidColorBrush(Color.Parse("#9CA3AF"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts integer to visibility (visible if greater than 0).
/// </summary>
public class IntToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}

/// <summary>
/// Converts current step number to color for capture progress dots.
/// Parameter is the step number (1-4). Value is the current completed step.
/// </summary>
public class StepToColorConverter : IValueConverter
{
    private static readonly SolidColorBrush CompletedColor = new(Color.Parse("#10B981")); // Green
    private static readonly SolidColorBrush CurrentColor = new(Color.Parse("#F59E0B"));   // Amber
    private static readonly SolidColorBrush PendingColor = new(Color.Parse("#E5E7EB"));   // Gray

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int currentStep || parameter is not string stepStr ||
            !int.TryParse(stepStr, out var stepNumber))
        {
            return PendingColor;
        }

        // If current step is greater than this step number, it's completed
        if (currentStep > stepNumber)
        {
            return CompletedColor;
        }
        // If current step equals this step number, it's the current one
        if (currentStep == stepNumber)
        {
            return CurrentColor;
        }
        // Otherwise it's pending
        return PendingColor;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
