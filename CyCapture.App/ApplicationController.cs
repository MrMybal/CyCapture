using System.Diagnostics;
using System.Drawing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CyCapture.Infrastructure;
using CyCapture.Models;
using CyCapture.Platform.Windows;
using CyCapture.Services;
using CyCapture.Views;

namespace CyCapture;

internal sealed class ApplicationController
{
    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly SelectionService _selection = new();
    private readonly ImageCaptureService _images = new();
    private readonly RecordingService _recording = new();
    private readonly RecordingIndicatorService _indicator = new();
    private readonly PostProcessingService _postProcessing = new();
    private readonly HistoryService _history = new();
    private readonly DispatcherTimer _recordingTimer;
    private readonly DispatcherTimer _printScreenHoldTimer;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly EventWaitHandle _existingInstanceSignal;
    private readonly Dictionary<string, NativeMenuItem> _pluginTrayItems = new(StringComparer.OrdinalIgnoreCase);
    private Preferences _preferences = new();
    private PrintScreenHook? _printScreenHook;
    private TrayIcon? _tray;
    private WindowIcon? _idleIcon;
    private WindowIcon? _recordingIcon;
    private NativeMenuItem? _recordingMenuItem;
    private QuickCaptureWindow? _quickWindow;
    private GalleryWindow? _galleryWindow;
    private SettingsWindow? _settingsWindow;
    private ToastWindow? _toast;
    private bool _captureFlowActive;
    private bool _printScreenHybridPending;
    private bool _testMode;
    private bool _quitting;

    internal ApplicationController(IClassicDesktopStyleApplicationLifetime desktop)
    {
        _desktop = desktop;
        _recording.StateChanged += (_, _) => Dispatcher.UIThread.Post(UpdateTrayState);
        _postProcessing.PluginsChanged += (_, _) => Dispatcher.UIThread.Post(UpdatePluginTrayItems);
        _recordingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _recordingTimer.Tick += (_, _) => UpdateRecordingTimer();
        _printScreenHoldTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _printScreenHoldTimer.Tick += PrintScreenHoldElapsed;
        _existingInstanceSignal = new EventWaitHandle(false, EventResetMode.AutoReset, NativeMethods.ExistingInstanceEventName);
    }

    internal void Start()
    {
        _ = StartAsync();
    }

    private async Task StartAsync()
    {
        var selfTestIndex = Array.FindIndex(_desktop.Args ?? [], argument =>
            argument.Equals("--self-test", StringComparison.OrdinalIgnoreCase));
        if (selfTestIndex >= 0)
        {
            var reportPath = selfTestIndex + 1 < (_desktop.Args?.Length ?? 0)
                ? _desktop.Args![selfTestIndex + 1]
                : Path.Combine(Path.GetTempPath(), "CyCapture", "self-test.json");
            await new SelfTestService().RunAsync(reportPath);
            _existingInstanceSignal.Dispose();
            _desktop.Shutdown();
            return;
        }

        _preferences = await Preferences.LoadAsync();
        if (_preferences.StartWithWindows)
            WindowsStartup.TrySetEnabled(true, out _);
        var testOutputIndex = Array.FindIndex(_desktop.Args ?? [], argument =>
            argument.Equals("--test-output", StringComparison.OrdinalIgnoreCase));
        if (testOutputIndex >= 0 && testOutputIndex + 1 < (_desktop.Args?.Length ?? 0))
        {
            _testMode = true;
            _preferences.OutputDirectory = Path.GetFullPath(_desktop.Args![testOutputIndex + 1]);
            _preferences.CopyScreenshotsToClipboard = false;
            _preferences.CopyVideosToClipboard = false;
            _preferences.IncludeSystemAudio = false;
            _preferences.IncludeMicrophone = false;
        }
        Directory.CreateDirectory(_preferences.EffectiveOutputDirectory);
        CreateTray();
        _ = _postProcessing.PrepareAsync(_shutdown.Token);
        _printScreenHook = new PrintScreenHook();
        _printScreenHook.Pressed += PrintScreenPressed;
        _printScreenHook.Released += PrintScreenReleased;
        try
        {
            _printScreenHook.Start();
        }
        catch (Exception error)
        {
            ShowToast("Impr écran indisponible", error.Message, true);
        }
        _ = WatchExistingInstanceAsync(_shutdown.Token);

        if (_desktop.Args?.Contains("--settings", StringComparer.OrdinalIgnoreCase) == true)
            ShowSettings();
        if (_desktop.Args?.Contains("--gallery", StringComparer.OrdinalIgnoreCase) == true)
            ShowGallery();
        if (_desktop.Args?.Contains("--quick", StringComparer.OrdinalIgnoreCase) == true)
            ShowQuickMenu();
    }

