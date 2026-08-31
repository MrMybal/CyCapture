using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CyCapture.Models;
using CyCapture.Platform.Windows;
using CyCapture.Services;

namespace CyCapture.Views;

public sealed partial class SettingsWindow : Window
{
    private readonly Preferences _preferences;
    private readonly PostProcessingService _postProcessing;
    private readonly TextBlock _outputPathText;
    private readonly ComboBox _videoQualityCombo;
    private readonly ComboBox _videoEncodingQualityCombo;
    private readonly ComboBox _audioEncodingQualityCombo;
    private readonly ComboBox _audioOnlyEncodingQualityCombo;
    private readonly ComboBox _imageFormatCombo;
    private readonly ComboBox _imageEncodingQualityCombo;
    private readonly ComboBox _gifQualityCombo;
    private readonly ComboBox _selectionModeCombo;
    private readonly ComboBox _printScreenBehaviorCombo;
    private readonly ComboBox _printScreenHoldDelayCombo;
    private readonly CheckBox _systemAudioCheck;
    private readonly CheckBox _microphoneCheck;
    private readonly CheckBox _audioOnlySystemCheck;
    private readonly CheckBox _audioOnlyMicrophoneCheck;
    private readonly CheckBox _clipboardCheck;
    private readonly CheckBox _videoClipboardCheck;
    private readonly CheckBox _quickAnnotationsCheck;
    private readonly CheckBox _frameCheck;
    private readonly CheckBox _gifFrameCheck;
    private readonly CheckBox _separateCaptureTypesCheck;
    private readonly CheckBox _dailyFoldersCheck;
    private readonly CheckBox _startupCheck;
    private readonly TextBlock _captureOrganizationPreview;
    private readonly TextBlock _startupStatusText;
    private readonly TextBlock _audioOnlySourceHint;
    private readonly StackPanel _pluginsPanel;
    private bool _initializing = true;
    private bool _changingPlugin;

    public SettingsWindow() : this(new Preferences(), new PostProcessingService())
    {
    }

