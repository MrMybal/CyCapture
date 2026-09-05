using System.Reflection;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Fonts.Inter;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CyCapture.Models;
using CyCapture.Views;

internal static class Program
{
    private const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly Assembly AppAssembly = typeof(Preferences).Assembly;

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            var projectRoot = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
            var output = Path.Combine(projectRoot, "artifacts", "settings-checks");
            Directory.CreateDirectory(output);
            CheckPreferences();
            CheckRecorderOptions();
            CheckSmartSelectionZOrder();
            CheckNativeWindowSnapshot();
            CheckXamlGroups(projectRoot);
            CheckWindows(output);
            Console.WriteLine("PASS: preferences, recorder routing, smart-selection Z-order, XAML groups and window construction.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static Preferences Normalize(Preferences preferences) =>
        (Preferences)typeof(Preferences).GetMethod("Sanitize", PrivateStatic)!.Invoke(null, [preferences])!;

    private static (bool SystemAudio, bool Microphone, AudioEncodingQuality Quality) Audio(Preferences preferences, CaptureMode mode) =>
        ((bool, bool, AudioEncodingQuality))typeof(Preferences).GetMethod("GetAudioSettings", PrivateInstance)!.Invoke(preferences, [mode])!;

    private static bool Frame(Preferences preferences, CaptureMode mode) =>
        (bool)typeof(Preferences).GetMethod("ShouldShowRecordingFrame", PrivateInstance)!.Invoke(preferences, [mode])!;

    private static void CheckPreferences()
    {
        var legacy = Normalize(JsonSerializer.Deserialize<Preferences>(
            """{"IncludeSystemAudio":false,"IncludeMicrophone":true,"AudioEncodingQuality":2,"ShowRecordingFrame":false}""")!);
        Require(Audio(legacy, CaptureMode.Audio) == (false, true, AudioEncodingQuality.High), "Legacy audio options were not preserved.");
        Require(!Frame(legacy, CaptureMode.Gif), "Legacy GIF frame option was not preserved.");
        legacy.IncludeSystemAudio = true;
        legacy.IncludeMicrophone = false;
        legacy.AudioEncodingQuality = AudioEncodingQuality.Compact;
        legacy.ShowRecordingFrame = true;
        Require(Audio(legacy, CaptureMode.Audio) == (false, true, AudioEncodingQuality.High), "Video changes leaked into audio-only settings.");
        Require(!Frame(legacy, CaptureMode.Gif), "Video frame change leaked into GIF settings.");
        legacy.AudioOnlyIncludeSystemAudio = true;
        legacy.AudioOnlyIncludeMicrophone = true;
        legacy.AudioOnlyEncodingQuality = AudioEncodingQuality.Balanced;
        legacy.ShowGifRecordingFrame = true;
        Require(Audio(legacy, CaptureMode.Video) == (true, false, AudioEncodingQuality.Compact), "Audio-only changes leaked into video settings.");
        var roundTrip = Normalize(JsonSerializer.Deserialize<Preferences>(JsonSerializer.Serialize(legacy))!);
        Require(Audio(roundTrip, CaptureMode.Audio) == (true, true, AudioEncodingQuality.Balanced), "Audio-only settings did not survive serialization.");
        Require(Audio(roundTrip, CaptureMode.Video) == (true, false, AudioEncodingQuality.Compact), "Video settings did not survive serialization.");
        Require(Frame(roundTrip, CaptureMode.Gif), "GIF frame setting did not survive serialization.");
        foreach (var mode in new[] { CaptureMode.Image, CaptureMode.Gif })
            Require(Audio(roundTrip, mode).SystemAudio == false && Audio(roundTrip, mode).Microphone == false, "A silent mode has an audio source.");
        var defaults = Normalize(new Preferences());
        Require(Audio(defaults, CaptureMode.Audio) == (true, false, AudioEncodingQuality.Balanced), "Default audio-only settings changed.");
        defaults.AudioOnlyEncodingQuality = (AudioEncodingQuality)999;
        Require(Audio(Normalize(defaults), CaptureMode.Audio).Quality == AudioEncodingQuality.Balanced, "Invalid audio profile was not sanitized.");
    }

    private static Preferences IndependentPreferences() => Normalize(new Preferences
    {
        IncludeSystemAudio = false,
        IncludeMicrophone = false,
        AudioEncodingQuality = AudioEncodingQuality.Compact,
        AudioOnlyIncludeSystemAudio = true,
        AudioOnlyIncludeMicrophone = true,
        AudioOnlyEncodingQuality = AudioEncodingQuality.High,
        ShowRecordingFrame = false,
        ShowGifRecordingFrame = true
    });

    private static void CheckRecorderOptions()
    {
        var preferences = IndependentPreferences();
        var bounds = new PixelBounds(0, 0, 640, 480);
        var monitor = new MonitorDescriptor(0, "TEST", "Test", bounds, bounds, 1, true);
        var selection = new CaptureSelection(bounds, monitor, SelectionKind.Region, "Settings check");
        var build = AppAssembly.GetType("CyCapture.Services.RecordingService")!.GetMethod("BuildOptions", PrivateStatic)!;
        var video = build.Invoke(null, [CaptureMode.Video, selection, preferences])!;
        var videoAudio = Property(video, "AudioOptions");
        Require(Equals(Property(videoAudio, "IsAudioEnabled"), false), "A muted video inherited audio-only sources.");
        Require(Property(videoAudio, "Bitrate").ToString() == "bitrate_96kbps", "Video did not use its own audio profile.");
        preferences.IncludeSystemAudio = true;
        var gif = build.Invoke(null, [CaptureMode.Gif, selection, preferences])!;
        Require(Equals(Property(Property(gif, "AudioOptions"), "IsAudioEnabled"), false), "GIF audio is not disabled.");
    }

    private static object Property(object target, string name) => target.GetType().GetProperty(name)!.GetValue(target)!;

    private static void CheckSmartSelectionZOrder()
    {
        const nint front = 100;
        const nint back = 200;
        const nint child = 110;
        const nint deepestChild = 111;
        const nint overlappingSibling = 112;
        var frontBounds = new PixelBounds(0, 0, 400, 300);
        var backBounds = new PixelBounds(40, 40, 180, 140);
        var regions = new List<SelectableRegion>
        {
            new(front, "Front", SelectionKind.Window, frontBounds, 10, front),
            new(front, "Front content", SelectionKind.Client, new PixelBounds(8, 30, 384, 262), 20, front),
            new(child, "Front child", SelectionKind.Control, new PixelBounds(50, 50, 160, 100), 30, front),
            new(deepestChild, "Front deepest child", SelectionKind.Control, new PixelBounds(70, 70, 90, 60), 30, front),
            new(overlappingSibling, "Covered sibling", SelectionKind.Control, new PixelBounds(75, 75, 20, 20), 30, front),
            new(back, "Back", SelectionKind.Window, backBounds, 10, back),
            new(back, "Back content", SelectionKind.Client, backBounds, 20, back)
        };
        var layers = new List<SelectableWindowLayer>
        {
            new(front, frontBounds, 0),
            new(back, backBounds, 1)
        };

        var resolved = ResolveSelection(regions, layers, 80, 80, CaptureSelectionMode.Smart, [child, deepestChild]);
        Require(resolved is { Handle: deepestChild }, "Smart selection ignored the visible child-window path.");
        resolved = ResolveSelection(regions, layers, 80, 80, CaptureSelectionMode.Smart, [child]);
        Require(resolved is { Handle: child }, "A smaller overlapping sibling replaced the actual visible child.");
        resolved = ResolveSelection(regions, layers, 80, 80, CaptureSelectionMode.Smart, [back]);
        Require(resolved is { Handle: front, Kind: SelectionKind.Client }, "A covered back window bypassed the front window.");
        resolved = ResolveSelection(regions, layers, 80, 80, CaptureSelectionMode.Window, []);
        Require(resolved is { Handle: front, Kind: SelectionKind.Window }, "Window mode ignored top-level Z-order.");

        var blockingLayers = new List<SelectableWindowLayer>
        {
            new(300, new PixelBounds(60, 60, 80, 80), 0),
            new(back, backBounds, 1)
        };
        resolved = ResolveSelection(regions, blockingLayers, 80, 80, CaptureSelectionMode.Smart, []);
        Require(resolved is null, "A non-selectable visible window did not occlude the window behind it.");
    }

    private static SelectableRegion? ResolveSelection(
        IReadOnlyList<SelectableRegion> regions,
        IReadOnlyList<SelectableWindowLayer> layers,
        int x,
        int y,
        CaptureSelectionMode mode,
        IReadOnlyList<nint> childPath) =>
        (SelectableRegion?)AppAssembly.GetType("CyCapture.Views.SelectionCandidateResolver")!
            .GetMethod("Resolve", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, [regions, layers, x, y, mode, childPath]);

    private static void CheckNativeWindowSnapshot()
    {
        var snapshot = AppAssembly.GetType("CyCapture.Platform.Windows.NativeMethods")!
            .GetMethod("EnumerateSelectableRegions", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, null)!;
        var snapshotType = snapshot.GetType();
        var regions = (IReadOnlyList<SelectableRegion>)snapshotType.GetField("Item1")!.GetValue(snapshot)!;
        var layers = (IReadOnlyList<SelectableWindowLayer>)snapshotType.GetField("Item2")!.GetValue(snapshot)!;
        Require(layers.Count > 0, "The native Windows Z-order snapshot is empty.");
        Require(layers.Select(item => item.ZOrder).SequenceEqual(Enumerable.Range(0, layers.Count)),
            "The native Windows layers are not stored in Z-order.");
        var layerHandles = layers.Select(item => item.Handle).ToHashSet();
        Require(regions.All(item => item.RootWindowHandle != 0 && layerHandles.Contains(item.RootWindowHandle)),
            "A selectable region is not associated with its top-level window layer.");
    }

    private static void CheckXamlGroups(string root)
    {
        foreach (var file in new[] { "SettingsWindow.axaml", "QuickCaptureWindow.axaml" })
        {
            var document = XDocument.Load(Path.Combine(root, "CyCapture.App", "Views", file));
            XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
            var expected = new Dictionary<string, string>
            {
                ["ImageFormatCombo"] = "Image", ["ImageEncodingQualityCombo"] = "Image",
                ["VideoQualityCombo"] = "Vidéo", ["VideoEncodingQualityCombo"] = "Vidéo",
                ["AudioEncodingQualityCombo"] = "Vidéo", ["GifQualityCombo"] = "GIF",
                ["AudioOnlyEncodingQualityCombo"] = "Audio", ["AudioOnlySystemCheck"] = "Audio",
                ["AudioOnlyMicrophoneCheck"] = "Audio"
            };
            expected[file == "SettingsWindow.axaml" ? "SystemAudioCheck" : "VideoSystemAudioCheck"] = "Vidéo";
            expected[file == "SettingsWindow.axaml" ? "MicrophoneCheck" : "VideoMicrophoneCheck"] = "Vidéo";
            foreach (var entry in expected)
            {
                var control = document.Descendants().Single(element => (string?)element.Attribute(x + "Name") == entry.Key);
                Require((string?)control.Ancestors().First(element => element.Name.LocalName == "TabItem").Attribute("Header") == entry.Value,
                    $"{file}: {entry.Key} is in the wrong tab.");
            }
        }
    }

    private static void CheckWindows(string output)
    {
        AppBuilder.Configure<CyCapture.App>().UsePlatformDetect().WithInterFont().SetupWithoutStarting();
        // No desktop lifetime: no controller, tray, keyboard hook, recording or real preference writes.
        var processorType = AppAssembly.GetType("CyCapture.Services.PostProcessingService")!;
        var processors = Activator.CreateInstance(processorType, PrivateInstance, null, new object?[] { Path.Combine(output, "plugins"), false, null }, null)!;
        var preferences = IndependentPreferences();
        var settings = (SettingsWindow)Activator.CreateInstance(typeof(SettingsWindow), PrivateInstance, null, new object?[] { preferences, processors }, null)!;
        var quick = (QuickCaptureWindow)Activator.CreateInstance(typeof(QuickCaptureWindow), PrivateInstance, null, new object?[] { preferences, processors }, null)!;
        Require(settings.FindControl<CheckBox>("SystemAudioCheck")!.IsChecked == false, "Video source initialized incorrectly.");
        Require(settings.FindControl<CheckBox>("AudioOnlySystemCheck")!.IsChecked == true, "Audio-only source initialized incorrectly.");
        Require(settings.FindControl<CheckBox>("FrameCheck")!.IsChecked == false && settings.FindControl<CheckBox>("GifFrameCheck")!.IsChecked == true,
            "Frame options are not independent.");
        Require(((ComboBoxItem)quick.FindControl<ComboBox>("AudioEncodingQualityCombo")!.SelectedItem!).Tag?.ToString() == "Compact", "Quick video quality mismatch.");
        Require(((ComboBoxItem)quick.FindControl<ComboBox>("AudioOnlyEncodingQualityCombo")!.SelectedItem!).Tag?.ToString() == "High", "Quick audio-only quality mismatch.");
        var tabs = settings.FindControl<TabControl>("CaptureSettingsTabs")!;
        Require(tabs.Items.Count == 4, "Four capture settings tabs were expected.");
        settings.WindowStartupLocation = WindowStartupLocation.Manual;
        settings.Position = new PixelPoint(-30000, -30000);
        settings.ShowInTaskbar = false;
        settings.ShowActivated = false;
        settings.Show();
        foreach (var tab in tabs.Items.OfType<TabItem>())
        {
            tabs.SelectedItem = tab;
            SettleAnimations();
            settings.UpdateLayout();
            var panel = (Control)tab.Content!;
            Require(panel.Bounds.Height >= panel.DesiredSize.Height - 1, $"The {tab.Header} tab content is clipped.");
            SaveVisual(panel, Path.Combine(output, $"settings-{tab.Header}.png"));
        }
        tabs.SelectedIndex = 1;
        SettleAnimations();
        settings.UpdateLayout();
        SaveVisual((Control)settings.Content!, Path.Combine(output, "settings-window.png"));
        var quickContent = (Control)quick.Content!;
        RenderWindowContent(quickContent, 720, 500, Path.Combine(output, "quick-window.png"));
        settings.Close();
        quick.Close();
    }

    private static void RenderWindowContent(Control content, int width, int height, string path)
    {
        Dispatcher.UIThread.RunJobs();
        content.Measure(new Size(width, height));
        content.Arrange(new Rect(0, 0, width, height));
        content.UpdateLayout();
        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        bitmap.Render(content);
        bitmap.Save(path, PngBitmapEncoderOptions.Default);
    }

    private static void SettleAnimations()
    {
        using var cancellation = new System.Threading.CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        Dispatcher.UIThread.MainLoop(cancellation.Token);
    }

    private static void SaveVisual(Control content, string path)
    {
        var width = Math.Max(1, (int)Math.Ceiling(content.Bounds.Width));
        var height = Math.Max(1, (int)Math.Ceiling(content.Bounds.Height));
        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        bitmap.Render(content);
        bitmap.Save(path, PngBitmapEncoderOptions.Default);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
