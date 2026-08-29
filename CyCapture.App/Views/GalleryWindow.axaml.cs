using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CyCapture.Models;
using CyCapture.Platform.Windows;
using CyCapture.Services;
using LibVLCSharp.Avalonia;
using LibVLCSharp.Shared;
using VlcMedia = LibVLCSharp.Shared.Media;
using VlcMediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace CyCapture.Views;

public sealed partial class GalleryWindow : Window
{
    private enum DateFilter
    {
        Today,
        Yesterday,
        All
    }

    private readonly Preferences _preferences;
    private readonly HistoryService _history;
    private readonly List<Bitmap> _thumbnails = [];
    private readonly DispatcherTimer _playbackTimer;
    private readonly TextBlock _galleryStatusText;
    private readonly TextBlock _outputPathText;
    private readonly WrapPanel _galleryPanel;
    private readonly Border _emptyGalleryPanel;
    private readonly TextBlock _emptyGalleryText;
    private readonly Button _todayFilterButton;
    private readonly Button _yesterdayFilterButton;
    private readonly Button _allFilterButton;
    private readonly Border _viewerOverlay;
    private readonly Image _viewerImage;
    private readonly VideoView _viewerVideoView;
    private readonly Border _viewerAudioArtwork;
    private readonly TextBlock _viewerErrorText;
    private readonly TextBlock _viewerTitleText;
    private readonly TextBlock _viewerDetailsText;
    private readonly Grid _playbackControls;
    private readonly Button _playPauseButton;
    private readonly Slider _playbackSlider;
    private readonly TextBlock _playbackTimeText;
    private List<HistoryEntry> _allEntries = [];
    private List<HistoryEntry> _visibleEntries = [];
    private DateFilter _dateFilter = DateFilter.Today;
    private HistoryEntry? _selectedEntry;
    private Bitmap? _viewerBitmap;
    private LibVLC? _libVlc;
    private VlcMediaPlayer? _mediaPlayer;
    private VlcMedia? _activeMedia;
    private bool _updatingPlaybackSlider;
    private bool _restoreWindowAfterViewer;
    private int _refreshRevision;

    public GalleryWindow() : this(new Preferences(), new HistoryService())
    {
    }

    internal GalleryWindow(Preferences preferences, HistoryService history)
    {
        _preferences = preferences;
        _history = history;
        AvaloniaXamlLoader.Load(this);
        WindowsWindowAppearance.Attach(this);
        _galleryStatusText = RequireControl<TextBlock>("GalleryStatusText");
        _outputPathText = RequireControl<TextBlock>("OutputPathText");
        _galleryPanel = RequireControl<WrapPanel>("GalleryPanel");
        _emptyGalleryPanel = RequireControl<Border>("EmptyGalleryPanel");
        _emptyGalleryText = RequireControl<TextBlock>("EmptyGalleryText");
        _todayFilterButton = RequireControl<Button>("TodayFilterButton");
        _yesterdayFilterButton = RequireControl<Button>("YesterdayFilterButton");
        _allFilterButton = RequireControl<Button>("AllFilterButton");
        _viewerOverlay = RequireControl<Border>("ViewerOverlay");
        _viewerImage = RequireControl<Image>("ViewerImage");
        _viewerVideoView = RequireControl<VideoView>("ViewerVideoView");
        _viewerAudioArtwork = RequireControl<Border>("ViewerAudioArtwork");
        _viewerErrorText = RequireControl<TextBlock>("ViewerErrorText");
        _viewerTitleText = RequireControl<TextBlock>("ViewerTitleText");
        _viewerDetailsText = RequireControl<TextBlock>("ViewerDetailsText");
        _playbackControls = RequireControl<Grid>("PlaybackControls");
        _playPauseButton = RequireControl<Button>("PlayPauseButton");
        _playbackSlider = RequireControl<Slider>("PlaybackSlider");
        _playbackTimeText = RequireControl<TextBlock>("PlaybackTimeText");
        _outputPathText.Text = preferences.EffectiveOutputDirectory;
        _playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _playbackTimer.Tick += (_, _) => UpdatePlaybackPosition();
        Opened += async (_, _) => await RefreshGalleryAsync();
        Closed += (_, _) => DisposeResources();
        UpdateDateFilterButtons();
    }

