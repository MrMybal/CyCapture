using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CyCapture.Models;
using CyCapture.Platform.Windows;

namespace CyCapture.Views;

internal sealed class RecordingIndicatorWindow : Window
{
    private readonly TextBlock? _timer;

    internal RecordingIndicatorWindow(PixelBounds bounds, double scale, bool hud)
    {
        Width = Math.Max(1, bounds.Width / scale);
        Height = Math.Max(1, bounds.Height / scale);
        Position = new PixelPoint(bounds.X, bounds.Y);
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowDecorations = Avalonia.Controls.WindowDecorations.None;
        CanResize = false;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Background = Brushes.Transparent;
        IsHitTestVisible = false;

        if (hud)
        {
            _timer = new TextBlock
            {
                Text = "● REC 00:00",
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Content = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(245, 20, 22, 22)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(255, 58, 72)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Child = _timer
            };
        }
        else
        {
            Content = new Border { Background = new SolidColorBrush(Color.FromRgb(255, 40, 55)) };
        }

        Opened += (_, _) => NativeMethods.SetWindowDisplayAffinity(
            TryGetPlatformHandle()?.Handle ?? 0,
            NativeMethods.WdaExcludeFromCapture);
    }

    internal void UpdateElapsed(TimeSpan elapsed)
    {
        if (_timer is not null)
            _timer.Text = $"● REC {(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
    }
}
