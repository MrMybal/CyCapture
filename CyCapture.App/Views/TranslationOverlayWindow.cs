using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CyCapture.Models;
using CyCapture.Platform.Windows;
using CyCapture.Services;

namespace CyCapture.Views;

internal sealed class TranslationOverlayWindow : Window
{
    private readonly ScreenTranslationResult _result;

    internal TranslationOverlayWindow(ScreenTranslationResult result)
    {
        _result = result;
        var monitor = result.Selection.Monitor;
        Title = UiLocalization.Text("CyCapture · Traduction", "CyCapture · Translation");
        Width = monitor.Bounds.Width / monitor.Scale;
        Height = monitor.Bounds.Height / monitor.Scale;
        Position = new PixelPoint(monitor.Bounds.X, monitor.Bounds.Y);
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowDecorations = WindowDecorations.None;
        CanResize = false;
        ShowInTaskbar = false;
        Topmost = true;
        Background = Brushes.Black;

        using var stream = new MemoryStream(result.FrozenDesktopPng, writable: false);
        var screenshot = new Bitmap(stream);
        var root = new Grid();
        root.Children.Add(new Image { Source = screenshot, Stretch = Stretch.Fill, IsHitTestVisible = false });
        var canvas = new Canvas { IsHitTestVisible = false };
        AddTranslationBoxes(canvas, monitor.Scale);
        root.Children.Add(canvas);
        root.Children.Add(BuildToolbar());
        Content = root;

        Opened += (_, _) =>
        {
            NativeMethods.SetWindowDisplayAffinity(TryGetPlatformHandle()?.Handle ?? 0, NativeMethods.WdaExcludeFromCapture);
            Activate();
            Focus();
        };
        Closed += (_, _) => screenshot.Dispose();
        KeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape)
            {
                Close();
                args.Handled = true;
            }
            else if (args.Key == Key.C && args.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                CopyAll();
                args.Handled = true;
            }
        };
    }

    private void AddTranslationBoxes(Canvas canvas, double scale)
    {
        var selection = _result.Selection;
        var selectionX = (selection.Bounds.X - selection.Monitor.Bounds.X) / scale;
        var selectionY = (selection.Bounds.Y - selection.Monitor.Bounds.Y) / scale;
        var maxWidth = selection.Monitor.Bounds.Width / scale;
        var rtl = _result.TargetLanguage is "ar" or "he" or "fa" or "ur";
        foreach (var line in _result.Lines)
        {
            var x = selectionX + line.Bounds.X / scale;
            var y = selectionY + line.Bounds.Y / scale;
            var originalWidth = Math.Max(80, line.Bounds.Width / scale);
            var width = Math.Min(Math.Max(originalWidth, 150), Math.Max(80, maxWidth - x - 8));
            var fontSize = Math.Clamp(line.Bounds.Height / scale * 0.62, 12, 28);
            var text = new TextBlock
            {
                Text = line.TranslatedText,
                FontSize = fontSize,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                MaxWidth = width - 12
            };
            var box = new Border
            {
                Width = width,
                MinHeight = Math.Max(28, line.Bounds.Height / scale),
                Padding = new Thickness(6, 3),
                CornerRadius = new CornerRadius(5),
                Background = new SolidColorBrush(Color.FromArgb(226, 5, 10, 10)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(210, 155, 255, 40)),
                BorderThickness = new Thickness(1),
                Child = text,
                IsHitTestVisible = false
            };
            ToolTip.SetTip(box, line.OriginalText);
            Canvas.SetLeft(box, x);
            Canvas.SetTop(box, y);
            canvas.Children.Add(box);
        }
    }

    private Border BuildToolbar()
    {
        var copy = new Button
        {
            Content = UiLocalization.Text("Copier le texte", "Copy text"),
            Padding = new Thickness(13, 6)
        };
        copy.Click += (_, _) => CopyAll();
        var close = new Button
        {
            Content = UiLocalization.Text("Fermer · Échap", "Close · Esc"),
            Padding = new Thickness(13, 6)
        };
        close.Click += (_, _) => Close();
        return new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 18, 0, 0),
            Padding = new Thickness(8),
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(238, 8, 14, 14)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(170, 155, 255, 40)),
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"{TranslationLanguageCatalog.DisplayName(_result.SourceLanguage)} → {TranslationLanguageCatalog.DisplayName(_result.TargetLanguage)}",
                        FontWeight = FontWeight.Bold,
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(5, 0, 8, 0)
                    },
                    copy,
                    close
                }
            }
        };
    }

    private void CopyAll()
    {
        try { WindowsClipboard.CopyText(_result.PlainText); }
        catch
        {
            // Keep the translation visible when another process temporarily owns the clipboard.
        }
    }
}