    internal async Task RefreshGalleryAsync()
    {
        var revision = ++_refreshRevision;
        _galleryStatusText.Text = "CHARGEMENT DES CAPTURES…";
        _outputPathText.Text = _preferences.EffectiveOutputDirectory;
        var entries = await _history.ReadAvailableAsync(_preferences.EffectiveOutputDirectory);
        if (revision != _refreshRevision || !IsVisible) return;

        _allEntries = entries;
        ApplyDateFilter();
    }

    private void ApplyDateFilter()
    {
        var today = DateTime.Today;
        _visibleEntries = _allEntries.Where(entry => _dateFilter switch
            {
                DateFilter.Today => entry.CreatedAt.LocalDateTime.Date == today,
                DateFilter.Yesterday => entry.CreatedAt.LocalDateTime.Date == today.AddDays(-1),
                _ => true
            })
            .OrderByDescending(entry => entry.CreatedAt)
            .ToList();

        DisposeThumbnails();
        _galleryPanel.Children.Clear();
        foreach (var entry in _visibleEntries)
            _galleryPanel.Children.Add(CreateCard(entry));

        _emptyGalleryPanel.IsVisible = _visibleEntries.Count == 0;
        _emptyGalleryText.Text = _allEntries.Count == 0
            ? "Vos prochaines images, vidéos, GIF et captures audio apparaîtront ici."
            : _dateFilter switch
            {
                DateFilter.Today => "Aucune capture enregistrée aujourd’hui.",
                DateFilter.Yesterday => "Aucune capture enregistrée hier.",
                _ => "Aucune capture disponible."
            };
        _galleryStatusText.Text = BuildStatusText();
        UpdateDateFilterButtons();
    }

    private string BuildStatusText()
    {
        if (_dateFilter == DateFilter.All)
        {
            return _visibleEntries.Count switch
            {
                0 => "AUCUNE CAPTURE",
                1 => "1 CAPTURE AU TOTAL",
                _ => $"{_visibleEntries.Count} CAPTURES AU TOTAL"
            };
        }

        var period = _dateFilter switch
        {
            DateFilter.Today => "AUJOURD’HUI",
            DateFilter.Yesterday => "HIER",
            _ => string.Empty
        };
        return _visibleEntries.Count switch
        {
            0 => $"AUCUNE CAPTURE {period}",
            1 => $"1 CAPTURE {period} · {_allEntries.Count} AU TOTAL",
            _ => $"{_visibleEntries.Count} CAPTURES {period} · {_allEntries.Count} AU TOTAL"
        };
    }

    private void UpdateDateFilterButtons()
    {
        SetFilterButtonState(_todayFilterButton, _dateFilter == DateFilter.Today);
        SetFilterButtonState(_yesterdayFilterButton, _dateFilter == DateFilter.Yesterday);
        SetFilterButtonState(_allFilterButton, _dateFilter == DateFilter.All);
    }

