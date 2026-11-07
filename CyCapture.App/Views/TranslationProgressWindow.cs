using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CyCapture.Models;
using CyCapture.Services;

namespace CyCapture.Views;

internal sealed class TranslationProgressWindow : Window
{
    private readonly TextBlock _status;
    private readonly ProgressBar _progress;
    private readonly CancellationTokenSource _cancellation;
    private bool _completed;

    internal TranslationProgressWindow(CancellationTokenSource cancellation)
    {
        _cancellation = cancellation;
        Title = UiLocalization.Text("CyCapture · Traduction", "CyCapture · Translation");
        Width = 410;
        Height = 155;
        CanResize = false;
        ShowInTaskbar = false;
        Topmost = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(8, 15, 14));
        _status = new TextBlock
        {
            Text = UiLocalization.Text("Préparation de la traduction locale…", "Preparing local translation…"),
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
                        Text = UiLocalization.Text("TRADUCTION DE LA ZONE", "TRANSLATING AREA"),
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
        Closed += (_, _) =>
        {
            if (!_completed) _cancellation.Cancel();
        };
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
