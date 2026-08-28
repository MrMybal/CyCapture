using System.Collections.Specialized;

namespace CyCapture.Platform.Windows;

internal static class WindowsClipboard
{
    internal static void CopyFile(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Le fichier à copier est introuvable.", fullPath);

        Exception? lastError = null;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                var files = new StringCollection { fullPath };
                System.Windows.Clipboard.SetFileDropList(files);
                return;
            }
            catch (Exception error)
            {
                lastError = error;
                Thread.Sleep(35 * (attempt + 1));
            }
        }
        throw new InvalidOperationException("Le presse-papiers Windows est occupé.", lastError);
    }
}
