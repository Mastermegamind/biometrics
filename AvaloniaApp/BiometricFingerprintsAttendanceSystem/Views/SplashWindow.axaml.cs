using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BiometricFingerprintsAttendanceSystem.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
