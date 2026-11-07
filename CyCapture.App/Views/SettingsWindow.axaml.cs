using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
    private readonly UpdateService _updateService = new();
    private readonly TranslationModelService _translationModels = new();
    private readonly CancellationTokenSource _updateCancellation = new();
    private readonly TextBlock _appVersionText;
    private readonly TextBlock _updateStatusText;
    private readonly Button _updateButton;
    private readonly ComboBox _applicationLanguageCombo;
    private readonly ComboBox _documentationLanguageCombo;
    private readonly ComboBox _translationSourceLanguageCombo;
    private readonly ComboBox _translationTargetLanguageCombo;
    private readonly TextBlock _translationModelsStatusText;
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
    private readonly CheckBox _audioOnlyIgnoreSystemVolumeCheck;
    private readonly CheckBox _clipboardCheck;
    private readonly CheckBox _videoClipboardCheck;
    private readonly CheckBox _gifClipboardCheck;
    private readonly CheckBox _audioClipboardCheck;
    private readonly CheckBox _translationClipboardCheck;
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
    private bool _checkingUpdate;
    private UpdateCheckResult? _availableUpdate;
    private string? _downloadedUpdatePath;

    public SettingsWindow() : this(new Preferences(), new PostProcessingService())
    {
    }

    internal SettingsWindow(Preferences preferences, PostProcessingService postProcessing)
    {
        _preferences = preferences;
        _postProcessing = postProcessing;
        UiLocalization.Use(preferences.ApplicationLanguage);
        AvaloniaXamlLoader.Load(this);
        WindowsWindowAppearance.Attach(this);
        _appVersionText = RequireControl<TextBlock>("AppVersionText");
        _updateStatusText = RequireControl<TextBlock>("UpdateStatusText");
        _updateButton = RequireControl<Button>("UpdateButton");
        _applicationLanguageCombo = RequireControl<ComboBox>("ApplicationLanguageCombo");
        _documentationLanguageCombo = RequireControl<ComboBox>("DocumentationLanguageCombo");
        _translationSourceLanguageCombo = RequireControl<ComboBox>("TranslationSourceLanguageCombo");
        _translationTargetLanguageCombo = RequireControl<ComboBox>("TranslationTargetLanguageCombo");
        _translationModelsStatusText = RequireControl<TextBlock>("TranslationModelsStatusText");
        RefreshVersionText();
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
        _audioOnlyIgnoreSystemVolumeCheck = RequireControl<CheckBox>("AudioOnlyIgnoreSystemVolumeCheck");
        _clipboardCheck = RequireControl<CheckBox>("ClipboardCheck");
        _videoClipboardCheck = RequireControl<CheckBox>("VideoClipboardCheck");
        _gifClipboardCheck = RequireControl<CheckBox>("GifClipboardCheck");
        _audioClipboardCheck = RequireControl<CheckBox>("AudioClipboardCheck");
        _translationClipboardCheck = RequireControl<CheckBox>("TranslationClipboardCheck");
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
        SelectByTag(_applicationLanguageCombo, preferences.ApplicationLanguage);
        SelectByTag(_documentationLanguageCombo, preferences.DocumentationLanguage);
        PopulateTranslationLanguages();
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
        _audioOnlyIgnoreSystemVolumeCheck.IsChecked = preferences.AudioOnlyIgnoreSystemVolume;
        _clipboardCheck.IsChecked = preferences.CopyScreenshotsToClipboard;
        _videoClipboardCheck.IsChecked = preferences.CopyVideosToClipboard;
        _gifClipboardCheck.IsChecked = preferences.CopyGifsToClipboard;
        _audioClipboardCheck.IsChecked = preferences.CopyAudioToClipboard;
        _translationClipboardCheck.IsChecked = preferences.CopyTranslationsToClipboard;
        _quickAnnotationsCheck.IsChecked = preferences.EnableQuickAnnotations;
        _frameCheck.IsChecked = preferences.ShowRecordingFrame;
        _gifFrameCheck.IsChecked = preferences.ShouldShowRecordingFrame(CaptureMode.Gif);
        _separateCaptureTypesCheck.IsChecked = preferences.SeparateCaptureTypes;
        _dailyFoldersCheck.IsChecked = preferences.CreateDailyCaptureFolders;
        preferences.StartWithWindows = WindowsStartup.IsEnabled();
        _startupCheck.IsChecked = preferences.StartWithWindows;
        UpdateCaptureOrganizationPreview();
        UpdateModeControls();
        UpdateTranslationModelStatus();
        _initializing = false;
        RenderPlugins();
        UiLocalization.ApplyTo(this);
        _postProcessing.PluginsChanged += PluginsChanged;
        Closed += (_, _) =>
        {
            _postProcessing.PluginsChanged -= PluginsChanged;
            _updateCancellation.Cancel();
            _updateCancellation.Dispose();
        };
        KeyDown += (_, args) =>
        {
            if (args.Key != Key.F1) return;
            DocumentationRequested?.Invoke(this, EventArgs.Empty);
            args.Handled = true;
        };
    }

    public event EventHandler? DocumentationRequested;
    public event EventHandler? InterfaceLanguageChanged;

    private void DocumentationClick(object? sender, RoutedEventArgs args) => DocumentationRequested?.Invoke(this, EventArgs.Empty);

    private async void ApplicationLanguageChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (_initializing || _applicationLanguageCombo.SelectedItem is not ComboBoxItem item) return;
        _preferences.ApplicationLanguage = Preferences.NormalizeLanguage(item.Tag?.ToString(), "fr");
        UiLocalization.Use(_preferences.ApplicationLanguage);
        UiLocalization.ApplyTo(this);
        RefreshVersionText();
        UpdateCaptureOrganizationPreview();
        UpdateModeControls();
        await _preferences.SaveAsync();
        InterfaceLanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    private async void DocumentationLanguageChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (_initializing || _documentationLanguageCombo.SelectedItem is not ComboBoxItem item) return;
        _preferences.DocumentationLanguage = CyDocHelpService.NormalizeLocale(item.Tag?.ToString());
        await _preferences.SaveAsync();
    }

    private void TranslationLanguageChanged(object? sender, SelectionChangedEventArgs args)
    {
        if (_initializing) return;
        SaveTranslationLanguagePreferences();
        SavePreferences();
    }

    private async void ManageTranslationModelsClick(object? sender, RoutedEventArgs args)
    {
        SaveTranslationLanguagePreferences();
        try { await _preferences.SaveAsync(); } catch { }
        var manager = new TranslationModelManagerWindow(_preferences, _translationModels);
        await manager.ShowDialog(this);
        _initializing = true;
        PopulateTranslationLanguages();
        _initializing = false;
        UpdateTranslationModelStatus();
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

    private async void UpdateClick(object? sender, RoutedEventArgs args)
    {
        if (_checkingUpdate) return;
        if (_downloadedUpdatePath is not null)
        {
            try
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{_downloadedUpdatePath}\"") { UseShellExecute = true });
            }
            catch (Exception error)
            {
                SetUpdateStatus($"Impossible d’ouvrir le dossier : {error.Message}", true);
            }
            return;
        }

        if (_availableUpdate is { } available)
        {
            if (available.DownloadUrl is null || available.AssetName is null)
            {
                OpenUpdatePage(available.ReleasePageUrl);
                return;
            }
            var destination = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = $"Télécharger CyCapture {available.LatestVersion:3}",
                SuggestedFileName = available.AssetName,
                DefaultExtension = "exe",
                FileTypeChoices =
                [
                    new FilePickerFileType("Application Windows") { Patterns = ["*.exe"] }
                ]
            });
            if (destination is null) return;

            await RunUpdateActionAsync(async () =>
            {
                SetUpdateStatus($"Téléchargement et vérification de {available.AssetName}…");
                _updateButton.Content = "Téléchargement…";
                await _updateService.DownloadAsync(available, destination.Path.LocalPath, _updateCancellation.Token);
                _downloadedUpdatePath = destination.Path.LocalPath;
                SetUpdateStatus("Mise à jour téléchargée et SHA-256 vérifié. Quittez CyCapture, puis lancez ce fichier.");
                _updateButton.Content = "Afficher le fichier";
            });
            return;
        }

        await RunUpdateActionAsync(async () =>
        {
            SetUpdateStatus("Vérification de la dernière release GitHub…");
            _updateButton.Content = "Vérification…";
            var result = await _updateService.CheckAsync(_updateCancellation.Token);
            if (!result.IsUpdateAvailable)
            {
                SetUpdateStatus($"CyCapture {AppBuildInfo.DisplayVersion} est à jour.");
                _updateButton.Content = "Revérifier";
                return;
            }

            _availableUpdate = result;
            if (result.DownloadUrl is not null)
            {
                SetUpdateStatus($"CyCapture {result.LatestVersion:3} est disponible pour l’édition {AppBuildInfo.EditionLabel.ToLowerInvariant()}.");
                _updateButton.Content = $"Télécharger {result.LatestVersion:3}";
            }
            else
            {
                SetUpdateStatus($"CyCapture {result.LatestVersion:3} est disponible, mais le binaire de cette édition est absent.", true);
                _updateButton.Content = "Voir la release";
            }
        });
    }

    internal void OfferUpdate(UpdateCheckResult update)
    {
        if (!update.IsUpdateAvailable) return;
        _availableUpdate = update;
        if (update.DownloadUrl is not null)
        {
            SetUpdateStatus(UiLocalization.Text(
                $"CyCapture {update.LatestVersion:3} est disponible pour l’édition {AppBuildInfo.EditionLabel.ToLowerInvariant()}.",
                $"CyCapture {update.LatestVersion:3} is available for the {UiLocalization.Translate(AppBuildInfo.EditionLabel).ToLowerInvariant()} edition."));
            _updateButton.Content = UiLocalization.Text(
                $"Télécharger {update.LatestVersion:3}",
                $"Download {update.LatestVersion:3}");
        }
        else
        {
            SetUpdateStatus(UiLocalization.Text(
                $"CyCapture {update.LatestVersion:3} est disponible, mais le binaire de cette édition est absent.",
                $"CyCapture {update.LatestVersion:3} is available, but this edition has no matching binary."), true);
            _updateButton.Content = UiLocalization.Text("Voir la release", "View release");
        }
    }

    private async Task RunUpdateActionAsync(Func<Task> action)
    {
        _checkingUpdate = true;
        _updateButton.IsEnabled = false;
        try
        {
            await action();
        }
        catch (OperationCanceledException) when (_updateCancellation.IsCancellationRequested)
        {
        }
        catch (Exception error)
        {
            SetUpdateStatus($"Mise à jour impossible : {error.Message}", true);
            _updateButton.Content = _availableUpdate is null ? "Réessayer" : "Télécharger";
        }
        finally
        {
            _checkingUpdate = false;
            if (!_updateCancellation.IsCancellationRequested) _updateButton.IsEnabled = true;
        }
    }

    private void OpenUpdatePage(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception error)
        {
            SetUpdateStatus($"Impossible d’ouvrir la release : {error.Message}", true);
        }
    }

    private void SetUpdateStatus(string text, bool error = false)
    {
        _updateStatusText.Text = text;
        _updateStatusText.Foreground = new SolidColorBrush(error
            ? Color.FromRgb(255, 101, 115)
            : Color.FromRgb(157, 174, 170));
    }

    private void RefreshVersionText()
    {
        var beta = UiLocalization.Text("BÊTA", "BETA");
        _appVersionText.Text = $"VERSION {AppBuildInfo.DisplayVersion} · {beta} · {UiLocalization.Translate(AppBuildInfo.EditionLabel)}";
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
        SaveTranslationLanguagePreferences();
        _preferences.IncludeSystemAudio = _systemAudioCheck.IsChecked == true;
        _preferences.IncludeMicrophone = _microphoneCheck.IsChecked == true;
        _preferences.AudioOnlyIncludeSystemAudio = _audioOnlySystemCheck.IsChecked == true;
        _preferences.AudioOnlyIncludeMicrophone = _audioOnlyMicrophoneCheck.IsChecked == true;
        _preferences.AudioOnlyIgnoreSystemVolume = _audioOnlyIgnoreSystemVolumeCheck.IsChecked == true;
        _preferences.CopyScreenshotsToClipboard = _clipboardCheck.IsChecked == true;
        _preferences.CopyVideosToClipboard = _videoClipboardCheck.IsChecked == true;
        _preferences.CopyGifsToClipboard = _gifClipboardCheck.IsChecked == true;
        _preferences.CopyAudioToClipboard = _audioClipboardCheck.IsChecked == true;
        _preferences.CopyTranslationsToClipboard = _translationClipboardCheck.IsChecked == true;
        _preferences.ClipboardOptionsInitialized = true;
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
                    ? UiLocalization.Text("CyCapture démarrera directement dans la zone de notification pour l’utilisateur actuel.", "CyCapture will start directly in the notification area for the current user.")
                    : UiLocalization.Text("Le lancement automatique de CyCapture est désactivé.", "Starting CyCapture with Windows is disabled.");
                _startupStatusText.Foreground = new SolidColorBrush(Color.FromRgb(127, 141, 138));
            }
            else
            {
                _initializing = true;
                _startupCheck.IsChecked = _preferences.StartWithWindows;
                _initializing = false;
                _startupStatusText.Text = UiLocalization.Text(
                    $"Impossible de modifier le démarrage Windows : {startupError}",
                    $"Unable to change Windows startup: {startupError}");
                _startupStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 101, 115));
            }
        }
        _preferences.ApplyEncodingProfiles();
        await _preferences.SaveAsync();
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
            _translationSourceLanguageCombo.Items.Add(new ComboBoxItem { Content = language.DisplayName, Tag = language.Code });
        _translationTargetLanguageCombo.Items.Clear();
        foreach (var language in TranslationLanguageCatalog.Languages)
            _translationTargetLanguageCombo.Items.Add(new ComboBoxItem { Content = language.DisplayName, Tag = language.Code });
        SelectByTag(_translationSourceLanguageCombo, _preferences.TranslationSourceLanguage);
        SelectByTag(_translationTargetLanguageCombo, _preferences.TranslationTargetLanguage);
    }

    private void SaveTranslationLanguagePreferences()
    {
        if (_translationSourceLanguageCombo.SelectedItem is ComboBoxItem source)
            _preferences.TranslationSourceLanguage = TranslationLanguageCatalog.NormalizeSource(source.Tag?.ToString());
        if (_translationTargetLanguageCombo.SelectedItem is ComboBoxItem target)
            _preferences.TranslationTargetLanguage = TranslationLanguageCatalog.NormalizeTarget(target.Tag?.ToString());
        UpdateTranslationModelStatus();
    }

    private void UpdateTranslationModelStatus()
    {
        if (_translationModelsStatusText is null) return;
        var ocrCount = _translationModels.InstalledOcrLanguages().Count;
        var directionCount = _translationModels.InstalledDirections().Count;
        _translationModelsStatusText.Text = UiLocalization.Text(
            $"Modèles installés : {ocrCount} langue(s) OCR · {directionCount} direction(s) de traduction.",
            $"Installed models: {ocrCount} OCR language(s) · {directionCount} translation direction(s).");
    }

    private void UpdateModeControls()
    {
        _imageEncodingQualityCombo.IsEnabled = _preferences.ImageFormat == "jpeg";
        _audioEncodingQualityCombo.IsEnabled = _systemAudioCheck.IsChecked == true || _microphoneCheck.IsChecked == true;
        var hasAudioSource = _audioOnlySystemCheck.IsChecked == true || _audioOnlyMicrophoneCheck.IsChecked == true;
        _audioOnlyEncodingQualityCombo.IsEnabled = hasAudioSource;
        _audioOnlySourceHint.Text = hasAudioSource
            ? UiLocalization.Text("Enregistrement MP3 (repli WAV si nécessaire). Impr écran arrête l’enregistrement.", "MP3 recording (WAV fallback when needed). Print Screen stops recording.")
            : UiLocalization.Text("Choisissez au moins une source pour pouvoir lancer une capture audio.", "Choose at least one source before starting an audio capture.");
        _audioOnlySourceHint.Foreground = new SolidColorBrush(hasAudioSource
            ? Color.FromRgb(127, 141, 138)
            : Color.FromRgb(255, 101, 115));
    }

    private void UpdateCaptureOrganizationPreview()
    {
        var example = CaptureStorage.GetDirectory(_preferences, CaptureMode.Image, DateTimeOffset.Now);
        _captureOrganizationPreview.Text = UiLocalization.Text($"Exemple pour une image : {example}", $"Image example: {example}");
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
            Content = plugin.Settings.Count == 0
                ? UiLocalization.Text("Aucun réglage", "No settings")
                : UiLocalization.Text("Configurer", "Configure"),
            IsEnabled = plugin.Settings.Count > 0,
            Padding = new Thickness(12, 6)
        };
        configure.Click += (_, _) =>
        {
            configuration.IsVisible = !configuration.IsVisible;
            configure.Content = configuration.IsVisible
                ? UiLocalization.Text("Masquer", "Hide")
                : UiLocalization.Text("Configurer", "Configure");
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