    private void CreateTray()
    {
        _idleIcon = CreateWindowIcon(false);
        _recordingIcon = CreateWindowIcon(true);
        _recordingMenuItem = new NativeMenuItem
        {
            Header = "Aucun enregistrement en cours",
            IsEnabled = false,
            Command = new ActionCommand(() => _ = StopRecordingAsync())
        };
        var menu = new NativeMenu
        {
            Items =
            {
                new NativeMenuItem { Header = "Capture rapide · Impr écran", Command = new ActionCommand(ShowQuickMenu) },
                new NativeMenuItemSeparator(),
                new NativeMenuItem { Header = "Capturer une image", Command = new ActionCommand(() => StartDirectCapture(CaptureMode.Image)) },
                new NativeMenuItem { Header = "Démarrer une capture vidéo", Command = new ActionCommand(() => StartDirectCapture(CaptureMode.Video)) },
                new NativeMenuItem { Header = "Créer un GIF", Command = new ActionCommand(() => StartDirectCapture(CaptureMode.Gif)) },
                new NativeMenuItem { Header = "Démarrer une capture audio", Command = new ActionCommand(() => StartDirectCapture(CaptureMode.Audio)) },
                new NativeMenuItemSeparator(),
                _recordingMenuItem,
                new NativeMenuItemSeparator(),
                new NativeMenuItem { Header = "Ouvrir CyCapture · Galerie", Command = new ActionCommand(ShowGallery) },
                new NativeMenuItem { Header = "Réglages…", Command = new ActionCommand(ShowSettings) },
                new NativeMenuItem { Header = "Dossier des captures", Command = new ActionCommand(OpenOutputDirectory) },
                new NativeMenuItem { Header = "Dossier des plugins", Command = new ActionCommand(OpenPluginsDirectory) },
                new NativeMenuItemSeparator(),
                new NativeMenuItem { Header = "Quitter CyCapture", Command = new ActionCommand(Quit) }
            }
        };
        var pluginInsertIndex = 6;
        var quickAccessPlugins = _postProcessing.GetPlugins()
            .Where(plugin => !string.IsNullOrWhiteSpace(plugin.QuickAccessLabel))
            .ToList();
        if (quickAccessPlugins.Count > 0)
        {
            menu.Items.Insert(pluginInsertIndex++, new NativeMenuItemSeparator());
            foreach (var plugin in quickAccessPlugins)
            {
                var item = new NativeMenuItem
                {
                    Header = plugin.QuickAccessLabel,
                    ToggleType = MenuItemToggleType.CheckBox,
                    IsChecked = plugin.Enabled,
                    Command = new ActionCommand(() => _ = TogglePluginAsync(plugin.Id))
                };
                _pluginTrayItems[plugin.Id] = item;
                menu.Items.Insert(pluginInsertIndex++, item);
            }
        }
        _tray = new TrayIcon
        {
            Icon = _idleIcon,
            ToolTipText = "CyCapture — Impr écran pour capturer",
            Menu = menu,
            Command = new ActionCommand(ShowQuickMenu),
            IsVisible = true
        };
        TrayIcon.SetIcons(Application.Current!, new TrayIcons { _tray });
    }

