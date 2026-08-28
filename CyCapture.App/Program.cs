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
        _singleInstance = new Mutex(true, "Local\\CyCapture.Avalonia.SingleInstance", out var isFirstInstance);
        if (!isFirstInstance)
        {
            NativeMethods.SignalExistingInstance();
            return 0;
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
            _singleInstance.ReleaseMutex();
            _singleInstance.Dispose();
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
