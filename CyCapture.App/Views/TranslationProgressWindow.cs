using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CyCapture.Models;
using CyCapture.Platform.Windows;
using CyCapture.Services;

namespace CyCapture.Views;

internal sealed class TranslationProgressWindow : Window
{
    private readonly TextBlock _status;
    private readonly ProgressBar _progress;
    private readonly CancellationTokenSource _cancellation;
    private readonly CaptureSelection _selection;
    private bool _completed;

    internal TranslationProgressWindow(
        CancellationTokenSource cancellation,
        CaptureSelection selection,
        bool translate)
    {
        _cancellation = cancellation;
        _selection = selection;
        Title = translate
            ? UiLocalization.Text("CyCapture · Traduction", "CyCapture · Translation")
            : UiLocalization.Text("CyCapture · Copie du texte", "CyCapture · Copy text");
        Width = 410;
        Height = 155;
        CanResize = false;
        ShowInTaskbar = false;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Background = new SolidColorBrush(Color.FromRgb(8, 15, 14));
        _status = new TextBlock
        {
            Text = translate
                ? UiLocalization.Text("Préparation de la traduction locale…", "Preparing local translation…")
                : UiLocalization.Text("Préparation de la reconnaissance locale…", "Preparing local text recognition…"),
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.White
        };
        _progress = new ProgressBar
        {
            IsIndeterminate = true,
            Height = 7,
            Foreground = new SolidColorBrush(Color.FromRgb(155, 255, 40))
        };
        var cancel = new Button
        {
            Content = UiLocalization.Text("Annuler", "Cancel"),
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(16, 7)
        };
        cancel.Click += (_, _) => cancellation.Cancel();
        Content = new Border
        {
            Padding = new Thickness(20),
            Child = new StackPanel
            {
                Spacing = 14,
                Children =
                {
                    new TextBlock
                    {
                        Text = translate
                            ? UiLocalization.Text("TRADUCTION DE LA ZONE", "TRANSLATING AREA")
                            : UiLocalization.Text("COPIE DU TEXTE DE LA ZONE", "COPYING TEXT FROM AREA"),
                        FontSize = 11,
                        FontWeight = FontWeight.Bold,
                        Foreground = new SolidColorBrush(Color.FromRgb(155, 255, 40)),
                        LetterSpacing = 1.2
                    },
                    _status,
                    _progress,
                    cancel
                }
            }
        };
        Opened += OnOpened;
        Closed += (_, _) =>
        {
            if (!_completed) _cancellation.Cancel();
        };
    }

    private void OnOpened(object? sender, EventArgs args)
    {
        Position = CalculatePosition(_selection, Width, Height);
        NativeMethods.SetWindowDisplayAffinity(
            TryGetPlatformHandle()?.Handle ?? 0,
            NativeMethods.WdaExcludeFromCapture);
        Activate();
    }

    internal static PixelPoint CalculatePosition(CaptureSelection selection, double width, double height)
    {
        var scale = selection.Monitor.Scale > 0 ? selection.Monitor.Scale : 1;
        var pixelWidth = (int)Math.Ceiling(width * scale);
        var pixelHeight = (int)Math.Ceiling(height * scale);
        return new PixelPoint(
            selection.Bounds.X + (selection.Bounds.Width - pixelWidth) / 2,
            selection.Bounds.Y + (selection.Bounds.Height - pixelHeight) / 2);
    }

    internal void CompleteAndClose()
    {
        _completed = true;
        Close();
    }

    internal void Report(TranslationProgress progress)
    {
        if (!string.IsNullOrWhiteSpace(progress.Message)) _status.Text = progress.Message;
        if (progress.Fraction is { } fraction)
        {
            _progress.IsIndeterminate = false;
            _progress.Minimum = 0;
            _progress.Maximum = 1;
            _progress.Value = Math.Clamp(fraction, 0, 1);
        }
        else
        {
            _progress.IsIndeterminate = true;
        }
    }
}