    internal SettingsWindow(Preferences preferences, PostProcessingService postProcessing)
    {
        _preferences = preferences;
        _postProcessing = postProcessing;
        AvaloniaXamlLoader.Load(this);
        WindowsWindowAppearance.Attach(this);
        _outputPathText = RequireControl<TextBlock>("OutputPathText");
        _videoQualityCombo = RequireControl<ComboBox>("VideoQualityCombo");
        _videoEncodingQualityCombo = RequireControl<ComboBox>("VideoEncodingQualityCombo");
        _audioEncodingQualityCombo = RequireControl<ComboBox>("AudioEncodingQualityCombo");
        _audioOnlyEncodingQualityCombo = RequireControl<ComboBox>("AudioOnlyEncodingQualityCombo");
        _imageFormatCombo = RequireControl<ComboBox>("ImageFormatCombo");
        _imageEncodingQualityCombo = RequireControl<ComboBox>("ImageEncodingQualityCombo");
        _gifQualityCombo = RequireControl<ComboBox>("GifQualityCombo");
        _selectionModeCombo = RequireControl<ComboBox>("SelectionModeCombo");
        _printScreenBehaviorCombo = RequireControl<ComboBox>("PrintScreenBehaviorCombo");
        _printScreenHoldDelayCombo = RequireControl<ComboBox>("PrintScreenHoldDelayCombo");
        _systemAudioCheck = RequireControl<CheckBox>("SystemAudioCheck");
        _microphoneCheck = RequireControl<CheckBox>("MicrophoneCheck");
        _audioOnlySystemCheck = RequireControl<CheckBox>("AudioOnlySystemCheck");
        _audioOnlyMicrophoneCheck = RequireControl<CheckBox>("AudioOnlyMicrophoneCheck");
        _clipboardCheck = RequireControl<CheckBox>("ClipboardCheck");
        _videoClipboardCheck = RequireControl<CheckBox>("VideoClipboardCheck");
        _quickAnnotationsCheck = RequireControl<CheckBox>("QuickAnnotationsCheck");
        _frameCheck = RequireControl<CheckBox>("FrameCheck");
        _gifFrameCheck = RequireControl<CheckBox>("GifFrameCheck");
        _separateCaptureTypesCheck = RequireControl<CheckBox>("SeparateCaptureTypesCheck");
        _dailyFoldersCheck = RequireControl<CheckBox>("DailyFoldersCheck");
        _startupCheck = RequireControl<CheckBox>("StartupCheck");
        _captureOrganizationPreview = RequireControl<TextBlock>("CaptureOrganizationPreview");
        _startupStatusText = RequireControl<TextBlock>("StartupStatusText");
        _audioOnlySourceHint = RequireControl<TextBlock>("AudioOnlySourceHint");
        _pluginsPanel = RequireControl<StackPanel>("PluginsPanel");
        _outputPathText.Text = preferences.EffectiveOutputDirectory;
        SelectByTag(_videoQualityCombo, preferences.VideoQualityLevel.ToString());
        SelectByTag(_videoEncodingQualityCombo, preferences.VideoEncodingQuality.ToString());
        SelectByTag(_audioEncodingQualityCombo, preferences.AudioEncodingQuality.ToString());
        var audioOnly = preferences.GetAudioSettings(CaptureMode.Audio);
        SelectByTag(_audioOnlyEncodingQualityCombo, audioOnly.Quality.ToString());
        SelectByTag(_imageFormatCombo, preferences.ImageFormat);
        SelectByTag(_imageEncodingQualityCombo, preferences.ImageEncodingQuality.ToString());
        SelectByTag(_gifQualityCombo, preferences.GifQuality.ToString());
        SelectByTag(_selectionModeCombo, preferences.SelectionMode.ToString());
        SelectByTag(_printScreenBehaviorCombo, preferences.PrintScreenBehavior.ToString());
        SelectByTag(_printScreenHoldDelayCombo, preferences.PrintScreenHoldDelayMilliseconds.ToString());
        _systemAudioCheck.IsChecked = preferences.IncludeSystemAudio;
        _microphoneCheck.IsChecked = preferences.IncludeMicrophone;
        _audioOnlySystemCheck.IsChecked = audioOnly.SystemAudio;
        _audioOnlyMicrophoneCheck.IsChecked = audioOnly.Microphone;
        _clipboardCheck.IsChecked = preferences.CopyScreenshotsToClipboard;
        _videoClipboardCheck.IsChecked = preferences.CopyVideosToClipboard;
        _quickAnnotationsCheck.IsChecked = preferences.EnableQuickAnnotations;
        _frameCheck.IsChecked = preferences.ShowRecordingFrame;
        _gifFrameCheck.IsChecked = preferences.ShouldShowRecordingFrame(CaptureMode.Gif);
        _separateCaptureTypesCheck.IsChecked = preferences.SeparateCaptureTypes;
        _dailyFoldersCheck.IsChecked = preferences.CreateDailyCaptureFolders;
        preferences.StartWithWindows = WindowsStartup.IsEnabled();
        _startupCheck.IsChecked = preferences.StartWithWindows;
        UpdateCaptureOrganizationPreview();
        UpdateModeControls();
        _initializing = false;
        RenderPlugins();
        _postProcessing.PluginsChanged += PluginsChanged;
        Closed += (_, _) => _postProcessing.PluginsChanged -= PluginsChanged;
    }

    public event EventHandler<CaptureMode>? CaptureRequested;

    private void ImageClick(object? sender, RoutedEventArgs args) => Request(CaptureMode.Image);
    private void VideoClick(object? sender, RoutedEventArgs args) => Request(CaptureMode.Video);
    private void GifClick(object? sender, RoutedEventArgs args) => Request(CaptureMode.Gif);
    private void AudioClick(object? sender, RoutedEventArgs args) => Request(CaptureMode.Audio);

    private void Request(CaptureMode mode)
    {
        Hide();
        CaptureRequested?.Invoke(this, mode);
        Close();
    }

