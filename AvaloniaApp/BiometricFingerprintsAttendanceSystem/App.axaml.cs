using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BiometricFingerprintsAttendanceSystem.Services;
using BiometricFingerprintsAttendanceSystem.ViewModels;
using BiometricFingerprintsAttendanceSystem.Views;
using System.Threading.Tasks;

namespace BiometricFingerprintsAttendanceSystem;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splash = new SplashWindow();
            desktop.MainWindow = splash;
            splash.Show();

            await Task.Delay(1500);

            var appState = new AppState(AppConfig.Load());
            var services = new ServiceRegistry(appState);
            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(services)
            };

            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            splash.Close();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
