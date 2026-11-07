using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CyCapture.Models;
using CyCapture.Platform.Windows;
using CyCapture.Services;

namespace CyCapture.Views;

public sealed partial class QuickCaptureWindow : Window
{
    private readonly Preferences _preferences;
    private readonly PostProcessingService _postProcessing;
    private readonly ComboBox _videoQualityCombo;
    private readonly ComboBox _videoEncodingQualityCombo;
    private readonly ComboBox _audioEncodingQualityCombo;
    private readonly ComboBox _audioOnlyEncodingQualityCombo;
    private readonly CheckBox _videoSystemAudioCheck;
    private readonly CheckBox _videoMicrophoneCheck;
    private readonly CheckBox _audioOnlySystemCheck;
    private readonly CheckBox _audioOnlyMicrophoneCheck;
    private readonly CheckBox _audioOnlyIgnoreSystemVolumeCheck;
    private readonly ComboBox _imageFormatCombo;
    private readonly ComboBox _imageEncodingQualityCombo;
    private readonly ComboBox _gifQualityCombo;
    private readonly ComboBox _selectionModeCombo;
    private readonly ComboBox _translationSourceLanguageCombo;
    private readonly ComboBox _translationTargetLanguageCombo;
    private readonly Border _pluginOptionsBorder;
    private readonly WrapPanel _pluginOptionsPanel;
    private bool _modeChosen;

    public QuickCaptureWindow() : this(new Preferences(), new PostProcessingService())
    {
    }

    internal QuickCaptureWindow(Preferences preferences, PostProcessingService postProcessing)
    {
        _preferences = preferences;
        _postProcessing = postProcessing;
        UiLocalization.Use(preferences.ApplicationLanguage);
        AvaloniaXamlLoader.Load(this);
        _videoQualityCombo = RequireControl<ComboBox>("VideoQualityCombo");
        _videoEncodingQualityCombo = RequireControl<ComboBox>("VideoEncodingQualityCombo");
        _audioEncodingQualityCombo = RequireControl<ComboBox>("AudioEncodingQualityCombo");
        _audioOnlyEncodingQualityCombo = RequireControl<ComboBox>("AudioOnlyEncodingQualityCombo");
        _videoSystemAudioCheck = RequireControl<CheckBox>("VideoSystemAudioCheck");
        _videoMicrophoneCheck = RequireControl<CheckBox>("VideoMicrophoneCheck");
        _audioOnlySystemCheck = RequireControl<CheckBox>("AudioOnlySystemCheck");
        _audioOnlyMicrophoneCheck = RequireControl<CheckBox>("AudioOnlyMicrophoneCheck");
        _audioOnlyIgnoreSystemVolumeCheck = RequireControl<CheckBox>("AudioOnlyIgnoreSystemVolumeCheck");
        _imageFormatCombo = RequireControl<ComboBox>("ImageFormatCombo");
        _imageEncodingQualityCombo = RequireControl<ComboBox>("ImageEncodingQualityCombo");
        _gifQualityCombo = RequireControl<ComboBox>("GifQualityCombo");
        _selectionModeCombo = RequireControl<ComboBox>("SelectionModeCombo");
        _translationSourceLanguageCombo = RequireControl<ComboBox>("TranslationSourceLanguageCombo");
        _translationTargetLanguageCombo = RequireControl<ComboBox>("TranslationTargetLanguageCombo");
        _pluginOptionsBorder = RequireControl<Border>("PluginOptionsBorder");
        _pluginOptionsPanel = RequireControl<WrapPanel>("PluginOptionsPanel");
        SelectByTag(_videoQualityCombo, preferences.VideoQualityLevel.ToString());
        SelectByTag(_videoEncodingQualityCombo, preferences.VideoEncodingQuality.ToString());
        SelectByTag(_audioEncodingQualityCombo, preferences.AudioEncodingQuality.ToString());
        var audioOnly = preferences.GetAudioSettings(CaptureMode.Audio);
        SelectByTag(_audioOnlyEncodingQualityCombo, audioOnly.Quality.ToString());
        _videoSystemAudioCheck.IsChecked = preferences.IncludeSystemAudio;
        _videoMicrophoneCheck.IsChecked = preferences.IncludeMicrophone;
        _audioOnlySystemCheck.IsChecked = audioOnly.SystemAudio;
        _audioOnlyMicrophoneCheck.IsChecked = audioOnly.Microphone;
        _audioOnlyIgnoreSystemVolumeCheck.IsChecked = preferences.AudioOnlyIgnoreSystemVolume;
        SelectByTag(_imageFormatCombo, preferences.ImageFormat);
        SelectByTag(_imageEncodingQualityCombo, preferences.ImageEncodingQuality.ToString());
        SelectByTag(_gifQualityCombo, preferences.GifQuality.ToString());
        SelectByTag(_selectionModeCombo, preferences.SelectionMode.ToString());
        PopulateTranslationLanguages();
        PopulatePluginOptions();
        UiLocalization.ApplyTo(this);
        Opened += OnOpened;
        KeyDown += (_, args) =>
        {
            if (args.Key == Avalonia.Input.Key.Escape) Close();
            else if (args.Key == Avalonia.Input.Key.F1)
            {
                HelpRequested?.Invoke(this, EventArgs.Empty);
                args.Handled = true;
            }
        };
    }

