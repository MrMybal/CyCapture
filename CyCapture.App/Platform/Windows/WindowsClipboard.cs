using System.Collections.Specialized;

namespace CyCapture.Platform.Windows;

internal static class WindowsClipboard
{
    internal static void CopyFile(string path)
        => SetClipboard(path, includeImage: false);

    internal static void CopyImageAndFile(string path)
        => SetClipboard(path, includeImage: true);

    private static void SetClipboard(string path, bool includeImage)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("Le fichier à copier est introuvable.", fullPath);

        Exception? lastError = null;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                var files = new StringCollection { fullPath };
                if (includeImage)
                {
                    var image = new System.Windows.Media.Imaging.BitmapImage();
                    image.BeginInit();
                    image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(fullPath, UriKind.Absolute);
                    image.EndInit();
                    image.Freeze();

                    var data = new System.Windows.DataObject();
                    data.SetFileDropList(files);
                    data.SetImage(image);
                    System.Windows.Clipboard.SetDataObject(data, true);
                }
                else
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
