using System.Reflection;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Fonts.Inter;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CyCapture.Models;
using CyCapture.Views;
using BergamotTranslatorSharp;
using TessEngine = Tesseract.TesseractEngine;
using TessEngineMode = Tesseract.EngineMode;

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
            CheckAudioPacketTimeline();
            CheckRecordingFrameLayout();
            CheckUpdates();
            CheckSmartSelectionZOrder();
            CheckNativeWindowSnapshot();
            CheckXamlGroups(projectRoot);
            CheckWindows(output);
            if (args.Contains("--translation-model-check", StringComparer.OrdinalIgnoreCase))
                Task.Run(CheckTranslationModels).GetAwaiter().GetResult();
            Console.WriteLine("PASS: preferences, recorder routing, audio packet timeline, recording frame, updates, documentation, smart-selection Z-order, XAML groups and window construction.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void CheckTranslationModels()
    {
        var serviceType = AppAssembly.GetType("CyCapture.Services.TranslationModelService")!;
        var service = Activator.CreateInstance(serviceType, nonPublic: true)!;
        var modelsDirectory = (string)serviceType.GetProperty("ModelsDirectory", PrivateStatic)!.GetValue(null)!;
        Console.WriteLine($"Translation models: {modelsDirectory}");
        var ensureOcr = serviceType.GetMethod("EnsureOcrAsync", PrivateInstance)!;
        Console.WriteLine("Downloading/loading Chinese OCR model…");
        var ocrTask = (Task)ensureOcr.Invoke(service, new object?[] { "zh", null, CancellationToken.None })!;
        ocrTask.GetAwaiter().GetResult();
        var ocrLanguage = (string)ocrTask.GetType().GetProperty("Result")!.GetValue(ocrTask)!;
        var ocrDirectory = (string)serviceType.GetProperty("OcrDirectory", PrivateStatic)!.GetValue(null)!;
        using (var engine = new TessEngine(ocrDirectory, ocrLanguage, TessEngineMode.LstmOnly))
            Require(engine.Version is not null, "The Chinese OCR model could not be loaded.");

        var ensureTranslation = serviceType.GetMethod("EnsureTranslationAsync", PrivateInstance)!;
        Console.WriteLine("Downloading/loading Chinese to French translation models…");
        var translationTask = (Task)ensureTranslation.Invoke(service, new object?[] { "zh", "fr", null, CancellationToken.None })!;
        translationTask.GetAwaiter().GetResult();
        var configs = ((IEnumerable<string>)translationTask.GetType().GetProperty("Result")!.GetValue(translationTask)!).ToArray();
        Require(configs.Length == 2 && configs.All(File.Exists), "The Chinese to French pivot models were not prepared.");
        using var translator = new BlockingService(configs[0], configs[1]);
        var translated = translator.Translate("你好，世界！");
        Require(!string.IsNullOrWhiteSpace(translated) && translated != "你好，世界！",
            "The local Chinese to French translation produced no result.");
        Console.WriteLine($"PASS: local zh→fr model chain ({translated}).");

        using var synthetic = new System.Drawing.Bitmap(720, 180);
        using (var graphics = System.Drawing.Graphics.FromImage(synthetic))
        using (var font = new System.Drawing.Font("Microsoft YaHei", 52, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel))
        {
            graphics.Clear(System.Drawing.Color.White);
            graphics.DrawString("你好，世界！", font, System.Drawing.Brushes.Black, new System.Drawing.PointF(24, 45));
        }
        byte[] png;
        using (var stream = new MemoryStream())
        {
            synthetic.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            png = stream.ToArray();
        }
        var bounds = new PixelBounds(0, 0, synthetic.Width, synthetic.Height);
        var monitor = new MonitorDescriptor(0, "TEST", "Synthetic", bounds, bounds, 1, true);
        var selection = new CaptureSelection(bounds, monitor, SelectionKind.Region, "Synthetic Chinese", FrozenFramePng: png);
        var preferences = new Preferences { TranslationSourceLanguage = "zh", TranslationTargetLanguage = "fr" };
        var ocrServiceType = AppAssembly.GetType("CyCapture.Services.OcrTranslationService")!;
        var ocrService = Activator.CreateInstance(ocrServiceType, PrivateInstance, null, new[] { service }, null)!;
        var recognizeAsync = ocrServiceType.GetMethod("RecognizeAsync", PrivateInstance)!;
        var recognitionTask = (Task)recognizeAsync.Invoke(
            ocrService,
            new object?[] { selection, preferences, null, CancellationToken.None })!;
        recognitionTask.GetAwaiter().GetResult();
        var recognizedLines = ((IEnumerable<OcrTextLine>)recognitionTask.GetType()
            .GetProperty("Result")!.GetValue(recognitionTask)!).ToArray();
        Require(recognizedLines.Length > 0 && recognizedLines.Any(line => !string.IsNullOrWhiteSpace(line.Text)),
            "The text-copy OCR mode did not recognize the synthetic Chinese screenshot.");
        Console.WriteLine($"PASS: text-copy OCR data ({string.Join(' ', recognizedLines.Select(line => line.Text))}).");

        var translateAsync = ocrServiceType.GetMethod("TranslateAsync", PrivateInstance)!;
        var endToEndTask = (Task)translateAsync.Invoke(
            ocrService,
            new object?[] { selection, preferences, null, CancellationToken.None })!;
        endToEndTask.GetAwaiter().GetResult();
        var result = (ScreenTranslationResult)endToEndTask.GetType().GetProperty("Result")!.GetValue(endToEndTask)!;
        Require(result.Lines.Count > 0 && result.PlainText.Length > 0,
            "The synthetic Chinese screenshot was not recognized and translated.");
        Console.WriteLine($"PASS: screenshot OCR overlay data ({result.PlainText}).");
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
        Require(defaults.AudioOnlyIgnoreSystemVolume, "Audio-only automatic level is not enabled by default.");
        Require(defaults.ApplicationLanguage == "fr", "The application language default no longer preserves the French interface.");
        Require(defaults.DocumentationLanguage == "en", "English is not the default documentation language.");
        Require(defaults.TranslationSourceLanguage == "auto" && defaults.TranslationTargetLanguage == "fr",
            "Default screen translation languages changed.");
        Require(defaults.CopyScreenshotsToClipboard
                && defaults.CopyVideosToClipboard
                && defaults.CopyGifsToClipboard
                && defaults.CopyAudioToClipboard
                && defaults.CopyTranslationsToClipboard,
            "Clipboard options are not enabled by default for every capture mode.");
        Require(defaults.ClipboardOptionsInitialized == true,
            "Clipboard defaults were not marked as initialized.");
        Require(defaults.PrintScreenBehavior == PrintScreenBehavior.QuickImageHoldSelection,
            "The hybrid Print Screen workflow is not the default.");
        Require(defaults.PrintScreenBehaviorSchemaVersion == 1,
            "The Print Screen behavior migration was not marked as completed.");

        var legacyHybridBehavior = Normalize(JsonSerializer.Deserialize<Preferences>(
            """{"PrintScreenBehavior":5}""")!);
        Require(legacyHybridBehavior.PrintScreenBehavior == PrintScreenBehavior.QuickImageHoldSelection,
            "The legacy hybrid Print Screen value was interpreted as direct translation.");
        Require(legacyHybridBehavior.PrintScreenBehaviorSchemaVersion == 1,
            "The legacy Print Screen behavior was not migrated exactly once.");

        var currentHybridBehavior = Normalize(JsonSerializer.Deserialize<Preferences>(
            """{"PrintScreenBehavior":6}""")!);
        Require(currentHybridBehavior.PrintScreenBehavior == PrintScreenBehavior.QuickImageHoldSelection,
            "The current hybrid Print Screen value changed during migration.");

        var explicitTranslationBehavior = Normalize(JsonSerializer.Deserialize<Preferences>(
            """{"PrintScreenBehavior":5,"PrintScreenBehaviorSchemaVersion":1}""")!);
        Require(explicitTranslationBehavior.PrintScreenBehavior == PrintScreenBehavior.CaptureTranslation,
            "An explicit direct translation choice was overwritten after migration.");

        var invalidPrintScreenBehavior = Normalize(new Preferences
        {
            PrintScreenBehavior = (PrintScreenBehavior)999
        });
        Require(invalidPrintScreenBehavior.PrintScreenBehavior == PrintScreenBehavior.QuickImageHoldSelection,
            "An invalid Print Screen behavior did not fall back to the hybrid workflow.");
        var legacyClipboard = Normalize(JsonSerializer.Deserialize<Preferences>(
            """{"CopyScreenshotsToClipboard":false,"CopyVideosToClipboard":false}""")!);
        Require(legacyClipboard.CopyScreenshotsToClipboard
                && legacyClipboard.CopyVideosToClipboard
                && legacyClipboard.CopyGifsToClipboard
                && legacyClipboard.CopyAudioToClipboard
                && legacyClipboard.CopyTranslationsToClipboard,
            "Legacy preferences did not receive the enabled clipboard defaults.");
        legacyClipboard.CopyAudioToClipboard = false;
        var migratedClipboard = Normalize(JsonSerializer.Deserialize<Preferences>(JsonSerializer.Serialize(legacyClipboard))!);
        Require(!migratedClipboard.CopyAudioToClipboard,
            "An explicit clipboard choice was overwritten after the one-time migration.");
        var frenchDocumentation = Normalize(new Preferences { DocumentationLanguage = "FR" });
        Require(frenchDocumentation.DocumentationLanguage == "fr", "French documentation language was not preserved.");
        var invalidDocumentation = Normalize(new Preferences { DocumentationLanguage = "de" });
        Require(invalidDocumentation.DocumentationLanguage == "en", "An unsupported documentation language was not sanitized.");
        var englishInterface = Normalize(new Preferences { ApplicationLanguage = "EN" });
        Require(englishInterface.ApplicationLanguage == "en", "The English application language was not preserved.");
        var invalidInterface = Normalize(new Preferences { ApplicationLanguage = "de" });
        Require(invalidInterface.ApplicationLanguage == "fr", "An unsupported application language did not use the French fallback.");
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
        var videoSources = (System.Collections.IEnumerable)Property(Property(video, "SourceOptions"), "RecordingSources");
        var videoSource = videoSources.Cast<object>().Single();
        Require(Property(videoSource, "RecorderApi").ToString() == "WindowsGraphicsCapture",
            "Video capture did not use the Windows Graphics Capture backend.");
        var videoAudio = Property(video, "AudioOptions");
        Require(Equals(Property(videoAudio, "IsAudioEnabled"), false), "A muted video inherited audio-only sources.");
        Require(Property(videoAudio, "Bitrate").ToString() == "bitrate_96kbps", "Video did not use its own audio profile.");
        preferences.IncludeSystemAudio = true;
        var gif = build.Invoke(null, [CaptureMode.Gif, selection, preferences])!;
        Require(Equals(Property(Property(gif, "AudioOptions"), "IsAudioEnabled"), false), "GIF audio is not disabled.");
    }

    private static void CheckRecordingFrameLayout()
    {
        var calculate = AppAssembly.GetType("CyCapture.Services.RecordingIndicatorService")!
            .GetMethod("CalculateOutlineBounds", PrivateStatic)!;
        var screen = new PixelBounds(-1920, 0, 1920, 1080);
        var capture = new PixelBounds(-1800, 100, 800, 600);
        var outline = (PixelBounds)calculate.Invoke(null, [capture, screen])!;
        Require(outline == new PixelBounds(-1802, 98, 804, 604),
            "The recording outline is offset, clipped or intrudes too far into the capture.");
    }

    private static void CheckAudioPacketTimeline()
    {
        var calculate = AppAssembly.GetType("CyCapture.Services.AudioRecordingSession+AudioCaptureTrack")!
            .GetMethod("CalculateMissingBytes", PrivateStatic)!;
        const int blockAlign = 8;
        const int averageBytesPerSecond = 384_000;
        const int tenMillisecondPacket = 3_840;
        var schedulingJitter = (long)calculate.Invoke(
            null,
            [TimeSpan.FromMilliseconds(14), tenMillisecondPacket, blockAlign, averageBytesPerSecond])!;
        Require(schedulingJitter == 0, "Normal WASAPI callback jitter inserted silence into the audio stream.");
        var realGap = (long)calculate.Invoke(
            null,
            [TimeSpan.FromMilliseconds(200), tenMillisecondPacket, blockAlign, averageBytesPerSecond])!;
        Require(realGap == 72_960 && realGap % blockAlign == 0,
            "A real WASAPI packet gap was not preserved on the audio timeline.");

        var gain = AppAssembly.GetType("CyCapture.Services.AudioRecordingSession")!
            .GetMethod("CalculateAutomaticGain", BindingFlags.NonPublic | BindingFlags.Static)!;
        var boosted = (float)gain.Invoke(null, [0.1f])!;
        Require(Math.Abs(boosted - 9.2f) < 0.01f, "Low system audio is not normalized to the target level.");
        Require((float)gain.Invoke(null, [0f])! == 1f, "Silence must not receive artificial gain.");
        Require((float)gain.Invoke(null, [0.95f])! == 1f, "Already loud system audio must not be amplified.");
    }

    private static object Property(object target, string name) => target.GetType().GetProperty(name)!.GetValue(target)!;

    private static void CheckUpdates()
    {
        Require(AppAssembly.GetType("CyCapture.ApplicationController")!
                    .GetMethod("CheckForUpdatesOnStartupAsync", PrivateInstance) is not null,
            "The startup update check is missing.");
        const string fullName = "CyCapture-1.5.1-windows-x64-portable.exe";
        const string liteName = "CyCapture-1.5.1-windows-x64-portable-without-CyAnnota.exe";
        const string installerName = "CyCapture-1.5.1-windows-x64-installer.exe";
        const string fullHash = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        const string liteHash = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
        const string installerHash = "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";
        var json = JsonSerializer.Serialize(new
        {
            tag_name = "v1.5.1",
            html_url = "https://github.com/MrMybal/CyCapture/releases/tag/v1.5.1",
            assets = new object[]
            {
                new { name = fullName, browser_download_url = $"https://github.com/MrMybal/CyCapture/releases/download/v1.5.1/{fullName}", size = 123L, digest = $"sha256:{fullHash}" },
                new { name = liteName, browser_download_url = $"https://github.com/MrMybal/CyCapture/releases/download/v1.5.1/{liteName}", size = 456L, digest = $"sha256:{liteHash}" },
                new { name = installerName, browser_download_url = $"https://github.com/MrMybal/CyCapture/releases/download/v1.5.1/{installerName}", size = 789L, digest = $"sha256:{installerHash}" }
            }
        });
        var parse = AppAssembly.GetType("CyCapture.Services.UpdateService")!
            .GetMethod("ParseLatestRelease", PrivateStatic)!;

        var full = parse.Invoke(null, [json, new Version(1, 4, 8), true, false])!;
        Require(Equals(Property(full, "IsUpdateAvailable"), true), "A newer GitHub release was not detected.");
        Require(Property(full, "AssetName").ToString() == fullName, "The full edition selected the wrong update asset.");
        Require(Property(full, "Sha256").ToString() == fullHash, "The full update digest was not preserved.");

        var lite = parse.Invoke(null, [json, new Version(1, 4, 8), false, false])!;
        Require(Property(lite, "AssetName").ToString() == liteName, "The edition without CyAnnota selected the wrong update asset.");
        Require(Property(lite, "Sha256").ToString() == liteHash, "The lite update digest was not preserved.");

        var installer = parse.Invoke(null, [json, new Version(1, 4, 8), true, true])!;
        Require(Property(installer, "AssetName").ToString() == installerName, "The installed edition selected the wrong update asset.");
        Require(Property(installer, "Sha256").ToString() == installerHash, "The installer update digest was not preserved.");

        var current = parse.Invoke(null, [json, new Version(1, 5, 1), true, false])!;
        Require(Equals(Property(current, "IsUpdateAvailable"), false), "The current version was reported as outdated.");

        var unsafeJson = json.Replace("https://github.com/MrMybal/CyCapture/releases/download/", "https://example.invalid/");
        var unsafeUpdate = parse.Invoke(null, [unsafeJson, new Version(1, 4, 8), true, false])!;
        Require(PropertyOrNull(unsafeUpdate, "DownloadUrl") is null, "An untrusted update download URL was accepted.");
    }

    private static object? PropertyOrNull(object target, string name) => target.GetType().GetProperty(name)!.GetValue(target);

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
            var textProcessingTab = file == "SettingsWindow.axaml" ? "Traduction" : "Texte & traduction";
            var expected = new Dictionary<string, string>
            {
                ["ImageFormatCombo"] = "Image", ["ImageEncodingQualityCombo"] = "Image",
                ["VideoQualityCombo"] = "Vidéo", ["VideoEncodingQualityCombo"] = "Vidéo",
                ["AudioEncodingQualityCombo"] = "Vidéo", ["GifQualityCombo"] = "GIF",
                ["AudioOnlyEncodingQualityCombo"] = "Audio", ["AudioOnlySystemCheck"] = "Audio",
                ["AudioOnlyMicrophoneCheck"] = "Audio", ["AudioOnlyIgnoreSystemVolumeCheck"] = "Audio",
                ["TranslationSourceLanguageCombo"] = textProcessingTab, ["TranslationTargetLanguageCombo"] = textProcessingTab
            };
            expected[file == "SettingsWindow.axaml" ? "SystemAudioCheck" : "VideoSystemAudioCheck"] = "Vidéo";
            expected[file == "SettingsWindow.axaml" ? "MicrophoneCheck" : "VideoMicrophoneCheck"] = "Vidéo";
            if (file == "SettingsWindow.axaml")
            {
                expected["ClipboardCheck"] = "Image";
                expected["VideoClipboardCheck"] = "Vidéo";
                expected["GifClipboardCheck"] = "GIF";
                expected["AudioClipboardCheck"] = "Audio";
                expected["TranslationClipboardCheck"] = "Traduction";
            }
            foreach (var entry in expected)
            {
                var control = document.Descendants().Single(element => (string?)element.Attribute(x + "Name") == entry.Key);
                Require((string?)control.Ancestors().First(element => element.Name.LocalName == "TabItem").Attribute("Header") == entry.Value,
                    $"{file}: {entry.Key} is in the wrong tab.");
            }
        }

        var settingsDocument = XDocument.Load(Path.Combine(root, "CyCapture.App", "Views", "SettingsWindow.axaml"));
        XNamespace settingsX = "http://schemas.microsoft.com/winfx/2006/xaml";
        var printScreenBehaviorCombo = settingsDocument.Descendants()
            .Single(element => (string?)element.Attribute(settingsX + "Name") == "PrintScreenBehaviorCombo");
        var printScreenBehaviorItems = printScreenBehaviorCombo.Elements().ToArray();
        var printScreenBehaviorTags = printScreenBehaviorItems
            .Select(element => (string?)element.Attribute("Tag"))
            .ToHashSet(StringComparer.Ordinal);
        Require((string?)printScreenBehaviorCombo.Attribute("SelectedIndex") == "0"
                && (string?)printScreenBehaviorItems.First().Attribute("Tag") == "QuickImageHoldSelection",
            "The hybrid Print Screen workflow is not the UI fallback selection.");
        Require(printScreenBehaviorTags.Contains("CaptureText")
                && printScreenBehaviorTags.Contains("CaptureTranslation")
                && printScreenBehaviorTags.Contains("QuickImageHoldSelection"),
            "The Print Screen behavior list is missing Text, Translation or the hybrid default.");

        var quickDocument = XDocument.Load(Path.Combine(root, "CyCapture.App", "Views", "QuickCaptureWindow.axaml"));
        Require(quickDocument.Descendants().Any(element => (string?)element.Attribute("Click") == "CopyTextClick"),
            "The quick palette is missing the text-copy OCR mode.");
        Require(settingsDocument.Descendants().Any(element => (string?)element.Attribute(settingsX + "Name") == "ApplicationLanguageCombo"),
            "The application language selector is missing.");
        Require(settingsDocument.Descendants().Any(element => (string?)element.Attribute(settingsX + "Name") == "DocumentationLanguageCombo"),
            "The documentation language selector is missing.");
        Require(!settingsDocument.Descendants().Any(element =>
                string.Equals((string?)element.Attribute("Text"), "CAPTURE RAPIDE", StringComparison.Ordinal)),
            "Quick capture actions are still embedded in the settings window.");
    }

    private static void CheckWindows(string output)
    {
        AppBuilder.Configure<CyCapture.App>().UsePlatformDetect().WithInterFont().SetupWithoutStarting();
        // No desktop lifetime: no controller, tray, keyboard hook, recording or real preference writes.
        CheckQuickAnnotationToolbar();
        CheckTranslationWindows(output);
        var processorType = AppAssembly.GetType("CyCapture.Services.PostProcessingService")!;
        var processors = Activator.CreateInstance(processorType, PrivateInstance, null, new object?[] { Path.Combine(output, "plugins"), false, null }, null)!;
        var preferences = IndependentPreferences();
        var settings = (SettingsWindow)Activator.CreateInstance(typeof(SettingsWindow), PrivateInstance, null, new object?[] { preferences, processors }, null)!;
        var quick = (QuickCaptureWindow)Activator.CreateInstance(typeof(QuickCaptureWindow), PrivateInstance, null, new object?[] { preferences, processors }, null)!;
        CheckCyDocDistribution();
        Require(settings.FindControl<CheckBox>("SystemAudioCheck")!.IsChecked == false, "Video source initialized incorrectly.");
        Require(settings.FindControl<CheckBox>("AudioOnlySystemCheck")!.IsChecked == true, "Audio-only source initialized incorrectly.");
        Require(settings.FindControl<CheckBox>("AudioOnlyIgnoreSystemVolumeCheck")!.IsChecked == true,
            "Audio-only automatic level was not initialized as enabled.");
        Require(settings.FindControl<CheckBox>("FrameCheck")!.IsChecked == false && settings.FindControl<CheckBox>("GifFrameCheck")!.IsChecked == true,
            "Frame options are not independent.");
        foreach (var clipboardControl in new[]
                 {
                     "ClipboardCheck", "VideoClipboardCheck", "GifClipboardCheck", "AudioClipboardCheck",
                     "TranslationClipboardCheck"
                 })
            Require(settings.FindControl<CheckBox>(clipboardControl)!.IsChecked == true,
                $"{clipboardControl} was not initialized as enabled.");
        var versionText = settings.FindControl<TextBlock>("AppVersionText")!.Text ?? string.Empty;
        var editionLabel = (string)AppAssembly.GetType("CyCapture.Services.AppBuildInfo")!
            .GetProperty("EditionLabel", PrivateStatic)!.GetValue(null)!;
        Require(versionText.Contains("1.5.2", StringComparison.Ordinal),
            "The settings window does not show the application version.");
        Require(versionText.Contains(editionLabel, StringComparison.Ordinal),
            "The settings window does not show the compiled edition.");
        Require(settings.FindControl<Button>("UpdateButton") is not null,
            "The settings window has no update button.");
        Require(settings.FindControl<Button>("DocumentationButton") is not null,
            "The settings window has no documentation button.");
        Require(((ComboBoxItem)settings.FindControl<ComboBox>("DocumentationLanguageCombo")!.SelectedItem!).Tag?.ToString() == "en",
            "The CyDoc language selector did not use the default English locale.");
        Require(((ComboBoxItem)settings.FindControl<ComboBox>("ApplicationLanguageCombo")!.SelectedItem!).Tag?.ToString() == "fr",
            "The application language selector is missing or did not use the interface locale.");
        Require(((ComboBoxItem)quick.FindControl<ComboBox>("AudioEncodingQualityCombo")!.SelectedItem!).Tag?.ToString() == "Compact", "Quick video quality mismatch.");
        Require(((ComboBoxItem)quick.FindControl<ComboBox>("AudioOnlyEncodingQualityCombo")!.SelectedItem!).Tag?.ToString() == "High", "Quick audio-only quality mismatch.");
        Require(quick.FindControl<CheckBox>("AudioOnlyIgnoreSystemVolumeCheck")!.IsChecked == true,
            "Quick audio-only automatic level mismatch.");
        var tabs = settings.FindControl<TabControl>("CaptureSettingsTabs")!;
        Require(tabs.Items.Count == 5, "Five capture settings tabs were expected.");
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
        RenderWindowContent(quickContent, 860, 500, Path.Combine(output, "quick-window.png"));
        settings.Close();
        quick.Close();

        var englishPreferences = IndependentPreferences();
        englishPreferences.ApplicationLanguage = "en";
        var englishSettings = (SettingsWindow)Activator.CreateInstance(
            typeof(SettingsWindow),
            PrivateInstance,
            null,
            new object?[] { englishPreferences, processors },
            null)!;
        Require(englishSettings.Title == "CyCapture · Settings", "The application language does not translate the settings window title.");
        Require(englishSettings.GetLogicalDescendants().OfType<TextBlock>()
                .Any(text => text.Text == "APPLICATION LANGUAGE"),
            "The English application interface was not applied.");
        Require(englishSettings.FindControl<TabControl>("CaptureSettingsTabs")!.Items
                .OfType<TabItem>().Any(tab => Equals(tab.Header, "Video")),
            "The English capture settings tabs were not applied.");
        englishSettings.Close();
    }

    private static void CheckTranslationWindows(string output)
    {
        byte[] png;
        using (var bitmap = new System.Drawing.Bitmap(800, 500))
        using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
        using (var font = new System.Drawing.Font("Segoe UI", 28, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel))
        using (var stream = new MemoryStream())
        {
            graphics.Clear(System.Drawing.Color.FromArgb(20, 28, 27));
            graphics.DrawString("Hello world", font, System.Drawing.Brushes.White, new System.Drawing.PointF(120, 180));
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            png = stream.ToArray();
        }
        var monitorBounds = new PixelBounds(0, 0, 800, 500);
        var monitor = new MonitorDescriptor(0, "TEST", "Synthetic", monitorBounds, monitorBounds, 1, true);
        var selection = new CaptureSelection(new PixelBounds(100, 150, 400, 100), monitor, SelectionKind.Region, "Synthetic", FrozenFramePng: png);
        var progressType = AppAssembly.GetType("CyCapture.Views.TranslationProgressWindow")!;
        var calculatePosition = progressType.GetMethod("CalculatePosition", PrivateStatic)!;
        var progressPosition = (PixelPoint)calculatePosition.Invoke(null, new object?[] { selection, 410d, 155d })!;
        Require(progressPosition == new PixelPoint(95, 123),
            $"The text-processing progress window is not centered on the selection ({progressPosition}).");
        using (var progressCancellation = new CancellationTokenSource())
        {
            var progressWindow = (Window)Activator.CreateInstance(
                progressType,
                PrivateInstance,
                null,
                new object?[] { progressCancellation, selection, false },
                null)!;
            RenderWindowContent(
                (Control)progressWindow.Content!,
                410,
                155,
                Path.Combine(output, "text-copy-progress.png"));
            progressWindow.Close();
        }
        var result = new ScreenTranslationResult(
            selection,
            png,
            "en",
            "fr",
            [new TranslatedTextLine(new PixelBounds(20, 20, 250, 45), "Hello world", "Bonjour le monde", 95)]);
        var overlayType = AppAssembly.GetType("CyCapture.Views.TranslationOverlayWindow")!;
        var overlay = (Window)Activator.CreateInstance(overlayType, PrivateInstance, null, new object?[] { result }, null)!;
        RenderWindowContent((Control)overlay.Content!, 800, 500, Path.Combine(output, "translation-overlay.png"));
        overlay.Close();

        var modelServiceType = AppAssembly.GetType("CyCapture.Services.TranslationModelService")!;
        var modelService = Activator.CreateInstance(modelServiceType, nonPublic: true)!;
        var managerType = AppAssembly.GetType("CyCapture.Views.TranslationModelManagerWindow")!;
        var manager = (Window)Activator.CreateInstance(
            managerType,
            PrivateInstance,
            null,
            new object?[] { new Preferences(), modelService },
            null)!;
        manager.WindowStartupLocation = WindowStartupLocation.Manual;
        manager.Position = new PixelPoint(-30000, -30000);
        manager.ShowInTaskbar = false;
        manager.ShowActivated = false;
        manager.Show();
        SettleAnimations();
        manager.UpdateLayout();
        SaveVisual((Control)manager.Content!, Path.Combine(output, "translation-model-manager.png"));
        manager.Close();
    }

    private static void CheckCyDocDistribution()
    {
        var bundledDocumentation = AppAssembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("CyCapture.Bundled.Documentation/", StringComparison.Ordinal))
            .Select(name => name.Replace('\\', '/'))
            .ToArray();
        Require(bundledDocumentation.Any(name => name.EndsWith("Documentation.html", StringComparison.Ordinal))
                && bundledDocumentation.Any(name => name.EndsWith("DocData/manifest.json", StringComparison.Ordinal)),
            "The standalone portable executable is missing its bundled CyDoc fallback.");

        var documentationRoot = Path.Combine(AppContext.BaseDirectory, "Documentation");
        foreach (var required in new[]
                 {
                     "Documentation.html", "DocAI.md", "DocData/viewer.js", "DocData/viewer.css", "DocData/data.js",
                     "DocData/manifest.json", "DocData/translation-report.json", "DocData/media-image.js",
                     "DocData/media-gallery.js", "DocData/assets/cycapture-logo.svg", "DocData/assets/licenses/CyDoc-MIT.txt"
                 })
            Require(File.Exists(Path.Combine(documentationRoot, required.Replace('/', Path.DirectorySeparatorChar))),
                $"CyDoc distribution is missing {required}.");
        Require(!File.Exists(Path.Combine(documentationRoot, "DocData", "media-video.js"))
                && !File.Exists(Path.Combine(documentationRoot, "DocData", "media-gif.js"))
                && !File.Exists(Path.Combine(documentationRoot, "DocData", "media-mesh.js"))
                && !File.Exists(Path.Combine(documentationRoot, "DocData", "media-graph.js")),
            "CyDoc distribution contains an unselected media module.");

        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(documentationRoot, "DocData", "manifest.json")));
        Require(manifest.RootElement.GetProperty("defaultLocale").GetString() == "en", "CyDoc default locale is not English.");
        var pages = manifest.RootElement.GetProperty("pages").EnumerateArray().ToArray();
        var englishIds = pages.Where(page => page.GetProperty("locale").GetString() == "en")
            .Select(page => page.GetProperty("id").GetString()).ToHashSet();
        var frenchIds = pages.Where(page => page.GetProperty("locale").GetString() == "fr")
            .Select(page => page.GetProperty("id").GetString()).ToHashSet();
        Require(englishIds.SetEquals(frenchIds), "CyDoc page identifiers differ between English and French.");
        Require(new[] { "getting-started", "video", "gallery", "plugins", "plugin-api", "troubleshooting", "licenses" }.All(englishIds.Contains),
            "CyDoc is missing a required help page.");
        Require(!englishIds.Contains("development"), "Internal maintenance content leaked into the human manual.");
        Require(pages.All(page => !page.TryGetProperty("kind", out var kind) || kind.GetString() != "source"),
            "CyDoc exposes a raw source browsing page.");

        using var report = JsonDocument.Parse(File.ReadAllText(Path.Combine(documentationRoot, "DocData", "translation-report.json")));
        Require(report.RootElement.GetProperty("missing").GetArrayLength() == 0
                && report.RootElement.GetProperty("outdated").GetArrayLength() == 0
                && report.RootElement.GetProperty("untracked").GetArrayLength() == 0,
            "CyDoc translation report is not current.");

        var routeType = AppAssembly.GetType("CyCapture.Services.CyDocRoute")!;
        var route = Activator.CreateInstance(
            routeType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            new object?[] { "video", "recording", new Dictionary<string, string> { ["media"] = "intro clip", ["time"] = "1.25" } },
            null)!;
        var serviceType = AppAssembly.GetType("CyCapture.Services.CyDocHelpService")!;
        var extractionRoot = Path.Combine(Path.GetTempPath(), $"CyCapture-Documentation-{Guid.NewGuid():N}");
        try
        {
            var extract = serviceType.GetMethod("ExtractBundledDocumentation", PrivateStatic)!;
            extract.Invoke(null, new object?[] { AppAssembly, AppAssembly.GetManifestResourceNames()
                .Where(name => name.StartsWith("CyCapture.Bundled.Documentation/", StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal).ToArray(), extractionRoot });
            Require(File.Exists(Path.Combine(extractionRoot, "Documentation.html"))
                    && File.Exists(Path.Combine(extractionRoot, "DocData", "manifest.json")),
                "The portable CyDoc fallback could not be extracted.");
        }
        finally
        {
            if (Directory.Exists(extractionRoot))
                Directory.Delete(extractionRoot, recursive: true);
        }

        var createUri = serviceType.GetMethod("CreateUri", PrivateStatic)!;
        var uri = (Uri)createUri.Invoke(null, new object?[] { Path.Combine(Path.GetTempPath(), "CyCapture Help", "Documentation.html"), "fr", route })!;
        Require(uri.IsFile && uri.AbsoluteUri.Contains("#/fr/video/recording?media=intro%20clip&time=1.25", StringComparison.Ordinal),
            "CyDoc contextual file URL is invalid or unstable.");

        var prepareUri = serviceType.GetMethod("PrepareUriAsync", PrivateStatic)!;
        var preparedUri = Task.Run(async () =>
        {
            var preparation = (Task<Uri>)prepareUri.Invoke(null, new object?[] { "fr", route, CancellationToken.None })!;
            return await preparation.ConfigureAwait(false);
        }).GetAwaiter().GetResult();
        Require(preparedUri.IsFile && File.Exists(preparedUri.LocalPath), "The distributed CyDoc reader did not resolve to a local file URL.");
        Require(File.Exists(Path.Combine(Path.GetDirectoryName(preparedUri.LocalPath)!, "DocData", "media-image.js"))
                && File.Exists(Path.Combine(Path.GetDirectoryName(preparedUri.LocalPath)!, "DocData", "media-gallery.js")),
            "The distributed CyDoc reader is missing selected modules.");
    }

    private static void CheckQuickAnnotationToolbar()
    {
        using var screenshot = new RenderTargetBitmap(new PixelSize(64, 64), new Vector(96, 96));
        var bounds = new PixelBounds(0, 0, 64, 64);
        var monitor = new MonitorDescriptor(0, "TEST", "Test", bounds, bounds, 1, true);
        var sessionType = AppAssembly.GetType("CyCapture.Views.QuickAnnotationSession")!;
        var session = Activator.CreateInstance(sessionType, nonPublic: true)!;
        var overlayType = AppAssembly.GetType("CyCapture.Views.SelectionOverlayWindow")!;
        var overlay = (Window)Activator.CreateInstance(
            overlayType,
            PrivateInstance,
            binder: null,
            args:
            [
                screenshot,
                null,
                monitor,
                Array.Empty<SelectableRegion>(),
                Array.Empty<SelectableWindowLayer>(),
                CaptureSelectionMode.Smart,
                true,
                session
            ],
            culture: null)!;

        var content = (Control)overlay.Content!;
        var buttons = content.GetLogicalDescendants()
            .OfType<Button>()
            .Where(button => !string.IsNullOrWhiteSpace(button.Name))
            .ToDictionary(button => button.Name!);
        foreach (var name in new[]
                 {
                     "QuickAnnotationSelectionButton",
                     "QuickAnnotationFreehandButton",
                     "QuickAnnotationRectangleButton",
                     "QuickAnnotationArrowButton",
                     "QuickAnnotationTextButton"
                 })
        {
            if (!buttons.TryGetValue(name, out var button))
                throw new InvalidOperationException($"The quick-annotation toolbar is missing {name}.");
            Require(button.ClickMode == ClickMode.Press, $"{name} does not react on the first pointer press.");
        }

        overlay.Close();

        var frozenFrame = new byte[] { 1, 2, 3, 4 };
        var surfaceType = AppAssembly.GetType("CyCapture.Views.SelectionSurface")!;
        var translationSurface = Activator.CreateInstance(
            surfaceType,
            PrivateInstance,
            null,
            new object?[]
            {
                screenshot,
                frozenFrame,
                monitor,
                Array.Empty<SelectableRegion>(),
                Array.Empty<SelectableWindowLayer>(),
                CaptureSelectionMode.Region,
                false,
                session
            },
            null)!;
        CaptureSelection? completed = null;
        EventHandler<CaptureSelection> completedHandler = (_, selection) => completed = selection;
        surfaceType.GetEvent("SelectionCompleted", PrivateInstance)!
            .GetAddMethod(nonPublic: true)!
            .Invoke(translationSurface, new object?[] { completedHandler });
        surfaceType.GetMethod("Complete", PrivateInstance)!.Invoke(
            translationSurface,
            new object?[] { bounds, SelectionKind.Region, "Translation", (nint)0 });
        Require(completed?.FrozenFramePng?.SequenceEqual(frozenFrame) == true,
            "A translation selection did not preserve its frozen desktop frame.");
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
