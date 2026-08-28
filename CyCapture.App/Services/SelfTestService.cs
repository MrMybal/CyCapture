using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json;
using CyCapture.Models;
using CyCapture.Platform.Windows;
using CyCapture.Plugins;

namespace CyCapture.Services;

internal sealed class SelfTestService
{
    internal async Task RunAsync(string reportPath)
    {
        var generatedRoot = Path.Combine(Path.GetTempPath(), "CyCapture", "self-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(generatedRoot);
        var checks = new Dictionary<string, object?>();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var monitors = NativeMethods.GetMonitors();
            checks["monitorCount"] = monitors.Count;
            checks["monitors"] = monitors.Select(item => new
            {
                item.DeviceName,
                item.Bounds.X,
                item.Bounds.Y,
                item.Bounds.Width,
                item.Bounds.Height,
                item.Scale,
                item.IsPrimary
            }).ToArray();
            if (monitors.Count == 0) throw new InvalidOperationException("Aucun écran détecté.");

            var monitor = monitors.FirstOrDefault(item => item.IsPrimary) ?? monitors[0];
            var width = Math.Min(320, monitor.Bounds.Width);
            var height = Math.Min(180, monitor.Bounds.Height);
            var bounds = new PixelBounds(
                monitor.Bounds.X + Math.Max(0, (monitor.Bounds.Width - width) / 2),
                monitor.Bounds.Y + Math.Max(0, (monitor.Bounds.Height - height) / 2),
                width,
                height);
            var selection = new CaptureSelection(bounds, monitor, SelectionKind.Region, "Self-test");

            using (var bitmap = ScreenCapture.Capture(bounds))
                checks["screenshot"] = new { bitmap.Width, bitmap.Height };

            var preferences = new Preferences
            {
                OutputDirectory = generatedRoot,
                IncludeSystemAudio = false,
                IncludeMicrophone = false,
                CopyScreenshotsToClipboard = false,
                ShowRecordingFrame = false,
                FramesPerSecond = 30,
                GifQuality = GifQuality.Compact
            };

            var image = await new ImageCaptureService().CaptureAsync(selection, preferences);
            checks["imageFile"] = new { bytes = new FileInfo(image.Path).Length };

            var videoProfiles = new Dictionary<string, object?>();
            foreach (var quality in Enum.GetValues<VideoQuality>())
            {
                var probe = new Preferences { VideoQualityLevel = quality };
                probe.ApplyVideoQuality();
                videoProfiles[quality.ToString()] = new { probe.FramesPerSecond, probe.VideoBitrate };
            }
            checks["captureOptions"] = new
            {
                videoProfiles,
                selectionModes = Enum.GetNames<CaptureSelectionMode>()
            };

            var pluginData = Path.Combine(generatedRoot, "plugin-state");
            var pluginManager = new PostProcessingService(pluginData, false, [new SelfTestQuickPlugin()]);
            var manifestPlugin = pluginManager.GetPlugins().Single(plugin => plugin.Id == "manifest-sidecar");

            var noMetadataPath = Path.Combine(pluginData, "no-metadata.png");
            Directory.CreateDirectory(pluginData);
            File.Copy(image.Path, noMetadataPath);
            var noMetadataArtifact = image with { Path = noMetadataPath };
            var noMetadataBytes = new FileInfo(noMetadataPath).Length;
            await pluginManager.SetSettingAsync(manifestPlugin.Id, "storage", "Aucune métadonnée");
            await pluginManager.RunAsync(noMetadataArtifact);
            var noMetadataWritten = new FileInfo(noMetadataPath).Length == noMetadataBytes
                                    && !File.Exists(noMetadataPath + ".cycapture.json")
                                    && !CaptureMetadataWriter.HasEmbeddedMetadata(noMetadataPath);

            await pluginManager.SetSettingAsync(manifestPlugin.Id, "storage", "JSON adjacent");
            await pluginManager.SetSettingAsync(manifestPlugin.Id, "includeSelection", "false");
            await pluginManager.RunAsync(image);
            using var manifestJson = JsonDocument.Parse(await File.ReadAllTextAsync(image.Path + ".cycapture.json"));
            var selectionExcluded = !manifestJson.RootElement.TryGetProperty("selection", out _);

            await pluginManager.SetSettingAsync(manifestPlugin.Id, "storage", "Intégré au média");
            await pluginManager.SetSettingAsync(manifestPlugin.Id, "includeSelection", "true");
            await pluginManager.RunAsync(image);
            var embeddedImageMetadata = CaptureMetadataWriter.HasEmbeddedMetadata(image.Path);
            using (var embeddedBitmap = new Bitmap(image.Path))
            {
                if (embeddedBitmap.Width != width || embeddedBitmap.Height != height)
                    throw new InvalidOperationException("L’image ne peut plus être relue après l’intégration des métadonnées.");
            }

            var jpegPath = Path.Combine(pluginData, "metadata-test.jpg");
            using (var sourceBitmap = new Bitmap(image.Path))
                sourceBitmap.Save(jpegPath, ImageFormat.Jpeg);
            await CaptureMetadataWriter.WriteAsync(jpegPath, "{\"selfTest\":true}", "Intégré au média", pluginData, default);
            var embeddedJpegMetadata = CaptureMetadataWriter.HasEmbeddedMetadata(jpegPath);
            using (var embeddedJpeg = new Bitmap(jpegPath))
            {
                if (embeddedJpeg.Width != width || embeddedJpeg.Height != height)
                    throw new InvalidOperationException("Le JPEG ne peut plus être relu après l’intégration des métadonnées.");
            }

            await pluginManager.SetSettingAsync(manifestPlugin.Id, "storage", "Base centrale");
            await pluginManager.RunAsync(image);
            var centralMetadata = Directory.Exists(Path.Combine(pluginData, "Metadata"))
                                  && Directory.EnumerateFiles(Path.Combine(pluginData, "Metadata"), "*.json").Any();

            var cyAnnotaPlugin = pluginManager.GetPlugins().Single(plugin => plugin.Id == "cyannota-post-edit");
            var cyAnnotaExecutable = new CyAnnotaPostProcessor().ResolveExecutable();
            await pluginManager.SetEnabledAsync(manifestPlugin.Id, false);
            var pluginDisabled = !pluginManager.GetPlugins().Single(plugin => plugin.Id == manifestPlugin.Id).Enabled;
            var quickPlugin = pluginManager.GetPlugins().Single(plugin => plugin.Id == "self-test-quick-plugin");
            await pluginManager.SetSettingAsync(quickPlugin.Id, "profile", "Quality");
            quickPlugin = pluginManager.GetPlugins().Single(plugin => plugin.Id == quickPlugin.Id);
            checks["plugins"] = new
            {
                count = pluginManager.GetPlugins().Count,
                configurable = manifestPlugin.Settings.Count > 0,
                noMetadataWritten,
                selectionExcluded,
                embeddedImageMetadata,
                embeddedJpegMetadata,
                centralMetadata,
                pluginDisabled,
                cyAnnota = new
                {
                    enabledByDefault = cyAnnotaPlugin.Enabled,
                    cyAnnotaPlugin.QuickAccessLabel,
                    executable = cyAnnotaExecutable,
                    executableFound = cyAnnotaExecutable is not null && File.Exists(cyAnnotaExecutable)
                },
                quickAccessLabel = quickPlugin.QuickAccessLabel,
                quickSetting = quickPlugin.Values["profile"],
                stateSaved = File.Exists(Path.Combine(pluginData, "plugin-settings.json"))
            };
            if (!noMetadataWritten || !selectionExcluded || !embeddedImageMetadata || !embeddedJpegMetadata
                || !centralMetadata || !pluginDisabled
                || cyAnnotaPlugin.Enabled || cyAnnotaPlugin.QuickAccessLabel != "PostEdit with CyAnnota"
                || quickPlugin.QuickAccessLabel is null || quickPlugin.Values["profile"] != "Quality")
                throw new InvalidOperationException("La configuration ou l’activation des plugins n’a pas été appliquée.");

            using var recording = new RecordingService();
            await recording.StartAsync(CaptureMode.Video, selection, preferences);
            await Task.Delay(1800);
            var video = await recording.StopAsync();
            await CaptureMetadataWriter.WriteAsync(video.Path, "{\"selfTest\":true}", "Intégré au média", pluginData, default);
            var embeddedVideoMetadata = CaptureMetadataWriter.HasEmbeddedMetadata(video.Path);
            checks["video"] = new
            {
                bytes = new FileInfo(video.Path).Length,
                durationMs = (video.FinishedAt - video.StartedAt).TotalMilliseconds,
                embeddedVideoMetadata
            };
            if (!embeddedVideoMetadata)
                throw new InvalidOperationException("Les métadonnées MP4 n’ont pas été intégrées.");

            await recording.StartAsync(CaptureMode.Gif, selection, preferences);
            await Task.Delay(1800);
            var gif = await recording.StopAsync();
            await CaptureMetadataWriter.WriteAsync(gif.Path, "{\"selfTest\":true}", "Intégré au média", pluginData, default);
            var gifInfo = InspectGif(gif.Path);
            var embeddedGifMetadata = CaptureMetadataWriter.HasEmbeddedMetadata(gif.Path);
            checks["gif"] = new
            {
                bytes = new FileInfo(gif.Path).Length,
                durationMs = (gif.FinishedAt - gif.StartedAt).TotalMilliseconds,
                gifInfo.FrameCount,
                gifInfo.EncodedDurationMs,
                embeddedGifMetadata
            };
            if (!embeddedGifMetadata || gifInfo.FrameCount == 0)
                throw new InvalidOperationException("Le GIF n’est plus valide après l’intégration des métadonnées.");

            var galleryEntries = await new HistoryService().ReadAvailableAsync(generatedRoot, false);
            checks["gallery"] = new
            {
                count = galleryEntries.Count,
                modes = galleryEntries.Select(entry => entry.Mode.ToString()).Order().ToArray()
            };
            if (galleryEntries.Count != 3)
                throw new InvalidOperationException($"La galerie devait détecter 3 captures, résultat : {galleryEntries.Count}.");
            checks["success"] = true;
        }
        catch (Exception error)
        {
            checks["success"] = false;
            checks["error"] = error.ToString();
        }
        finally
        {
            stopwatch.Stop();
            checks["elapsedMs"] = stopwatch.ElapsedMilliseconds;
            var absoluteReport = Path.GetFullPath(reportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absoluteReport)!);
            await using var stream = File.Create(absoluteReport);
            await JsonSerializer.SerializeAsync(stream, checks, new JsonSerializerOptions { WriteIndented = true });

            var expectedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "CyCapture")) + Path.DirectorySeparatorChar;
            var target = Path.GetFullPath(generatedRoot);
            if (target.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(target))
                Directory.Delete(target, true);
        }
    }

    private static (int FrameCount, int EncodedDurationMs) InspectGif(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var frames = 0;
        var duration = 0;
        for (var index = 0; index + 7 < bytes.Length; index++)
        {
            if (bytes[index] == 0x21 && bytes[index + 1] == 0xF9 && bytes[index + 2] == 0x04)
            {
                frames++;
                duration += (bytes[index + 4] | bytes[index + 5] << 8) * 10;
                index += 7;
            }
        }
        return (frames, duration);
    }

    private sealed class SelfTestQuickPlugin : ICapturePostProcessor, ICapturePluginMetadata
    {
        public string Id => "self-test-quick-plugin";
        public string Name => "Plugin rapide de test";
        public string Description => "Valide le contrat de contribution rapide.";
        public bool EnabledByDefault => true;
        public string? QuickAccessLabel => "PostEdit with SelfTest";
        public IReadOnlyList<CapturePluginSettingDefinition> Settings =>
        [
            new("profile", "Profil", CapturePluginSettingKind.Choice, "Standard", ["Fast", "Standard", "Quality"])
        ];

        public void ApplySettings(IReadOnlyDictionary<string, string> values)
        {
        }

        public ValueTask ProcessAsync(CaptureArtifact artifact, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }
}
