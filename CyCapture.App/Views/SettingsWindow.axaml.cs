using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CyCapture.Models;
using CyCapture.Services;

namespace CyCapture.Views;

public sealed partial class SettingsWindow : Window
{
    private readonly Preferences _preferences;
    private readonly PostProcessingService _postProcessing;
    private readonly TextBlock _outputPathText;
    private readonly ComboBox _videoQualityCombo;
    private readonly ComboBox _gifQualityCombo;
    private readonly ComboBox _selectionModeCombo;
    private readonly ComboBox _printScreenBehaviorCombo;
    private readonly CheckBox _systemAudioCheck;
    private readonly CheckBox _microphoneCheck;
    private readonly CheckBox _clipboardCheck;
    private readonly CheckBox _frameCheck;
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
        _outputPathText = RequireControl<TextBlock>("OutputPathText");
        _videoQualityCombo = RequireControl<ComboBox>("VideoQualityCombo");
        _gifQualityCombo = RequireControl<ComboBox>("GifQualityCombo");
        _selectionModeCombo = RequireControl<ComboBox>("SelectionModeCombo");
        _printScreenBehaviorCombo = RequireControl<ComboBox>("PrintScreenBehaviorCombo");
        _systemAudioCheck = RequireControl<CheckBox>("SystemAudioCheck");
        _microphoneCheck = RequireControl<CheckBox>("MicrophoneCheck");
        _clipboardCheck = RequireControl<CheckBox>("ClipboardCheck");
        _frameCheck = RequireControl<CheckBox>("FrameCheck");
        _pluginsPanel = RequireControl<StackPanel>("PluginsPanel");
        _outputPathText.Text = preferences.EffectiveOutputDirectory;
        SelectByTag(_videoQualityCombo, preferences.VideoQualityLevel.ToString());
        SelectByTag(_gifQualityCombo, preferences.GifQuality.ToString());
        SelectByTag(_selectionModeCombo, preferences.SelectionMode.ToString());
        SelectByTag(_printScreenBehaviorCombo, preferences.PrintScreenBehavior.ToString());
        _systemAudioCheck.IsChecked = preferences.IncludeSystemAudio;
        _microphoneCheck.IsChecked = preferences.IncludeMicrophone;
        _clipboardCheck.IsChecked = preferences.CopyScreenshotsToClipboard;
        _frameCheck.IsChecked = preferences.ShowRecordingFrame;
        _initializing = false;
        RenderPlugins();
        _postProcessing.PluginsChanged += PluginsChanged;
        Closed += (_, _) => _postProcessing.PluginsChanged -= PluginsChanged;
    }

    public event EventHandler<CaptureMode>? CaptureRequested;

    private void ImageClick(object? sender, RoutedEventArgs args) => Request(CaptureMode.Image);
    private void VideoClick(object? sender, RoutedEventArgs args) => Request(CaptureMode.Video);
    private void GifClick(object? sender, RoutedEventArgs args) => Request(CaptureMode.Gif);

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
        if (_gifQualityCombo.SelectedItem is ComboBoxItem quality
            && Enum.TryParse<GifQuality>(quality.Tag?.ToString(), out var qualityValue))
            _preferences.GifQuality = qualityValue;
        if (_selectionModeCombo.SelectedItem is ComboBoxItem selectionMode
            && Enum.TryParse<CaptureSelectionMode>(selectionMode.Tag?.ToString(), out var selectionModeValue))
            _preferences.SelectionMode = selectionModeValue;
        if (_printScreenBehaviorCombo.SelectedItem is ComboBoxItem printScreenBehavior
            && Enum.TryParse<PrintScreenBehavior>(printScreenBehavior.Tag?.ToString(), out var printScreenBehaviorValue))
            _preferences.PrintScreenBehavior = printScreenBehaviorValue;
        _preferences.IncludeSystemAudio = _systemAudioCheck.IsChecked == true;
        _preferences.IncludeMicrophone = _microphoneCheck.IsChecked == true;
        _preferences.CopyScreenshotsToClipboard = _clipboardCheck.IsChecked == true;
        _preferences.ShowRecordingFrame = _frameCheck.IsChecked == true;
        _preferences.ApplyVideoQuality();
        await _preferences.SaveAsync();
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
