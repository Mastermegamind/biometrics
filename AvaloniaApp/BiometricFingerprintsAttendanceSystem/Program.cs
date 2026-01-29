using Avalonia;
using DotNetEnv;

namespace BiometricFingerprintsAttendanceSystem;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Env.Load();
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
