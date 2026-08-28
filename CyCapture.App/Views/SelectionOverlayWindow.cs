using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CyCapture.Models;
using CyCapture.Platform.Windows;

namespace CyCapture.Views;

internal sealed class SelectionOverlayWindow : Window
{
    private readonly MonitorDescriptor _monitor;
    private readonly TextBlock _label;

    internal SelectionOverlayWindow(
        Bitmap screenshot,
        MonitorDescriptor monitor,
        IReadOnlyList<SelectableRegion> regions,
        CaptureSelectionMode selectionMode)
    {
        _monitor = monitor;
        Title = $"CyCapture · {SelectionModeLabel(selectionMode)}";
        Width = monitor.Bounds.Width / monitor.Scale;
        Height = monitor.Bounds.Height / monitor.Scale;
        Position = new PixelPoint(monitor.Bounds.X, monitor.Bounds.Y);
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowDecorations = Avalonia.Controls.WindowDecorations.None;
        CanResize = false;
        ShowInTaskbar = false;
        Topmost = true;
        Background = Brushes.Black;

        var surface = new SelectionSurface(screenshot, monitor, regions, selectionMode);
        surface.SelectionCompleted += (_, selection) => SelectionCompleted?.Invoke(this, selection);

        _label = new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            IsHitTestVisible = false
        };
        var labelBorder = new Border
        {
            Name = "SelectionLabel",
            Background = new SolidColorBrush(Color.FromArgb(238, 8, 14, 14)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(155, 255, 40)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Padding = new Thickness(9, 5),
            Child = _label,
            IsVisible = false,
            IsHitTestVisible = false
        };
        surface.HighlightChanged += (_, highlight) => UpdateLabel(highlight.Bounds, highlight.Label);

        var instruction = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 20, 0, 0),
            Padding = new Thickness(16, 9),
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(238, 8, 14, 14)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(120, 155, 255, 40)),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = SelectionInstruction(selectionMode),
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White
            },
            IsHitTestVisible = false
        };

        var canvas = new Canvas { IsHitTestVisible = false };
        canvas.Children.Add(labelBorder);
        var root = new Grid();
        root.Children.Add(surface);
        root.Children.Add(canvas);
        root.Children.Add(instruction);
        Content = root;

        Opened += (_, _) =>
        {
            NativeMethods.SetWindowDisplayAffinity(TryGetPlatformHandle()?.Handle ?? 0, NativeMethods.WdaExcludeFromCapture);
            surface.Focus();
        };
        KeyDown += (_, args) =>
        {
            if (args.Key == Key.Escape) Canceled?.Invoke(this, EventArgs.Empty);
        };

        void UpdateLabel(PixelBounds? bounds, string text)
        {
            if (bounds is null)
            {
                labelBorder.IsVisible = false;
                return;
            }

            var localX = (bounds.X - _monitor.Bounds.X) / _monitor.Scale;
            var localY = (bounds.Y - _monitor.Bounds.Y) / _monitor.Scale;
            _label.Text = $"{text}  ·  {bounds.Width} × {bounds.Height}";
            Canvas.SetLeft(labelBorder, Math.Clamp(localX, 8, Math.Max(8, Width - 380)));
            Canvas.SetTop(labelBorder, localY < 52 ? localY + 8 : localY - 38);
            labelBorder.IsVisible = true;
        }
    }

    internal event EventHandler<CaptureSelection>? SelectionCompleted;
    internal event EventHandler? Canceled;

    private static string SelectionModeLabel(CaptureSelectionMode mode) => mode switch
    {
        CaptureSelectionMode.Window => "Sélection de fenêtre",
        CaptureSelectionMode.Screen => "Sélection d’écran",
        CaptureSelectionMode.Region => "Zone libre",
        _ => "Sélection intelligente"
    };

    private static string SelectionInstruction(CaptureSelectionMode mode) => mode switch
    {
        CaptureSelectionMode.Window => "Cliquez la fenêtre à capturer · Échap pour annuler",
        CaptureSelectionMode.Screen => "Cliquez l’écran à capturer · Échap pour annuler",
        CaptureSelectionMode.Region => "Glissez pour dessiner la zone à capturer · Échap pour annuler",
        _ => "Cliquez une fenêtre ou un contenu · Glissez pour une zone libre · Échap pour annuler"
    };
}