    private async void ChooseFolderClick(object? sender, RoutedEventArgs args)
    {
        var choices = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Dossier des captures CyCapture",
            AllowMultiple = false
        });
        var folder = choices.FirstOrDefault();
        if (folder is null) return;
        _preferences.OutputDirectory = folder.Path.LocalPath;
        _outputPathText.Text = _preferences.EffectiveOutputDirectory;
        await _preferences.SaveAsync();
    }

    private void OpenFolderClick(object? sender, RoutedEventArgs args)
    {
        Directory.CreateDirectory(_preferences.EffectiveOutputDirectory);
        Process.Start(new ProcessStartInfo("explorer.exe", _preferences.EffectiveOutputDirectory) { UseShellExecute = true });
    }

    private void OpenPluginsFolderClick(object? sender, RoutedEventArgs args)
    {
        Directory.CreateDirectory(_postProcessing.PluginsDirectory);
        Process.Start(new ProcessStartInfo("explorer.exe", _postProcessing.PluginsDirectory) { UseShellExecute = true });
    }

    private void SelectionPreferenceChanged(object? sender, SelectionChangedEventArgs args) => SavePreferences();
    private void CheckedPreferenceChanged(object? sender, RoutedEventArgs args) => SavePreferences();

    private async void SavePreferences()
    {
        if (_initializing) return;
        if (_videoQualityCombo.SelectedItem is ComboBoxItem videoQuality
            && Enum.TryParse<VideoQuality>(videoQuality.Tag?.ToString(), out var videoQualityValue))
            _preferences.VideoQualityLevel = videoQualityValue;
        if (_videoEncodingQualityCombo.SelectedItem is ComboBoxItem videoEncoding
            && Enum.TryParse<VideoEncodingQuality>(videoEncoding.Tag?.ToString(), out var videoEncodingValue))
            _preferences.VideoEncodingQuality = videoEncodingValue;
        if (_audioEncodingQualityCombo.SelectedItem is ComboBoxItem audioEncoding
            && Enum.TryParse<AudioEncodingQuality>(audioEncoding.Tag?.ToString(), out var audioEncodingValue))
            _preferences.AudioEncodingQuality = audioEncodingValue;
        if (_audioOnlyEncodingQualityCombo.SelectedItem is ComboBoxItem audioOnlyEncoding
            && Enum.TryParse<AudioEncodingQuality>(audioOnlyEncoding.Tag?.ToString(), out var audioOnlyEncodingValue))
            _preferences.AudioOnlyEncodingQuality = audioOnlyEncodingValue;
        if (_imageFormatCombo.SelectedItem is ComboBoxItem imageFormat)
            _preferences.ImageFormat = imageFormat.Tag?.ToString() ?? "png";
        if (_imageEncodingQualityCombo.SelectedItem is ComboBoxItem imageEncoding
            && Enum.TryParse<ImageEncodingQuality>(imageEncoding.Tag?.ToString(), out var imageEncodingValue))
            _preferences.ImageEncodingQuality = imageEncodingValue;
        if (_gifQualityCombo.SelectedItem is ComboBoxItem quality
            && Enum.TryParse<GifQuality>(quality.Tag?.ToString(), out var qualityValue))
            _preferences.GifQuality = qualityValue;
        if (_selectionModeCombo.SelectedItem is ComboBoxItem selectionMode
            && Enum.TryParse<CaptureSelectionMode>(selectionMode.Tag?.ToString(), out var selectionModeValue))
            _preferences.SelectionMode = selectionModeValue;
        if (_printScreenBehaviorCombo.SelectedItem is ComboBoxItem printScreenBehavior
            && Enum.TryParse<PrintScreenBehavior>(printScreenBehavior.Tag?.ToString(), out var printScreenBehaviorValue))
            _preferences.PrintScreenBehavior = printScreenBehaviorValue;
        if (_printScreenHoldDelayCombo.SelectedItem is ComboBoxItem holdDelay
            && int.TryParse(holdDelay.Tag?.ToString(), out var holdDelayMilliseconds))
            _preferences.PrintScreenHoldDelayMilliseconds = holdDelayMilliseconds;
        _preferences.IncludeSystemAudio = _systemAudioCheck.IsChecked == true;
        _preferences.IncludeMicrophone = _microphoneCheck.IsChecked == true;
        _preferences.AudioOnlyIncludeSystemAudio = _audioOnlySystemCheck.IsChecked == true;
        _preferences.AudioOnlyIncludeMicrophone = _audioOnlyMicrophoneCheck.IsChecked == true;
        _preferences.CopyScreenshotsToClipboard = _clipboardCheck.IsChecked == true;
        _preferences.CopyVideosToClipboard = _videoClipboardCheck.IsChecked == true;
        _preferences.EnableQuickAnnotations = _quickAnnotationsCheck.IsChecked == true;
        _preferences.ShowRecordingFrame = _frameCheck.IsChecked == true;
        _preferences.ShowGifRecordingFrame = _gifFrameCheck.IsChecked == true;
        _preferences.SeparateCaptureTypes = _separateCaptureTypesCheck.IsChecked == true;
        _preferences.CreateDailyCaptureFolders = _dailyFoldersCheck.IsChecked == true;
        UpdateCaptureOrganizationPreview();
        UpdateModeControls();

        var startWithWindows = _startupCheck.IsChecked == true;
        if (startWithWindows != _preferences.StartWithWindows)
        {
            if (WindowsStartup.TrySetEnabled(startWithWindows, out var startupError))
            {
                _preferences.StartWithWindows = startWithWindows;
                _startupStatusText.Text = startWithWindows
                    ? "CyCapture démarrera directement dans la zone de notification pour l’utilisateur actuel."
                    : "Le lancement automatique de CyCapture est désactivé.";
                _startupStatusText.Foreground = new SolidColorBrush(Color.FromRgb(127, 141, 138));
            }
            else
            {
                _initializing = true;
                _startupCheck.IsChecked = _preferences.StartWithWindows;
                _initializing = false;
                _startupStatusText.Text = $"Impossible de modifier le démarrage Windows : {startupError}";
                _startupStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 101, 115));
            }
        }
        _preferences.ApplyEncodingProfiles();
        await _preferences.SaveAsync();
    }

    private void UpdateModeControls()
    {
        _imageEncodingQualityCombo.IsEnabled = _preferences.ImageFormat == "jpeg";
        _audioEncodingQualityCombo.IsEnabled = _systemAudioCheck.IsChecked == true || _microphoneCheck.IsChecked == true;
        var hasAudioSource = _audioOnlySystemCheck.IsChecked == true || _audioOnlyMicrophoneCheck.IsChecked == true;
        _audioOnlyEncodingQualityCombo.IsEnabled = hasAudioSource;
        _audioOnlySourceHint.Text = hasAudioSource
            ? "Enregistrement MP3 (repli WAV si nécessaire). Impr écran arrête l’enregistrement."
            : "Choisissez au moins une source pour pouvoir lancer une capture audio.";
        _audioOnlySourceHint.Foreground = new SolidColorBrush(hasAudioSource
            ? Color.FromRgb(127, 141, 138)
            : Color.FromRgb(255, 101, 115));
    }

    private void UpdateCaptureOrganizationPreview()
    {
        var example = CaptureStorage.GetDirectory(_preferences, CaptureMode.Image, DateTimeOffset.Now);
        _captureOrganizationPreview.Text = $"Exemple pour une image : {example}";
    }

    private void PluginsChanged(object? sender, EventArgs args)
    {
        if (!_changingPlugin) RenderPlugins();
    }

    private void RenderPlugins()
    {
        _pluginsPanel.Children.Clear();
        foreach (var plugin in _postProcessing.GetPlugins())
            _pluginsPanel.Children.Add(CreatePluginCard(plugin));
    }

    private Control CreatePluginCard(CapturePluginDescriptor plugin)
    {
        var enabled = new CheckBox
        {
            Content = plugin.Name,
            IsChecked = plugin.Enabled,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center
        };
        enabled.IsCheckedChanged += async (_, _) =>
            await ChangePluginAsync(() => _postProcessing.SetEnabledAsync(plugin.Id, enabled.IsChecked == true));

        var configuration = new StackPanel { Spacing = 8, IsVisible = false };
        foreach (var setting in plugin.Settings)
            configuration.Children.Add(CreatePluginSetting(plugin, setting));

        var configure = new Button
        {
            Content = plugin.Settings.Count == 0 ? "Aucun réglage" : "Configurer",
            IsEnabled = plugin.Settings.Count > 0,
            Padding = new Thickness(12, 6)
        };
        configure.Click += (_, _) =>
        {
            configuration.IsVisible = !configuration.IsVisible;
            configure.Content = configuration.IsVisible ? "Masquer" : "Configurer";
        };

        return new Border
        {
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromRgb(9, 15, 15)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(38, 51, 49)),
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                        Children =
                        {
                            enabled,
                            new Border { [Grid.ColumnProperty] = 1, Child = configure }
                        }
                    },
                    new TextBlock { Text = plugin.Description, Foreground = new SolidColorBrush(Color.FromRgb(166, 179, 175)), TextWrapping = TextWrapping.Wrap },
                    new TextBlock { Text = plugin.Id, Foreground = new SolidColorBrush(Color.FromRgb(105, 119, 116)), FontSize = 10 },
                    configuration
                }
            }
        };
    }

    private Control CreatePluginSetting(CapturePluginDescriptor plugin, CapturePluginSettingDefinition setting)
    {
        plugin.Values.TryGetValue(setting.Key, out var currentValue);
        currentValue ??= setting.DefaultValue;
        if (setting.Kind == CapturePluginSettingKind.Boolean)
        {
            var check = new CheckBox
            {
                Content = setting.Label,
                IsChecked = bool.TryParse(currentValue, out var value) && value
            };
            check.IsCheckedChanged += async (_, _) =>
                await ChangePluginAsync(() =>
                    _postProcessing.SetSettingAsync(plugin.Id, setting.Key, (check.IsChecked == true).ToString()));
            return check;
        }

        if (setting.Kind == CapturePluginSettingKind.Choice)
        {
            var combo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
            foreach (var choice in setting.Choices ?? [])
                combo.Items.Add(new ComboBoxItem { Content = choice, Tag = choice });
            SelectByTag(combo, currentValue);
            combo.SelectionChanged += async (_, _) =>
            {
                if (combo.SelectedItem is ComboBoxItem selected)
                    await ChangePluginAsync(() =>
                        _postProcessing.SetSettingAsync(plugin.Id, setting.Key, selected.Tag?.ToString() ?? string.Empty));
            };
            return new StackPanel
            {
                Spacing = 4,
                Children = { new TextBlock { Text = setting.Label, FontSize = 11 }, combo }
            };
        }

        var text = new TextBox { Text = currentValue, PlaceholderText = setting.Label };
        text.LostFocus += async (_, _) =>
            await ChangePluginAsync(() =>
                _postProcessing.SetSettingAsync(plugin.Id, setting.Key, text.Text ?? string.Empty));
        return new StackPanel
        {
            Spacing = 4,
            Children = { new TextBlock { Text = setting.Label, FontSize = 11 }, text }
        };
    }

    private void CloseClick(object? sender, RoutedEventArgs args) => Close();

    private static void SelectByTag(ComboBox combo, string tag)
    {
        combo.SelectedItem = combo.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase));
    }

    private async Task ChangePluginAsync(Func<Task> change)
    {
        _changingPlugin = true;
        try
        {
            await change();
        }
        finally
        {
            _changingPlugin = false;
        }
    }

    private T RequireControl<T>(string name) where T : Control =>
        this.FindControl<T>(name) ?? throw new InvalidOperationException($"Contrôle Avalonia introuvable : {name}");
}
