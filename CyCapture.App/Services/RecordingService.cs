using System.Windows.Media.Imaging;
using CyCapture.Models;
using ScreenRecorderLib;

namespace CyCapture.Services;

internal sealed class RecordingService : IDisposable
{
    private readonly object _sync = new();
    private Recorder? _recorder;
    private TaskCompletionSource<RecordingCompleteEventArgs>? _completion;
    private TaskCompletionSource<string>? _failure;
    private Task<CaptureArtifact>? _stopTask;
    private CaptureSelection? _selection;
    private Preferences? _preferences;
    private string? _outputPath;
    private string? _temporaryGifDirectory;

    internal bool IsActive { get; private set; }
    internal bool IsFinishing { get; private set; }
    internal CaptureMode? ActiveMode { get; private set; }
    internal DateTimeOffset StartedAt { get; private set; }

    internal event EventHandler? StateChanged;

    internal Task StartAsync(CaptureMode mode, CaptureSelection selection, Preferences preferences)
    {
        if (mode == CaptureMode.Image) throw new ArgumentException("Le mode image n’est pas un enregistrement.", nameof(mode));
        lock (_sync)
        {
            if (IsActive || IsFinishing) throw new InvalidOperationException("Un enregistrement est déjà en cours.");
            Directory.CreateDirectory(preferences.EffectiveOutputDirectory);
            _selection = selection;
            _preferences = preferences;
            ActiveMode = mode;
            StartedAt = DateTimeOffset.Now;
            _completion = new TaskCompletionSource<RecordingCompleteEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            _failure = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _stopTask = null;

            var options = BuildOptions(mode, selection, preferences);
            _recorder = Recorder.CreateRecorder(options);
            _recorder.OnRecordingComplete += RecordingComplete;
            _recorder.OnRecordingFailed += RecordingFailed;
            _recorder.OnStatusChanged += StatusChanged;

            if (mode == CaptureMode.Video)
            {
                _outputPath = Path.Combine(preferences.EffectiveOutputDirectory, FileNames.Create("video", "mp4"));
                _recorder.Record(_outputPath);
            }
            else
            {
                _temporaryGifDirectory = Path.Combine(Path.GetTempPath(), "CyCapture", "gif-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_temporaryGifDirectory);
                _outputPath = Path.Combine(preferences.EffectiveOutputDirectory, FileNames.Create("animation", "gif"));
                _recorder.Record(_temporaryGifDirectory + Path.DirectorySeparatorChar);
            }

            IsActive = true;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        return Task.CompletedTask;
    }

    internal Task<CaptureArtifact> StopAsync()
    {
        lock (_sync)
        {
            if (_stopTask is not null) return _stopTask;
            if (!IsActive || _recorder is null || _selection is null || _outputPath is null || ActiveMode is null)
                throw new InvalidOperationException("Aucun enregistrement n’est en cours.");
            IsActive = false;
            IsFinishing = true;
            StateChanged?.Invoke(this, EventArgs.Empty);
            _recorder.Stop();
            _stopTask = FinishAsync();
            return _stopTask;
        }
    }

    private async Task<CaptureArtifact> FinishAsync()
    {
        try
        {
            if (_completion is null || _failure is null || _selection is null || _outputPath is null || ActiveMode is null)
                throw new InvalidOperationException("La session d’enregistrement a été perdue.");

            var completed = await AwaitCompletionOrFailureAsync(_completion.Task, _failure.Task);
            var finishedAt = DateTimeOffset.Now;
            if (ActiveMode == CaptureMode.Gif)
            {
                await Task.Run(() => EncodeGif(completed, _outputPath, finishedAt - StartedAt));
            }
            if (!File.Exists(_outputPath) || new FileInfo(_outputPath).Length == 0)
                throw new InvalidOperationException("Le fichier d’enregistrement est vide.");
            return new CaptureArtifact(_outputPath, ActiveMode.Value, _selection, StartedAt, finishedAt);
        }
        finally
        {
            CleanupRecorder();
            CleanupTemporaryGifDirectory();
            IsFinishing = false;
            ActiveMode = null;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static async Task<RecordingCompleteEventArgs> AwaitCompletionOrFailureAsync(
        Task<RecordingCompleteEventArgs> completed,
        Task<string> failed)
    {
        var winner = await Task.WhenAny(completed, failed);
        if (winner == failed) throw new InvalidOperationException(await failed);
        return await completed;
    }

    private static RecorderOptions BuildOptions(CaptureMode mode, CaptureSelection selection, Preferences preferences)
    {
        var local = selection.LocalBounds;
        var source = new DisplayRecordingSource(selection.Monitor.DeviceName)
        {
            RecorderApi = RecorderApi.DesktopDuplication,
            IsCursorCaptureEnabled = true,
            IsBorderRequired = false,
            SourceRect = new ScreenRect(local.X, local.Y, local.Width, local.Height)
        };

        var (outputWidth, outputHeight, gifFps) = mode == CaptureMode.Gif
            ? GifDimensions(local.Width, local.Height, preferences.GifQuality)
            : (Even(local.Width), Even(local.Height), preferences.FramesPerSecond);

        return new RecorderOptions
        {
            SourceOptions = new SourceOptions { RecordingSources = [source] },
            OutputOptions = new OutputOptions
            {
                RecorderMode = mode == CaptureMode.Video ? RecorderMode.Video : RecorderMode.Slideshow,
                OutputFrameSize = new ScreenSize(outputWidth, outputHeight),
                Stretch = StretchMode.Fill
            },
            VideoEncoderOptions = new VideoEncoderOptions
            {
                Framerate = preferences.FramesPerSecond,
                Bitrate = preferences.VideoBitrate,
                IsFixedFramerate = true,
                IsHardwareEncodingEnabled = true,
                IsLowLatencyEnabled = true,
                Encoder = new H264VideoEncoder
                {
                    BitrateMode = H264BitrateControlMode.CBR,
                    EncoderProfile = H264Profile.Main
                }
            },
            AudioOptions = new AudioOptions
            {
                IsAudioEnabled = mode == CaptureMode.Video && (preferences.IncludeSystemAudio || preferences.IncludeMicrophone),
                IsOutputDeviceEnabled = mode == CaptureMode.Video && preferences.IncludeSystemAudio,
                IsInputDeviceEnabled = mode == CaptureMode.Video && preferences.IncludeMicrophone,
                InputVolume = preferences.IncludeSystemAudio && preferences.IncludeMicrophone ? 0.5f : 1f,
                OutputVolume = preferences.IncludeSystemAudio && preferences.IncludeMicrophone ? 0.5f : 1f
            },
            MouseOptions = new MouseOptions
            {
                IsMousePointerEnabled = true,
                IsMouseClicksDetected = false
            },
            SnapshotOptions = new SnapshotOptions
            {
                SnapshotFormat = ScreenRecorderLib.ImageFormat.PNG,
                SnapshotsIntervalMillis = Math.Max(20, (int)Math.Round(1000d / gifFps))
            }
        };
    }

    private static (int Width, int Height, int Fps) GifDimensions(int width, int height, GifQuality quality)
    {
        var (maxDimension, fps) = quality switch
        {
            GifQuality.Compact => (720, 8),
            GifQuality.High => (1280, 12),
            _ => (960, 10)
        };
        var scale = Math.Min(1d, maxDimension / (double)Math.Max(width, height));
        return (Even((int)Math.Round(width * scale)), Even((int)Math.Round(height * scale)), fps);
    }

    private static int Even(int value) => Math.Max(2, value - value % 2);

    private static void EncodeGif(RecordingCompleteEventArgs completed, string outputPath, TimeSpan duration)
    {
        var frames = completed.FrameInfos
            .Where(frame => !string.IsNullOrWhiteSpace(frame.Path) && File.Exists(frame.Path))
            .ToList();
        if (frames.Count == 0 && !string.IsNullOrWhiteSpace(completed.FilePath) && Directory.Exists(completed.FilePath))
        {
            frames = Directory.EnumerateFiles(completed.FilePath, "*.png")
                .OrderBy(path => NumericFileStem(path))
                .Select(path => new FrameData(path, 0))
                .ToList();
        }
        if (frames.Count == 0) throw new InvalidOperationException("Aucune image n’a été produite pour le GIF.");

        var averageDelay = Math.Max(20, (int)Math.Round(duration.TotalMilliseconds / frames.Count));
        var rawDelays = frames.Select(frame => frame.Delay > 0 ? frame.Delay : averageDelay).ToArray();
        var rawTotal = Math.Max(1, rawDelays.Sum());
        var targetCentiseconds = Math.Max(frames.Count * 2, (int)Math.Round(duration.TotalMilliseconds / 10d));
        var delays = rawDelays
            .Select(delay => (ushort)Math.Clamp(
                (int)Math.Round(delay * targetCentiseconds / (double)rawTotal),
                2,
                ushort.MaxValue))
            .ToList();
        var correction = targetCentiseconds - delays.Sum(value => (int)value);
        delays[^1] = (ushort)Math.Clamp(delays[^1] + correction, 2, ushort.MaxValue);

        var encoder = new GifBitmapEncoder();
        for (var index = 0; index < frames.Count; index++)
        {
            using var stream = File.OpenRead(frames[index].Path);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var metadata = new BitmapMetadata("gif");
            var delayCentiseconds = delays[index];
            metadata.SetQuery("/grctlext/Delay", delayCentiseconds);
            metadata.SetQuery("/grctlext/Disposal", (byte)2);
            if (index == 0)
            {
                try
                {
                    metadata.SetQuery("/appext/Application", "NETSCAPE2.0");
                    metadata.SetQuery("/appext/Data", new byte[] { 3, 1, 0, 0, 0 });
                }
                catch { }
            }
            encoder.Frames.Add(BitmapFrame.Create(decoder.Frames[0], decoder.Frames[0].Thumbnail, metadata, decoder.Frames[0].ColorContexts));
        }

        using var raw = new MemoryStream();
        encoder.Save(raw);
        File.WriteAllBytes(outputPath, GifTimingInjector.Inject(raw.ToArray(), delays));
    }

    private static int NumericFileStem(string path) =>
        int.TryParse(Path.GetFileNameWithoutExtension(path), out var value) ? value : int.MaxValue;

    private static class GifTimingInjector
    {
        private static readonly byte[] LoopExtension =
        [
            0x21, 0xFF, 0x0B,
            (byte)'N', (byte)'E', (byte)'T', (byte)'S', (byte)'C', (byte)'A', (byte)'P', (byte)'E', (byte)'2', (byte)'.', (byte)'0',
            0x03, 0x01, 0x00, 0x00, 0x00
        ];

        internal static byte[] Inject(byte[] source, IReadOnlyList<ushort> delays)
        {
            if (source.Length < 14 || source[0] != (byte)'G' || source[1] != (byte)'I' || source[2] != (byte)'F')
                throw new InvalidDataException("Le flux produit n’est pas un GIF valide.");

            using var output = new MemoryStream(source.Length + delays.Count * 8 + LoopExtension.Length);
            var position = 0;
            Copy(source, output, ref position, 13);
            var packed = source[10];
            if ((packed & 0x80) != 0)
            {
                var globalColorTableBytes = 3 * (1 << ((packed & 0x07) + 1));
                Copy(source, output, ref position, globalColorTableBytes);
            }
            output.Write(LoopExtension);

            var frameIndex = 0;
            while (position < source.Length)
            {
                var marker = source[position];
                if (marker == 0x3B)
                {
                    output.WriteByte(marker);
                    break;
                }

                if (marker == 0x21)
                {
                    if (position + 1 >= source.Length) throw new InvalidDataException("Extension GIF tronquée.");
                    var label = source[position + 1];
                    if (label == 0xF9)
                    {
                        position += 2;
                        SkipSubBlocks(source, ref position);
                    }
                    else
                    {
                        output.WriteByte(source[position++]);
                        output.WriteByte(source[position++]);
                        CopySubBlocks(source, output, ref position);
                    }
                    continue;
                }

                if (marker != 0x2C)
                    throw new InvalidDataException($"Bloc GIF inconnu à la position {position}.");

                var delay = frameIndex < delays.Count ? delays[frameIndex] : delays.LastOrDefault((ushort)10);
                output.WriteByte(0x21);
                output.WriteByte(0xF9);
                output.WriteByte(0x04);
                output.WriteByte(0x04);
                output.WriteByte((byte)(delay & 0xFF));
                output.WriteByte((byte)(delay >> 8));
                output.WriteByte(0x00);
                output.WriteByte(0x00);
                frameIndex++;

                if (position + 10 > source.Length) throw new InvalidDataException("Descripteur GIF tronqué.");
                var imagePacked = source[position + 9];
                Copy(source, output, ref position, 10);
                if ((imagePacked & 0x80) != 0)
                {
                    var localColorTableBytes = 3 * (1 << ((imagePacked & 0x07) + 1));
                    Copy(source, output, ref position, localColorTableBytes);
                }
                Copy(source, output, ref position, 1);
                CopySubBlocks(source, output, ref position);
            }

            if (frameIndex == 0) throw new InvalidDataException("Le GIF ne contient aucune image.");
            return output.ToArray();
        }

        private static void CopySubBlocks(byte[] source, Stream output, ref int position)
        {
            while (true)
            {
                if (position >= source.Length) throw new InvalidDataException("Sous-bloc GIF tronqué.");
                var length = source[position];
                output.WriteByte(source[position++]);
                if (length == 0) return;
                Copy(source, output, ref position, length);
            }
        }

        private static void SkipSubBlocks(byte[] source, ref int position)
        {
            while (true)
            {
                if (position >= source.Length) throw new InvalidDataException("Sous-bloc GIF tronqué.");
                var length = source[position++];
                if (length == 0) return;
                position += length;
                if (position > source.Length) throw new InvalidDataException("Sous-bloc GIF tronqué.");
            }
        }

        private static void Copy(byte[] source, Stream output, ref int position, int length)
        {
            if (length < 0 || position + length > source.Length) throw new InvalidDataException("Flux GIF tronqué.");
            output.Write(source, position, length);
            position += length;
        }
    }

    private void RecordingComplete(object? sender, RecordingCompleteEventArgs args) => _completion?.TrySetResult(args);
    private void RecordingFailed(object? sender, RecordingFailedEventArgs args) => _failure?.TrySetResult(args.Error);
    private void StatusChanged(object? sender, RecordingStatusEventArgs args) => StateChanged?.Invoke(this, EventArgs.Empty);

    private void CleanupRecorder()
    {
        if (_recorder is null) return;
        _recorder.OnRecordingComplete -= RecordingComplete;
        _recorder.OnRecordingFailed -= RecordingFailed;
        _recorder.OnStatusChanged -= StatusChanged;
        _recorder.Dispose();
        _recorder = null;
    }

    private void CleanupTemporaryGifDirectory()
    {
        if (string.IsNullOrWhiteSpace(_temporaryGifDirectory)) return;
        var expectedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "CyCapture")) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(_temporaryGifDirectory);
        if (target.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(target))
            Directory.Delete(target, true);
        _temporaryGifDirectory = null;
    }

    public void Dispose()
    {
        try { if (IsActive) _recorder?.Stop(); }
        catch { }
        CleanupRecorder();
    }
}