    private async void PrintScreenPressed(object? sender, EventArgs args)
    {
        if (_quitting) return;
        if (_recording.IsActive)
        {
            CancelPendingPrintScreenGesture();
            await StopRecordingAsync();
            return;
        }
        if (_recording.IsFinishing) return;
        if (_selection.IsSelecting)
        {
            CancelPendingPrintScreenGesture();
            _selection.CancelSelection();
            return;
        }

        if (_preferences.PrintScreenBehavior == PrintScreenBehavior.QuickImageHoldSelection)
        {
            if (_captureFlowActive || _printScreenHybridPending) return;
            _printScreenHybridPending = true;
            _printScreenHoldTimer.Stop();
            _printScreenHoldTimer.Interval = TimeSpan.FromMilliseconds(_preferences.PrintScreenHoldDelayMilliseconds);
            _printScreenHoldTimer.Start();
            return;
        }

        switch (_preferences.PrintScreenBehavior)
        {
            case PrintScreenBehavior.CaptureImage:
                await BeginCaptureAsync(CaptureMode.Image);
                break;
            case PrintScreenBehavior.CaptureVideo:
                await BeginCaptureAsync(CaptureMode.Video);
                break;
            case PrintScreenBehavior.CaptureGif:
                await BeginCaptureAsync(CaptureMode.Gif);
                break;
            case PrintScreenBehavior.CaptureAudio:
                await BeginCaptureAsync(CaptureMode.Audio);
                break;
            default:
                ShowQuickMenu();
                break;
        }
    }

    private async void PrintScreenReleased(object? sender, EventArgs args)
    {
        if (!_printScreenHybridPending) return;
        _printScreenHoldTimer.Stop();
        _printScreenHybridPending = false;
        await BeginCaptureAsync(CaptureMode.Image, CaptureSelectionMode.Smart);
    }

    private void PrintScreenHoldElapsed(object? sender, EventArgs args)
    {
        if (!_printScreenHybridPending) return;
        _printScreenHoldTimer.Stop();
        _printScreenHybridPending = false;
        ShowQuickMenu();
    }

    private void CancelPendingPrintScreenGesture()
    {
        _printScreenHoldTimer.Stop();
        _printScreenHybridPending = false;
    }

    private void StartDirectCapture(CaptureMode mode)
    {
        _quickWindow?.Close();
        _ = BeginCaptureAsync(mode);
    }

    private void ShowQuickMenu()
    {
        if (_recording.IsActive)
        {
            _ = StopRecordingAsync();
            return;
        }
        if (_recording.IsFinishing || _selection.IsSelecting || _captureFlowActive) return;
        if (_quickWindow is { IsVisible: true })
        {
            _quickWindow.Activate();
            return;
        }

        try
        {
            _quickWindow = new QuickCaptureWindow(_preferences, _postProcessing) { Icon = _idleIcon };
            _quickWindow.CaptureRequested += QuickCaptureRequested;
            _quickWindow.Closed += (_, _) => _quickWindow = null;
            _quickWindow.Show();
        }
        catch (Exception error)
        {
            _quickWindow = null;
            LogUiError("quick-capture", error);
            ShowToast("Palette indisponible", error.Message, true);
        }
    }

    private async void QuickCaptureRequested(object? sender, CaptureRequest request)
    {
        await BeginCaptureAsync(request.Mode, request.SelectionMode);
    }

    private async void SettingsCaptureRequested(object? sender, CaptureMode mode)
    {
        await BeginCaptureAsync(mode, _preferences.SelectionMode);
    }

    private async Task BeginCaptureAsync(CaptureMode mode, CaptureSelectionMode? selectionMode = null)
    {
        if (mode == CaptureMode.Audio)
        {
            await BeginAudioCaptureAsync();
            return;
        }
        if (_captureFlowActive || _recording.IsActive || _recording.IsFinishing) return;
        _captureFlowActive = true;
        try
        {
            var selection = await _selection.SelectAsync(
                selectionMode ?? _preferences.SelectionMode,
                mode == CaptureMode.Image && _preferences.EnableQuickAnnotations);
            if (selection is null) return;

            if (mode == CaptureMode.Image)
            {
                var artifact = await _images.CaptureAsync(selection, _preferences);
                await CompleteArtifactAsync(artifact);
                ShowToast("Image enregistrée", Path.GetFileName(artifact.Path), false, artifact.Path);
                return;
            }

            await _recording.StartAsync(mode, selection, _preferences);
            if (_preferences.ShowRecordingFrame) _indicator.Show(selection);
            UpdateTrayState();
        }
        catch (Exception error)
        {
            _indicator.Hide();
            ShowToast("Capture impossible", error.Message, true);
        }
        finally
        {
            _captureFlowActive = false;
        }
    }

