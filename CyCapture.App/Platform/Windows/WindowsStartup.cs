using Microsoft.Win32;

namespace CyCapture.Platform.Windows;

internal static class WindowsStartup
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "CyCapture";

    internal static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(ValueName) is string command && !string.IsNullOrWhiteSpace(command);
        }
        catch
        {
            return false;
        }
    }

    internal static bool TrySetEnabled(bool enabled, out string? error)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true)
                            ?? throw new InvalidOperationException("La clé de démarrage Windows est inaccessible.");
            if (enabled)
            {
                var executable = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(executable))
                    throw new InvalidOperationException("Le chemin de CyCapture est introuvable.");
                key.SetValue(ValueName, $"\"{Path.GetFullPath(executable)}\" --startup", RegistryValueKind.String);
            }
            else
                key.DeleteValue(ValueName, false);

            error = null;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }
}
