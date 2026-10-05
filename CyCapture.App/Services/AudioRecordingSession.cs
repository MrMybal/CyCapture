using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using CyCapture.Models;

namespace CyCapture.Services;

internal sealed class AudioRecordingSession : IDisposable
{
    private const int TargetSampleRate = 48_000;
    private readonly List<AudioCaptureTrack> _tracks = [];
    private readonly Stopwatch _clock = new();
    private readonly string _temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        "CyCapture",
        "audio-" + Guid.NewGuid().ToString("N"));
    private bool _started;
    private bool _stopping;

    internal void Start(bool includeSystemAudio, bool includeMicrophone)
    {
        if (!includeSystemAudio && !includeMicrophone)
            throw new InvalidOperationException("Activez le son du système, le microphone, ou les deux dans les réglages.");

        Directory.CreateDirectory(_temporaryDirectory);
        try
        {
            if (includeSystemAudio)
                _tracks.Add(new AudioCaptureTrack(
                    new WasapiLoopbackCapture(),
                    Path.Combine(_temporaryDirectory, "system.wav"),
                    _clock));
            if (includeMicrophone)
                _tracks.Add(new AudioCaptureTrack(
                    new WasapiCapture(),
                    Path.Combine(_temporaryDirectory, "microphone.wav"),
                    _clock));

            _clock.Start();
            foreach (var track in _tracks) track.Start();
            _started = true;
        }
        catch (Exception error)
        {
            Dispose();
            throw new InvalidOperationException("Impossible de démarrer la capture audio Windows.", error);
        }
    }

    internal async Task<AudioRecordingResult> StopAsync(string outputPath, AudioEncodingQuality quality)
    {
        if (!_started || _stopping)
            throw new InvalidOperationException("Aucune capture audio n’est en cours.");
        _stopping = true;
        _clock.Stop();
        var duration = _clock.Elapsed;

        try
        {
            foreach (var track in _tracks) track.RequestStop();
            await Task.WhenAll(_tracks.Select(track => track.WaitForStopAsync()));
            foreach (var track in _tracks) track.Complete(duration);
            var path = await Task.Run(() => Encode(outputPath, quality, duration));
            return new AudioRecordingResult(path, duration);
        }
        finally
        {
            foreach (var track in _tracks) track.Dispose();
            _tracks.Clear();
            CleanupTemporaryDirectory();
        }
    }

    private string Encode(string outputPath, AudioEncodingQuality quality, TimeSpan duration)
    {
        var targetChannels = quality == AudioEncodingQuality.Compact ? 1 : 2;
        var bitrate = quality switch
        {
            AudioEncodingQuality.Compact => 96_000,
            AudioEncodingQuality.High => 192_000,
            _ => 128_000
        };
        var readers = new List<AudioFileReader>();
        try
        {
            var inputs = new List<ISampleProvider>();
            foreach (var track in _tracks)
            {
                var reader = new AudioFileReader(track.Path);
                readers.Add(reader);
                inputs.Add(Normalize(reader, TargetSampleRate, targetChannels));
            }

            if (inputs.Count == 0)
                throw new InvalidOperationException("Aucune piste audio n’a été produite.");
            if (inputs.Count > 1)
            {
                inputs = inputs
                    .Select(input => (ISampleProvider)new VolumeSampleProvider(input) { Volume = 0.5f })
                    .ToList();
            }

            ISampleProvider source = inputs.Count == 1
                ? inputs[0]
                : new MixingSampleProvider(inputs) { ReadFully = false };
            source = new OffsetSampleProvider(source) { Take = duration };
            var pcmPath = Path.Combine(_temporaryDirectory, "mixed.wav");
            WaveFileWriter.CreateWaveFile16(pcmPath, source);

            try
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
                using var pcm = new WaveFileReader(pcmPath);
                MediaFoundationEncoder.EncodeToMp3(pcm, outputPath, bitrate);
                return outputPath;
            }
            catch
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
                var wavePath = Path.ChangeExtension(outputPath, ".wav");
                File.Move(pcmPath, wavePath, true);
                return wavePath;
            }
        }
        finally
        {
            foreach (var reader in readers) reader.Dispose();
        }
    }

    private static ISampleProvider Normalize(ISampleProvider source, int sampleRate, int channels)
    {
        ISampleProvider result = source;
        if (result.WaveFormat.Channels != channels)
            result = new ChannelConversionSampleProvider(result, channels);
        if (result.WaveFormat.SampleRate != sampleRate)
            result = new WdlResamplingSampleProvider(result, sampleRate);
        return result;
    }

    private void CleanupTemporaryDirectory()
    {
        try
        {
            var expectedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "CyCapture")) + Path.DirectorySeparatorChar;
            var target = Path.GetFullPath(_temporaryDirectory);
            if (target.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(target))
                Directory.Delete(target, true);
        }
        catch
        {
            // A temporary file can remain locked briefly by a Windows audio driver.
        }
    }

    public void Dispose()
    {
        foreach (var track in _tracks) track.Dispose();
        _tracks.Clear();
        CleanupTemporaryDirectory();
    }

    private sealed class AudioCaptureTrack : IDisposable
    {
        private static readonly byte[] Silence = new byte[16_384];
        private const int MissingAudioToleranceMilliseconds = 50;
        private readonly object _sync = new();
        private readonly IWaveIn _capture;
        private readonly Stopwatch _clock;
        private readonly TaskCompletionSource<Exception?> _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private WaveFileWriter? _writer;
        private long _bytesWritten;
        private TimeSpan _lastPacketTimestamp;
        private bool _started;
        private bool _acceptingData;

        internal AudioCaptureTrack(IWaveIn capture, string path, Stopwatch clock)
        {
            _capture = capture;
            _clock = clock;
            Path = path;
            _writer = new WaveFileWriter(path, capture.WaveFormat);
            _capture.DataAvailable += DataAvailable;
            _capture.RecordingStopped += RecordingStopped;
        }

        internal string Path { get; }

        internal void Start()
        {
            _acceptingData = true;
            _capture.StartRecording();
            _started = true;
        }

        internal void RequestStop()
        {
            if (!_started)
            {
                _stopped.TrySetResult(null);
                return;
            }
            try
            {
                _capture.StopRecording();
            }
            catch (Exception error)
            {
                _stopped.TrySetResult(error);
            }
        }

        internal async Task WaitForStopAsync()
        {
            var error = await _stopped.Task.WaitAsync(TimeSpan.FromSeconds(8));
            if (error is not null) throw new InvalidOperationException("Une source audio Windows s’est arrêtée avec une erreur.", error);
        }

        internal void Complete(TimeSpan duration)
        {
            lock (_sync)
            {
                if (_writer is null) return;
                FillSilenceUntil(BytesAt(duration));
                _writer.Dispose();
                _writer = null;
            }
        }

        private void DataAvailable(object? sender, WaveInEventArgs args)
        {
            lock (_sync)
            {
                if (!_acceptingData || _writer is null || args.BytesRecorded <= 0) return;
                var blockAlign = _capture.WaveFormat.BlockAlign;
                var count = args.BytesRecorded - args.BytesRecorded % blockAlign;
                if (count <= 0) return;

                var packetTimestamp = _clock.Elapsed;
                var observedInterval = packetTimestamp - _lastPacketTimestamp;
                _lastPacketTimestamp = packetTimestamp;
                var missingBytes = CalculateMissingBytes(
                    observedInterval,
                    count,
                    blockAlign,
                    _capture.WaveFormat.AverageBytesPerSecond);
                FillSilenceUntil(_bytesWritten + missingBytes);
                _writer.Write(args.Buffer, 0, count);
                _bytesWritten += count;
            }
        }

        private void RecordingStopped(object? sender, StoppedEventArgs args)
        {
            lock (_sync) _acceptingData = false;
            _stopped.TrySetResult(args.Exception);
        }

        private static long CalculateMissingBytes(
            TimeSpan observedInterval,
            int packetBytes,
            int blockAlign,
            int averageBytesPerSecond)
        {
            var intervalBytes = (long)Math.Round(observedInterval.TotalSeconds * averageBytesPerSecond);
            intervalBytes -= intervalBytes % blockAlign;
            var missingBytes = intervalBytes - packetBytes;
            var toleranceBytes = (long)Math.Ceiling(
                averageBytesPerSecond * MissingAudioToleranceMilliseconds / 1000d);
            toleranceBytes -= toleranceBytes % blockAlign;
            return missingBytes > toleranceBytes ? missingBytes - missingBytes % blockAlign : 0;
        }

        private long BytesAt(TimeSpan elapsed)
        {
            var bytes = (long)Math.Round(elapsed.TotalSeconds * _capture.WaveFormat.AverageBytesPerSecond);
            return bytes - bytes % _capture.WaveFormat.BlockAlign;
        }

        private void FillSilenceUntil(long targetBytes)
        {
            if (_writer is null || targetBytes <= _bytesWritten) return;
            var remaining = targetBytes - _bytesWritten;
            while (remaining > 0)
            {
                var count = (int)Math.Min(Silence.Length, remaining);
                count -= count % _capture.WaveFormat.BlockAlign;
                if (count <= 0) break;
                _writer.Write(Silence, 0, count);
                _bytesWritten += count;
                remaining -= count;
            }
        }

        public void Dispose()
        {
            lock (_sync) _acceptingData = false;
            _capture.DataAvailable -= DataAvailable;
            _capture.RecordingStopped -= RecordingStopped;
            try { if (_started) _capture.StopRecording(); }
            catch { }
            _capture.Dispose();
            lock (_sync)
            {
                _writer?.Dispose();
                _writer = null;
            }
        }
    }

    private sealed class ChannelConversionSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly int _sourceChannels;
        private readonly int _targetChannels;
        private float[] _sourceBuffer = [];

        internal ChannelConversionSampleProvider(ISampleProvider source, int targetChannels)
        {
            if (targetChannels is not (1 or 2)) throw new ArgumentOutOfRangeException(nameof(targetChannels));
            _source = source;
            _sourceChannels = source.WaveFormat.Channels;
            _targetChannels = targetChannels;
            WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, targetChannels);
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            var requestedFrames = count / _targetChannels;
            var sourceSamples = requestedFrames * _sourceChannels;
            if (_sourceBuffer.Length < sourceSamples) _sourceBuffer = new float[sourceSamples];
            var samplesRead = _source.Read(_sourceBuffer, 0, sourceSamples);
            var framesRead = samplesRead / _sourceChannels;

            for (var frame = 0; frame < framesRead; frame++)
            {
                var sourceOffset = frame * _sourceChannels;
                var targetOffset = offset + frame * _targetChannels;
                if (_targetChannels == 1)
                {
                    var sum = 0f;
                    for (var channel = 0; channel < _sourceChannels; channel++)
                        sum += _sourceBuffer[sourceOffset + channel];
                    buffer[targetOffset] = sum / _sourceChannels;
                }
                else if (_sourceChannels == 1)
                {
                    buffer[targetOffset] = _sourceBuffer[sourceOffset];
                    buffer[targetOffset + 1] = _sourceBuffer[sourceOffset];
                }
                else
                {
                    buffer[targetOffset] = _sourceBuffer[sourceOffset];
                    buffer[targetOffset + 1] = _sourceBuffer[sourceOffset + 1];
                }
            }
            return framesRead * _targetChannels;
        }
    }
}

internal sealed record AudioRecordingResult(string Path, TimeSpan Duration);