    private async Task BeginAudioCaptureAsync()
    {
        if (_captureFlowActive || _recording.IsActive || _recording.IsFinishing) return;
        _captureFlowActive = true;
        try
        {
            var monitors = NativeMethods.GetMonitors();
            if (monitors.Count == 0) throw new InvalidOperationException("Aucun écran Windows n’a été détecté.");
            var monitor = NativeMethods.GetCursorPos(out var cursor)
                ? NativeMethods.MonitorAt(cursor.X, cursor.Y)
                : monitors.FirstOrDefault(item => item.IsPrimary) ?? monitors[0];
            var selection = new CaptureSelection(
                new PixelBounds(0, 0, 0, 0),
                monitor,
                SelectionKind.Region,
                "Audio");
            await _recording.StartAsync(CaptureMode.Audio, selection, _preferences);
            UpdateTrayState();
        }
        catch (Exception error)
        {
            ShowToast("Capture impossible", error.Message, true);
        }
        finally
        {
            _captureFlowActive = false;
        }
    }

    private async Task StopRecordingAsync()
    {
        if (!_recording.IsActive) return;
        _indicator.Hide();
        UpdateTrayState();
        try
        {
            var artifact = await _recording.StopAsync();
            if (artifact.Mode == CaptureMode.Video && _preferences.CopyVideosToClipboard)
            {
                try { WindowsClipboard.CopyFile(artifact.Path); }
                catch { /* La vidéo reste enregistrée même si une application verrouille le presse-papiers. */ }
            }
            await CompleteArtifactAsync(artifact);
            ShowToast(
                artifact.Mode switch
                {
                    CaptureMode.Gif => "GIF enregistré",
                    CaptureMode.Audio => "Audio enregistré",
                    _ => "Vidéo enregistrée"
                },
                Path.GetFileName(artifact.Path),
                false,
                artifact.Path);
        }
        catch (Exception error)
        {
            ShowToast("Finalisation impossible", error.Message, true);
        }
        finally
        {
            UpdateTrayState();
        }
    }

    private async Task CompleteArtifactAsync(CaptureArtifact artifact)
    {
        if (_testMode) return;
        await _history.AddAsync(artifact);
        await _postProcessing.RunAsync(artifact);
        if (_galleryWindow is { IsVisible: true }) await _galleryWindow.RefreshGalleryAsync();
    }

    private void UpdateTrayState()
    {
        if (_tray is null || _recordingMenuItem is null) return;
        if (_recording.IsActive)
        {
            _tray.Icon = _recordingIcon;
            _recordingMenuItem.IsEnabled = true;
            _recordingTimer.Start();
            UpdateRecordingTimer();
        }
        else if (_recording.IsFinishing)
        {
            _tray.Icon = _recordingIcon;
            _tray.ToolTipText = "CyCapture — Finalisation en cours…";
            _recordingMenuItem.Header = "Finalisation en cours…";
            _recordingMenuItem.IsEnabled = false;
            _recordingTimer.Stop();
        }
        else
        {
            _recordingTimer.Stop();
            _tray.Icon = _idleIcon;
            _tray.ToolTipText = "CyCapture — Impr écran pour capturer";
            _recordingMenuItem.Header = "Aucun enregistrement en cours";
            _recordingMenuItem.IsEnabled = false;
        }
    }

    private void UpdateRecordingTimer()
    {
        if (!_recording.IsActive || _tray is null || _recordingMenuItem is null) return;
        var elapsed = DateTimeOffset.Now - _recording.StartedAt;
        var time = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
        var kind = _recording.ActiveMode switch
        {
            CaptureMode.Gif => "GIF",
            CaptureMode.Audio => "AUDIO",
            _ => "VIDÉO"
        };
        _tray.ToolTipText = $"CyCapture — ● REC {time} · Impr écran pour arrêter";
        _recordingMenuItem.Header = $"■ Arrêter {kind} · {time}";
        _indicator.UpdateElapsed(elapsed);
    }