    private static void SetFilterButtonState(Button button, bool active)
    {
        button.Background = new SolidColorBrush(active ? Color.FromRgb(61, 86, 67) : Color.FromRgb(24, 32, 31));
        button.BorderBrush = new SolidColorBrush(active ? Color.FromRgb(155, 255, 40) : Color.FromRgb(38, 51, 49));
        button.BorderThickness = new Thickness(1);
        button.Foreground = new SolidColorBrush(active ? Color.FromRgb(244, 248, 244) : Color.FromRgb(170, 183, 180));
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
            MaxWidth = 234,
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
                CaptureMode.Audio => "▶  Écouter",
                _ => "⛶  Agrandir"
            },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };
        openButton.Click += (_, args) =>
        {
            args.Handled = true;
            OpenViewer(entry);
        };

        var copyButton = new Button { Content = "Copier", HorizontalAlignment = HorizontalAlignment.Stretch };
        copyButton.Click += (_, args) =>
        {
            args.Handled = true;
            CopyEntry(entry);
        };

        var revealButton = new Button { Content = "Dossier", HorizontalAlignment = HorizontalAlignment.Stretch };
        revealButton.Click += (_, args) =>
        {
            args.Handled = true;
            RevealFile(entry.Path);
        };

        void OpenFromPointer(object? _, PointerPressedEventArgs args)
        {
            if (args.GetCurrentPoint(preview).Properties.IsLeftButtonPressed)
                OpenViewer(entry);
        }
        preview.Cursor = new Cursor(StandardCursorType.Hand);
        preview.PointerPressed += OpenFromPointer;
        name.Cursor = new Cursor(StandardCursorType.Hand);
        name.PointerPressed += OpenFromPointer;

        return new Border
        {
            Width = 270,
            Margin = new Thickness(0, 0, 14, 14),
            Padding = new Thickness(10),
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(Color.FromRgb(17, 24, 23)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(38, 51, 49)),
            BorderThickness = new Thickness(1),
            Cursor = new Cursor(StandardCursorType.Hand),
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
                        ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
                        ColumnSpacing = 7,
                        Children =
                        {
                            openButton,
                            new Border { [Grid.ColumnProperty] = 1, Child = copyButton },
                            new Border { [Grid.ColumnProperty] = 2, Child = revealButton }
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
            Height = 138,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromRgb(7, 11, 11)),
            ClipToBounds = true
        };

        if (entry.Mode is CaptureMode.Image or CaptureMode.Gif)
        {
            try
            {
                using var stream = File.OpenRead(entry.Path);
                var bitmap = Bitmap.DecodeToWidth(stream, 248, BitmapInterpolationMode.HighQuality);
                _thumbnails.Add(bitmap);
                container.Child = new Image { Source = bitmap, Stretch = Stretch.Uniform };
                return container;
            }
            catch
            {
                // Un fichier verrouillé ou endommagé conserve une carte exploitable avec son icône.
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
                    Text = entry.Mode switch
                    {
                        CaptureMode.Video => "▶",
                        CaptureMode.Gif => "GIF",
                        CaptureMode.Audio => "♪",
                        _ => "▣"
                    },
                    FontSize = entry.Mode == CaptureMode.Gif ? 25 : 36,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(entry.Mode switch
                    {
                        CaptureMode.Video => Color.FromRgb(255, 91, 105),
                        CaptureMode.Audio => Color.FromRgb(197, 140, 255),
                        _ => Color.FromRgb(155, 255, 40)
                    }),
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new TextBlock { Text = ModeLabel(entry.Mode), Foreground = new SolidColorBrush(Color.FromRgb(142, 156, 153)), FontSize = 10 }
            }
        };
        return container;
    }

    private void OpenViewer(HistoryEntry entry)
    {
        if (!File.Exists(entry.Path))
        {
            _galleryStatusText.Text = "FICHIER INTROUVABLE";
            return;
        }

        if (!_viewerOverlay.IsVisible)
        {
            _restoreWindowAfterViewer = WindowState == WindowState.Normal;
            if (_restoreWindowAfterViewer) WindowState = WindowState.Maximized;
        }
        StopActiveMedia();
        _selectedEntry = entry;
        _viewerOverlay.IsVisible = true;
        _viewerTitleText.Text = Path.GetFileName(entry.Path);
        _viewerDetailsText.Text = $"{ModeLabel(entry.Mode)} · {entry.CreatedAt.LocalDateTime:dd/MM/yyyy HH:mm} · {FormatSize(new FileInfo(entry.Path).Length)}";
        _viewerImage.IsVisible = false;
        _viewerVideoView.IsVisible = false;
        _viewerAudioArtwork.IsVisible = false;
        _viewerErrorText.IsVisible = false;
        _playbackControls.IsVisible = entry.Mode != CaptureMode.Image;

        try
        {
            if (entry.Mode == CaptureMode.Image)
            {
                _viewerBitmap = new Bitmap(entry.Path);
                _viewerImage.Source = _viewerBitmap;
                _viewerImage.IsVisible = true;
                return;
            }

            EnsureMediaPlayer();
            _viewerVideoView.IsVisible = entry.Mode is CaptureMode.Video or CaptureMode.Gif;
            _viewerAudioArtwork.IsVisible = entry.Mode == CaptureMode.Audio;
            _activeMedia = new VlcMedia(_libVlc!, new Uri(entry.Path));
            if (entry.Mode == CaptureMode.Gif) _activeMedia.AddOption(":input-repeat=-1");
            if (_mediaPlayer!.Play(_activeMedia))
            {
                _playPauseButton.Content = "Pause";
                _playbackTimer.Start();
            }
            else
                ShowViewerError("La lecture de ce fichier n’a pas pu démarrer.");
        }
        catch (Exception error)
        {
            ShowViewerError($"Lecture impossible : {error.Message}");
        }
    }

    private void EnsureMediaPlayer()
    {
        if (_mediaPlayer is not null) return;
        Core.Initialize();
        _libVlc = new LibVLC("--quiet", "--no-video-title-show");
        _mediaPlayer = new VlcMediaPlayer(_libVlc);
        _mediaPlayer.EndReached += (_, _) => Dispatcher.UIThread.Post(() =>
        {
            _playPauseButton.Content = "Lire";
            UpdatePlaybackPosition();
        });
        _mediaPlayer.EncounteredError += (_, _) => Dispatcher.UIThread.Post(() =>
            ShowViewerError("Le moteur multimédia ne parvient pas à lire ce fichier."));
        _viewerVideoView.MediaPlayer = _mediaPlayer;
    }

    private void ShowViewerError(string message)
    {
        _playbackTimer.Stop();
        _playbackControls.IsVisible = false;
        _viewerVideoView.IsVisible = false;
        _viewerAudioArtwork.IsVisible = false;
        _viewerImage.IsVisible = false;
        _viewerErrorText.Text = message;
        _viewerErrorText.IsVisible = true;
    }

    private void UpdatePlaybackPosition()
    {
        if (_mediaPlayer is null) return;
        var length = Math.Max(0, _mediaPlayer.Length);
        var time = Math.Clamp(_mediaPlayer.Time, 0, length == 0 ? long.MaxValue : length);
        _updatingPlaybackSlider = true;
        _playbackSlider.Value = length > 0 ? time * 1000d / length : 0;
        _updatingPlaybackSlider = false;
        _playbackTimeText.Text = $"{FormatDuration(time)} / {FormatDuration(length)}";
        _playPauseButton.Content = _mediaPlayer.IsPlaying ? "Pause" : "Lire";
    }

    private void StopActiveMedia()
    {
        _playbackTimer.Stop();
        try { _mediaPlayer?.Stop(); }
        catch { }
        _activeMedia?.Dispose();
        _activeMedia = null;
        _viewerBitmap?.Dispose();
        _viewerBitmap = null;
        _viewerImage.Source = null;
        _updatingPlaybackSlider = true;
        _playbackSlider.Value = 0;
        _updatingPlaybackSlider = false;
        _playbackTimeText.Text = "00:00 / 00:00";
    }

    private void CloseViewer()
    {
        if (!_viewerOverlay.IsVisible) return;
        StopActiveMedia();
        _selectedEntry = null;
        _viewerOverlay.IsVisible = false;
        if (_restoreWindowAfterViewer) WindowState = WindowState.Normal;
        _restoreWindowAfterViewer = false;
    }

    private void CopyEntry(HistoryEntry entry)
    {
        try
        {
            if (entry.Mode == CaptureMode.Image)
                WindowsClipboard.CopyImageAndFile(entry.Path);
            else
                WindowsClipboard.CopyFile(entry.Path);
            var message = $"COPIÉ DANS LE PRESSE-PAPIERS · {Path.GetFileName(entry.Path)}";
            _galleryStatusText.Text = message;
            if (_viewerOverlay.IsVisible) _viewerDetailsText.Text = message;
        }
        catch (Exception error)
        {
            var message = $"COPIE IMPOSSIBLE · {error.Message}";
            _galleryStatusText.Text = message;
            if (_viewerOverlay.IsVisible) _viewerDetailsText.Text = message;
        }
    }

    private void SetDateFilter(DateFilter filter)
    {
        if (_dateFilter == filter) return;
        _dateFilter = filter;
        ApplyDateFilter();
    }

    private async void RefreshClick(object? sender, RoutedEventArgs args) => await RefreshGalleryAsync();
    private void TodayFilterClick(object? sender, RoutedEventArgs args) => SetDateFilter(DateFilter.Today);
    private void YesterdayFilterClick(object? sender, RoutedEventArgs args) => SetDateFilter(DateFilter.Yesterday);
    private void AllFilterClick(object? sender, RoutedEventArgs args) => SetDateFilter(DateFilter.All);
    private void CloseViewerClick(object? sender, RoutedEventArgs args) => CloseViewer();
    private void CopyViewerClick(object? sender, RoutedEventArgs args)
    {
        if (_selectedEntry is not null) CopyEntry(_selectedEntry);
    }
    private void RevealViewerClick(object? sender, RoutedEventArgs args)
    {
        if (_selectedEntry is not null) RevealFile(_selectedEntry.Path);
    }

    private void PreviousMediaClick(object? sender, RoutedEventArgs args) => NavigateViewer(-1);
    private void NextMediaClick(object? sender, RoutedEventArgs args) => NavigateViewer(1);

    private void NavigateViewer(int offset)
    {
        if (_visibleEntries.Count == 0) return;
        var index = _selectedEntry is null ? -1 : _visibleEntries.FindIndex(entry =>
            entry.Path.Equals(_selectedEntry.Path, StringComparison.OrdinalIgnoreCase));
        index = index < 0 ? 0 : (index + offset + _visibleEntries.Count) % _visibleEntries.Count;
        OpenViewer(_visibleEntries[index]);
    }

    private void PlayPauseClick(object? sender, RoutedEventArgs args)
    {
        if (_mediaPlayer is null) return;
        if (_mediaPlayer.IsPlaying)
            _mediaPlayer.Pause();
        else
        {
            if (_mediaPlayer.Position >= .995f) _mediaPlayer.Position = 0;
            _mediaPlayer.Play();
        }
        UpdatePlaybackPosition();
    }

    private void StopPlaybackClick(object? sender, RoutedEventArgs args)
    {
        if (_mediaPlayer is null) return;
        _mediaPlayer.Stop();
        UpdatePlaybackPosition();
    }

    private void PlaybackValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs args)
    {
        if (_updatingPlaybackSlider || _mediaPlayer is null || _mediaPlayer.Length <= 0) return;
        _mediaPlayer.Position = (float)Math.Clamp(args.NewValue / 1000d, 0, 1);
        UpdatePlaybackPosition();
    }

    private void WindowKeyDown(object? sender, KeyEventArgs args)
    {
        if (!_viewerOverlay.IsVisible) return;
        switch (args.Key)
        {
            case Key.Escape:
                CloseViewer();
                args.Handled = true;
                break;
            case Key.Left:
                NavigateViewer(-1);
                args.Handled = true;
                break;
            case Key.Right:
                NavigateViewer(1);
                args.Handled = true;
                break;
            case Key.Space:
                PlayPauseClick(null, new RoutedEventArgs());
                args.Handled = true;
                break;
        }
    }

    private void OpenFolderClick(object? sender, RoutedEventArgs args)
    {
        Directory.CreateDirectory(_preferences.EffectiveOutputDirectory);
        Process.Start(new ProcessStartInfo("explorer.exe", _preferences.EffectiveOutputDirectory) { UseShellExecute = true });
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

    private void DisposeResources()
    {
        StopActiveMedia();
        DisposeThumbnails();
        _viewerVideoView.MediaPlayer = null;
        _mediaPlayer?.Dispose();
        _mediaPlayer = null;
        _libVlc?.Dispose();
        _libVlc = null;
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
        CaptureMode.Audio => "AUDIO",
        _ => "IMAGE"
    };

    private static string FormatSize(long bytes) => bytes switch
    {
        >= 1_048_576 => $"{bytes / 1_048_576d:0.0} Mo",
        >= 1_024 => $"{bytes / 1_024d:0.0} Ko",
        _ => $"{bytes} o"
    };

    private static string FormatDuration(long milliseconds)
    {
        var duration = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}"
            : $"{duration.Minutes:00}:{duration.Seconds:00}";
    }

    private T RequireControl<T>(string name) where T : Control =>
        this.FindControl<T>(name) ?? throw new InvalidOperationException($"Contrôle Avalonia introuvable : {name}");
}
