using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CyCapture.Platform.Windows;

namespace CyCapture.Views;

internal sealed class ToastWindow : Window
{
    internal ToastWindow(string title, string message, bool error, string? filePath = null)
    {
        Width = 390;
        Height = filePath is null ? 92 : 108;
        CanResize = false;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        WindowDecorations = Avalonia.Controls.WindowDecorations.None;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = Brushes.Transparent;
        IsHitTestVisible = filePath is not null;

        Content = new Border
        {
            Margin = new Thickness(8),
            Padding = new Thickness(14, 11),
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromArgb(247, 9, 15, 15)),
            BorderBrush = new SolidColorBrush(error ? Color.FromRgb(255, 70, 87) : Color.FromRgb(155, 255, 40)),
            BorderThickness = new Thickness(1),
            Child = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
                ColumnSpacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = error ? "!" : "✓",
                        FontSize = 22,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(error ? Color.FromRgb(255, 70, 87) : Color.FromRgb(155, 255, 40)),
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new StackPanel
                    {
                        [Grid.ColumnProperty] = 1,
                        Spacing = 2,
                        VerticalAlignment = VerticalAlignment.Center,
                        Children =
                        {
                            new TextBlock { Text = title, FontWeight = FontWeight.Bold, FontSize = 13 },
                            new TextBlock { Text = message, Foreground = new SolidColorBrush(Color.FromRgb(166, 179, 175)), FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis },
                            new TextBlock
                            {
                                Text = "Cliquer pour ouvrir le fichier",
                                IsVisible = filePath is not null,
                                Foreground = new SolidColorBrush(Color.FromRgb(155, 255, 40)),
                                FontSize = 10
                            }
                        }
                    }
                }
            }
        };

        if (filePath is not null)
        {
            Cursor = new Cursor(StandardCursorType.Hand);
            PointerPressed += (_, args) =>
            {
                if (!args.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
                try
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                catch
                {
                    // The gallery remains available if the default Windows application cannot open the file.
                }
                Close();
            };
        }

        Opened += (_, _) =>
        {
            if (NativeMethods.GetCursorPos(out var cursor))
            {
                var monitor = NativeMethods.MonitorAt(cursor.X, cursor.Y);
                var pixelWidth = (int)Math.Ceiling(Width * monitor.Scale);
                var pixelHeight = (int)Math.Ceiling(Height * monitor.Scale);
                Position = new PixelPoint(monitor.WorkArea.Right - pixelWidth - 12, monitor.WorkArea.Bottom - pixelHeight - 12);
            }
            NativeMethods.SetWindowDisplayAffinity(TryGetPlatformHandle()?.Handle ?? 0, NativeMethods.WdaExcludeFromCapture);
        };

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(error ? 5 : filePath is null ? 3.5 : 6) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Close();
        };
        timer.Start();
    }
}