    private void ShowSettings()
    {
        try
        {
            if (_settingsWindow is { IsVisible: true })
            {
                _settingsWindow.Activate();
                return;
            }
            _settingsWindow = new SettingsWindow(_preferences, _postProcessing) { Icon = _idleIcon };
            _settingsWindow.CaptureRequested += SettingsCaptureRequested;
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }
        catch (Exception error)
        {
            _settingsWindow = null;
            LogUiError("settings", error);
            ShowToast("Réglages indisponibles", error.Message, true);
        }
    }

    private void ShowGallery()
    {
        try
        {
            if (_galleryWindow is { IsVisible: true })
            {
                _galleryWindow.Activate();
                _ = _galleryWindow.RefreshGalleryAsync();
                return;
            }
            _galleryWindow = new GalleryWindow(_preferences, _history) { Icon = _idleIcon };
            _galleryWindow.Closed += (_, _) => _galleryWindow = null;
            _galleryWindow.Show();
            _galleryWindow.Activate();
        }
        catch (Exception error)
        {
            _galleryWindow = null;
            LogUiError("gallery", error);
            ShowToast("Galerie indisponible", error.Message, true);
        }
    }

    private void ShowToast(string title, string message, bool error, string? filePath = null)
    {
        _toast?.Close();
        _toast = new ToastWindow(title, message, error, filePath) { Icon = error ? _recordingIcon : _idleIcon };
        _toast.Closed += (_, _) => _toast = null;
        _toast.Show();
    }

    private void OpenOutputDirectory()
    {
        Directory.CreateDirectory(_preferences.EffectiveOutputDirectory);
        Process.Start(new ProcessStartInfo("explorer.exe", _preferences.EffectiveOutputDirectory) { UseShellExecute = true });
    }

    private void OpenPluginsDirectory()
    {
        Directory.CreateDirectory(_postProcessing.PluginsDirectory);
        Process.Start(new ProcessStartInfo("explorer.exe", _postProcessing.PluginsDirectory) { UseShellExecute = true });
    }

    private async Task TogglePluginAsync(string id)
    {
        var plugin = _postProcessing.GetPlugins()
            .FirstOrDefault(item => item.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        if (plugin is null) return;
        await _postProcessing.SetEnabledAsync(id, !plugin.Enabled);
    }

    private void UpdatePluginTrayItems()
    {
        foreach (var plugin in _postProcessing.GetPlugins())
        {
            if (!_pluginTrayItems.TryGetValue(plugin.Id, out var item)) continue;
            item.Header = plugin.QuickAccessLabel ?? plugin.Name;
            item.IsChecked = plugin.Enabled;
        }
    }

    private async Task WatchExistingInstanceAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() =>
        {
            var handles = new[] { _existingInstanceSignal, cancellationToken.WaitHandle };
            while (!cancellationToken.IsCancellationRequested)
            {
                if (WaitHandle.WaitAny(handles) != 0) break;
                Dispatcher.UIThread.Post(ShowGallery);
            }
        }, cancellationToken);
    }

    private static void LogUiError(string area, Exception error)
    {
        try
        {
            Directory.CreateDirectory(Preferences.DataDirectory);
            File.AppendAllText(
                Path.Combine(Preferences.DataDirectory, "errors.log"),
                $"[{DateTimeOffset.Now:O}] {area}{Environment.NewLine}{error}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never close the tray process.
        }
    }

    private static WindowIcon CreateWindowIcon(bool recording)
    {
        var handle = NativeMethods.CreateIconHandle(recording);
        try
        {
            using var borrowed = Icon.FromHandle(handle);
            using var icon = (Icon)borrowed.Clone();
            using var stream = new MemoryStream();
            icon.Save(stream);
            stream.Position = 0;
            return new WindowIcon(stream);
        }
        finally
        {
            NativeMethods.ReleaseIconHandle(handle);
        }
    }

    private void Quit()
    {
        if (_quitting) return;
        _quitting = true;
        _shutdown.Cancel();
        _recordingTimer.Stop();
        CancelPendingPrintScreenGesture();
        _indicator.Hide();
        _printScreenHook?.Dispose();
        _recording.Dispose();
        _tray?.Dispose();
        _existingInstanceSignal.Dispose();
        _desktop.Shutdown();
    }
}