    public event EventHandler<CaptureRequest>? CaptureRequested;
    public event EventHandler? HelpRequested;

    private void OnOpened(object? sender, EventArgs args)
    {
        if (!NativeMethods.GetCursorPos(out var cursor)) return;
        var monitor = NativeMethods.MonitorAt(cursor.X, cursor.Y);
        var pixelWidth = (int)Math.Ceiling(Width * monitor.Scale);
        var pixelHeight = (int)Math.Ceiling(Height * monitor.Scale);
        var x = Math.Clamp(cursor.X - pixelWidth / 2, monitor.WorkArea.X + 8, monitor.WorkArea.Right - pixelWidth - 8);
        var y = Math.Clamp(cursor.Y - pixelHeight / 2, monitor.WorkArea.Y + 8, monitor.WorkArea.Bottom - pixelHeight - 8);
        Position = new PixelPoint(x, y);
        NativeMethods.SetWindowDisplayAffinity(TryGetPlatformHandle()?.Handle ?? 0, NativeMethods.WdaExcludeFromCapture);
        Activate();
    }

    private async Task SelectAsync(CaptureMode mode, bool translateSelection = false)
    {
        if (_modeChosen) return;
        _modeChosen = true;
        if (_videoQualityCombo.SelectedItem is ComboBoxItem videoQuality
            && Enum.TryParse<VideoQuality>(videoQuality.Tag?.ToString(), out var selectedVideoQuality))
            _preferences.VideoQualityLevel = selectedVideoQuality;
        if (_videoEncodingQualityCombo.SelectedItem is ComboBoxItem videoEncoding
            && Enum.TryParse<VideoEncodingQuality>(videoEncoding.Tag?.ToString(), out var selectedVideoEncoding))
            _preferences.VideoEncodingQuality = selectedVideoEncoding;
        if (_audioEncodingQualityCombo.SelectedItem is ComboBoxItem audioEncoding
            && Enum.TryParse<AudioEncodingQuality>(audioEncoding.Tag?.ToString(), out var selectedAudioEncoding))
            _preferences.AudioEncodingQuality = selectedAudioEncoding;
        if (_audioOnlyEncodingQualityCombo.SelectedItem is ComboBoxItem audioOnlyEncoding
            && Enum.TryParse<AudioEncodingQuality>(audioOnlyEncoding.Tag?.ToString(), out var selectedAudioOnlyEncoding))
            _preferences.AudioOnlyEncodingQuality = selectedAudioOnlyEncoding;
        _preferences.IncludeSystemAudio = _videoSystemAudioCheck.IsChecked == true;
        _preferences.IncludeMicrophone = _videoMicrophoneCheck.IsChecked == true;
        _preferences.AudioOnlyIncludeSystemAudio = _audioOnlySystemCheck.IsChecked == true;
        _preferences.AudioOnlyIncludeMicrophone = _audioOnlyMicrophoneCheck.IsChecked == true;
        _preferences.AudioOnlyIgnoreSystemVolume = _audioOnlyIgnoreSystemVolumeCheck.IsChecked == true;
        if (_imageFormatCombo.SelectedItem is ComboBoxItem imageFormat)
            _preferences.ImageFormat = imageFormat.Tag?.ToString() ?? "png";
        if (_imageEncodingQualityCombo.SelectedItem is ComboBoxItem imageEncoding
            && Enum.TryParse<ImageEncodingQuality>(imageEncoding.Tag?.ToString(), out var selectedImageEncoding))
            _preferences.ImageEncodingQuality = selectedImageEncoding;
        if (_gifQualityCombo.SelectedItem is ComboBoxItem gifQuality
            && Enum.TryParse<GifQuality>(gifQuality.Tag?.ToString(), out var selectedGifQuality))
            _preferences.GifQuality = selectedGifQuality;
        if (_selectionModeCombo.SelectedItem is ComboBoxItem selectionMode
            && Enum.TryParse<CaptureSelectionMode>(selectionMode.Tag?.ToString(), out var selectedMode))
            _preferences.SelectionMode = selectedMode;
        SaveTranslationLanguages();
        _preferences.ApplyEncodingProfiles();
        try
        {
            await _preferences.SaveAsync();
        }
        catch
        {
            // A capture must still start when preferences cannot be persisted.
        }
        Hide();
        CaptureRequested?.Invoke(this, new CaptureRequest(mode, _preferences.SelectionMode, translateSelection));
        Close();
    }

