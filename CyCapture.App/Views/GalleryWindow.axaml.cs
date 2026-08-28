using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CyCapture.Models;
using CyCapture.Services;

namespace CyCapture.Views;

public sealed partial class GalleryWindow : Window
{
    private readonly Preferences _preferences;
    private readonly HistoryService _history;
    private readonly List<Bitmap> _thumbnails = [];
    private readonly TextBlock _galleryStatusText;
    private readonly TextBlock _outputPathText;
    private readonly WrapPanel _galleryPanel;
    private readonly Border _emptyGalleryPanel;
    private int _refreshRevision;

    public GalleryWindow() : this(new Preferences(), new HistoryService())
    {
    }

    internal GalleryWindow(Preferences preferences, HistoryService history)
    {
        _preferences = preferences;
        _history = history;
        AvaloniaXamlLoader.Load(this);
        _galleryStatusText = RequireControl<TextBlock>("GalleryStatusText");
        _outputPathText = RequireControl<TextBlock>("OutputPathText");
        _galleryPanel = RequireControl<WrapPanel>("GalleryPanel");
        _emptyGalleryPanel = RequireControl<Border>("EmptyGalleryPanel");
        _outputPathText.Text = preferences.EffectiveOutputDirectory;
        Opened += async (_, _) => await RefreshGalleryAsync();
        Closed += (_, _) => DisposeThumbnails();
    }

    internal async Task RefreshGalleryAsync()
    {
        var revision = ++_refreshRevision;
        _galleryStatusText.Text = "CHARGEMENT DES CAPTURES…";
        _outputPathText.Text = _preferences.EffectiveOutputDirectory;
        var entries = await _history.ReadAvailableAsync(_preferences.EffectiveOutputDirectory);
        if (revision != _refreshRevision || !IsVisible) return;

        DisposeThumbnails();
        _galleryPanel.Children.Clear();
        foreach (var entry in entries)
            _galleryPanel.Children.Add(CreateCard(entry));

        _emptyGalleryPanel.IsVisible = entries.Count == 0;
        _galleryStatusText.Text = entries.Count switch
        {
            0 => "AUCUNE CAPTURE",
            1 => "1 CAPTURE DISPONIBLE",
            _ => $"{entries.Count} CAPTURES DISPONIBLES"
        };
    }

    private Control CreateCard(HistoryEntry entry)
    {
        var preview = CreatePreview(entry);
        var file = new FileInfo(entry.Path);
        var name = new TextBlock
        {
            Text = file.Name,
            FontWeight = FontWeight.Bold,
            FontSize = 12,
            MaxWidth = 224,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        ToolTip.SetTip(name, entry.Path);

        var details = entry.Width > 0 && entry.Height > 0
            ? $"{ModeLabel(entry.Mode)} · {entry.Width} × {entry.Height}"
            : $"{ModeLabel(entry.Mode)} · {FormatSize(file.Length)}";
        var openButton = new Button
        {
            Content = entry.Mode switch
            {
                CaptureMode.Video => "▶  Lire",
                CaptureMode.Gif => "▶  Lire le GIF",
                _ => "↗  Ouvrir"
            },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        openButton.Click += (_, _) => OpenFile(entry.Path);

        var revealButton = new Button
        {
            Content = "Dossier",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        revealButton.Click += (_, _) => RevealFile(entry.Path);

        return new Border
        {
            Width = 260,
            Margin = new Thickness(0, 0, 14, 14),
            Padding = new Thickness(10),
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromRgb(17, 24, 23)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(38, 51, 49)),
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    preview,
                    name,
                    new TextBlock { Text = details, Foreground = new SolidColorBrush(Color.FromRgb(142, 156, 153)), FontSize = 10 },
                    new TextBlock { Text = entry.CreatedAt.LocalDateTime.ToString("dd/MM/yyyy · HH:mm"), Foreground = new SolidColorBrush(Color.FromRgb(112, 126, 123)), FontSize = 10 },
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                        ColumnSpacing = 8,
                        Children =
                        {
                            openButton,
                            new Border { [Grid.ColumnProperty] = 1, Child = revealButton }
                        }
                    }
                }
            }
        };
    }

    private Control CreatePreview(HistoryEntry entry)
    {
        var container = new Border
        {
            Height = 132,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromRgb(7, 11, 11)),
            ClipToBounds = true
        };

        if (entry.Mode is CaptureMode.Image or CaptureMode.Gif)
        {
            try
            {
                using var stream = File.OpenRead(entry.Path);
                var bitmap = Bitmap.DecodeToWidth(stream, 238, BitmapInterpolationMode.HighQuality);
                _thumbnails.Add(bitmap);
                container.Child = new Image { Source = bitmap, Stretch = Stretch.Uniform };
                return container;
            }
            catch
            {
                // A broken or temporarily locked file falls back to its media icon.
            }
        }

        container.Child = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 5,
            Children =
            {
                new TextBlock
                {
                    Text = entry.Mode == CaptureMode.Video ? "▶" : entry.Mode == CaptureMode.Gif ? "GIF" : "▣",
                    FontSize = entry.Mode == CaptureMode.Gif ? 25 : 36,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(entry.Mode == CaptureMode.Video
                        ? Color.FromRgb(255, 91, 105)
                        : Color.FromRgb(155, 255, 40)),
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new TextBlock { Text = ModeLabel(entry.Mode), Foreground = new SolidColorBrush(Color.FromRgb(142, 156, 153)), FontSize = 10 }
            }
        };
        return container;
    }

    private async void RefreshClick(object? sender, RoutedEventArgs args) => await RefreshGalleryAsync();

    private void OpenFolderClick(object? sender, RoutedEventArgs args)
    {
        Directory.CreateDirectory(_preferences.EffectiveOutputDirectory);
        Process.Start(new ProcessStartInfo("explorer.exe", _preferences.EffectiveOutputDirectory) { UseShellExecute = true });
    }

    private void OpenFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception error)
        {
            _galleryStatusText.Text = $"OUVERTURE IMPOSSIBLE · {error.Message}";
        }
    }

    private void RevealFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        }
        catch (Exception error)
        {
            _galleryStatusText.Text = $"DOSSIER INDISPONIBLE · {error.Message}";
        }
    }

    private void DisposeThumbnails()
    {
        foreach (var thumbnail in _thumbnails) thumbnail.Dispose();
        _thumbnails.Clear();
    }

    private static string ModeLabel(CaptureMode mode) => mode switch
    {
        CaptureMode.Video => "VIDÉO",
        CaptureMode.Gif => "GIF",
        _ => "IMAGE"
    };

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_048_576 => $"{bytes / 1_048_576d:0.0} Mo",
        >= 1_024 => $"{bytes / 1_024d:0.0} Ko",
        _ => $"{bytes} o"
    };

    private T RequireControl<T>(string name) where T : Control =>
        this.FindControl<T>(name) ?? throw new InvalidOperationException($"Contrôle Avalonia introuvable : {name}");
}
