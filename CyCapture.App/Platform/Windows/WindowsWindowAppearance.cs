using Avalonia.Controls;
using Avalonia.Platform;

namespace CyCapture.Platform.Windows;

internal static class WindowsWindowAppearance
{
    internal static void Attach(Window window)
    {
        void Apply() => NativeMethods.ApplyDarkWindowChrome(window.TryGetPlatformHandle()?.Handle ?? 0);
        window.Opened += (_, _) => Apply();
        window.Activated += (_, _) => Apply();
        window.Deactivated += (_, _) => Apply();
    }
}
