using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Fonts.Inter;
using CyCapture.Platform.Windows;

namespace CyCapture;

internal static class Program
{
    private static Mutex? _singleInstance;

    [STAThread]
    public static int Main(string[] args)
    {
        NativeMethods.EnablePerMonitorDpiAwareness();
        var isSelfTest = args.Contains("--self-test", StringComparer.OrdinalIgnoreCase);
        if (!isSelfTest)
        {
            _singleInstance = new Mutex(true, "Local\\CyCapture.Avalonia.SingleInstance", out var isFirstInstance);
            if (!isFirstInstance)
            {
                NativeMethods.SignalExistingInstance();
                _singleInstance.Dispose();
                _singleInstance = null;
                return 0;
            }
        }

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(
                args,
                ShutdownMode.OnExplicitShutdown);
            return 0;
        }
        finally
        {
            if (_singleInstance is not null)
            {
                _singleInstance.ReleaseMutex();
                _singleInstance.Dispose();
                _singleInstance = null;
            }
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                RenderingMode = [Win32RenderingMode.Software],
                CompositionMode = [Win32CompositionMode.RedirectionSurface],
                ShouldRenderOnUIThread = true
            })
            .WithInterFont()
            .LogToTrace();
}