    private async void ImageClick(object? sender, RoutedEventArgs args) => await SelectAsync(CaptureMode.Image);
    private async void VideoClick(object? sender, RoutedEventArgs args) => await SelectAsync(CaptureMode.Video);
    private async void GifClick(object? sender, RoutedEventArgs args) => await SelectAsync(CaptureMode.Gif);
    private async void AudioClick(object? sender, RoutedEventArgs args) => await SelectAsync(CaptureMode.Audio);
    private async void TranslateClick(object? sender, RoutedEventArgs args)
    {
        if (_modeChosen) return;
        SaveTranslationLanguages();
        await SelectAsync(CaptureMode.Image, translateSelection: true);
    }
    private void CloseClick(object? sender, RoutedEventArgs args) => Close();

    private async void ManageTranslationModelsClick(object? sender, RoutedEventArgs args)
    {
        SaveTranslationLanguages();
        try { await _preferences.SaveAsync(); } catch { }
        var manager = new TranslationModelManagerWindow(_preferences, new TranslationModelService());
        await manager.ShowDialog(this);
        PopulateTranslationLanguages();
    }

    private void PopulateTranslationLanguages()
    {
        _translationSourceLanguageCombo.Items.Clear();
        _translationSourceLanguageCombo.Items.Add(new ComboBoxItem
        {
            Content = TranslationLanguageCatalog.DisplayName(TranslationLanguageCatalog.AutomaticCode),
            Tag = TranslationLanguageCatalog.AutomaticCode
        });
        foreach (var language in TranslationLanguageCatalog.Languages)
        {
            _translationSourceLanguageCombo.Items.Add(new ComboBoxItem { Content = language.DisplayName, Tag = language.Code });
        }
        _translationTargetLanguageCombo.Items.Clear();
        foreach (var language in TranslationLanguageCatalog.Languages)
        {
            _translationTargetLanguageCombo.Items.Add(new ComboBoxItem { Content = language.DisplayName, Tag = language.Code });
        }
        SelectByTag(_translationSourceLanguageCombo, _preferences.TranslationSourceLanguage);
        SelectByTag(_translationTargetLanguageCombo, _preferences.TranslationTargetLanguage);
    }

    private void SaveTranslationLanguages()
    {
        if (_translationSourceLanguageCombo.SelectedItem is ComboBoxItem source)
            _preferences.TranslationSourceLanguage = TranslationLanguageCatalog.NormalizeSource(source.Tag?.ToString());
        if (_translationTargetLanguageCombo.SelectedItem is ComboBoxItem target)
            _preferences.TranslationTargetLanguage = TranslationLanguageCatalog.NormalizeTarget(target.Tag?.ToString());
    }

    private void PopulatePluginOptions()
    {
        var plugins = _postProcessing.GetPlugins()
            .Where(plugin => !string.IsNullOrWhiteSpace(plugin.QuickAccessLabel))
            .ToList();
        foreach (var plugin in plugins)
        {
            var check = new CheckBox
            {
                Content = plugin.QuickAccessLabel,
                IsChecked = plugin.Enabled,
                Margin = new Thickness(0, 0, 18, 0)
            };
            check.IsCheckedChanged += async (_, _) =>
                await _postProcessing.SetEnabledAsync(plugin.Id, check.IsChecked == true);
            _pluginOptionsPanel.Children.Add(check);
        }
        _pluginOptionsBorder.IsVisible = plugins.Count > 0;
    }

    private static void SelectByTag(ComboBox combo, string tag)
    {
        combo.SelectedItem = combo.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase));
    }

    private T RequireControl<T>(string name) where T : Control =>
        this.FindControl<T>(name) ?? throw new InvalidOperationException($"Contrôle Avalonia introuvable : {name}");
}
