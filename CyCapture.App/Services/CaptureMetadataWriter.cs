using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace CyCapture.Services;

internal static class CaptureMetadataWriter
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly byte[] Mp4MetadataUuid =
    [
        0x43, 0x79, 0x43, 0x61, 0x70, 0x74, 0x75, 0x72,
        0x65, 0x4D, 0x65, 0x74, 0x61, 0x30, 0x30, 0x31
    ];

    internal static async Task WriteAsync(
        string mediaPath,
        string json,
        string storage,
        string dataDirectory,
        CancellationToken cancellationToken)
    {
        if (storage.StartsWith("Aucune", StringComparison.OrdinalIgnoreCase)) return;
        if (storage.StartsWith("JSON", StringComparison.OrdinalIgnoreCase))
        {
            await File.WriteAllTextAsync(mediaPath + ".cycapture.json", json, cancellationToken);
            return;
        }
        if (storage.StartsWith("Base centrale", StringComparison.OrdinalIgnoreCase)
            || storage.StartsWith("Central", StringComparison.OrdinalIgnoreCase))
        {
            await WriteCentralAsync(mediaPath, json, dataDirectory, cancellationToken);
            return;
        }

        var source = await File.ReadAllBytesAsync(mediaPath, cancellationToken);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var extension = Path.GetExtension(mediaPath).ToLowerInvariant();
        var embedded = extension switch
        {
            ".png" => EmbedPng(source, jsonBytes),
            ".jpg" or ".jpeg" => EmbedJpeg(source, jsonBytes),
            ".gif" => EmbedGif(source, jsonBytes),
            ".mp4" or ".mov" or ".m4v" => EmbedMp4(source, jsonBytes),
            _ => null
        };

        if (embedded is null)
        {
            await WriteCentralAsync(mediaPath, json, dataDirectory, cancellationToken);
            return;
        }
        await ReplaceMediaAsync(mediaPath, embedded, cancellationToken);
    }

    internal static bool HasEmbeddedMetadata(string mediaPath)
    {
        if (!File.Exists(mediaPath)) return false;
        var bytes = File.ReadAllBytes(mediaPath);
        return Contains(bytes, Encoding.ASCII.GetBytes("CyCapture"))
               || Contains(bytes, Encoding.ASCII.GetBytes("cycapture:manifest"))
               || Contains(bytes, Mp4MetadataUuid);
    }

    private static async Task WriteCentralAsync(
        string mediaPath,
        string json,
        string dataDirectory,
        CancellationToken cancellationToken)
    {
        var directory = Path.Combine(dataDirectory, "Metadata");
        Directory.CreateDirectory(directory);
        var normalizedPath = Path.GetFullPath(mediaPath).ToUpperInvariant();
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedPath)))[..16];
        var name = $"{Path.GetFileName(mediaPath)}.{key}.json";
        await File.WriteAllTextAsync(Path.Combine(directory, name), json, cancellationToken);
    }

    private static async Task ReplaceMediaAsync(string mediaPath, byte[] content, CancellationToken cancellationToken)
    {
        var createdAt = File.GetCreationTimeUtc(mediaPath);
        var modifiedAt = File.GetLastWriteTimeUtc(mediaPath);
        var temporaryPath = mediaPath + ".cycapture-embed-" + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporaryPath, content, cancellationToken);
            File.Move(temporaryPath, mediaPath, true);
            File.SetCreationTimeUtc(mediaPath, createdAt);
            File.SetLastWriteTimeUtc(mediaPath, modifiedAt);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static byte[]? EmbedPng(byte[] source, byte[] json)
    {
        if (source.Length < PngSignature.Length || !source.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
            return null;

        var insertion = -1;
        var position = PngSignature.Length;
        while (position + 12 <= source.Length)
        {
            var length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(source.AsSpan(position, 4)));
            if (length < 0 || position + 12L + length > source.Length) return null;
            if (source.AsSpan(position + 4, 4).SequenceEqual("IEND"u8))
            {
                insertion = position;
                break;
            }
            position += 12 + length;
        }
        if (insertion < 0) return null;

        var keyword = Encoding.ASCII.GetBytes("CyCapture");
        var data = new byte[keyword.Length + 5 + json.Length];
        keyword.CopyTo(data, 0);
        var jsonOffset = keyword.Length + 5;
        json.CopyTo(data, jsonOffset);
        var chunk = BuildPngChunk("iTXt"u8, data);
        return Insert(source, insertion, chunk);
    }

    private static byte[]? EmbedJpeg(byte[] source, byte[] json)
    {
        if (source.Length < 2 || source[0] != 0xFF || source[1] != 0xD8) return null;
        var xmpHeader = Encoding.ASCII.GetBytes("http://ns.adobe.com/xap/1.0/\0");
        var encoded = Convert.ToBase64String(json);
        var xmp = Encoding.UTF8.GetBytes(
            "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description xmlns:cycapture=\"https://cycapture.local/ns/1.0/\" cycapture:manifest=\"" +
            encoded + "\"/></rdf:RDF></x:xmpmeta>");
        var payloadLength = xmpHeader.Length + xmp.Length;
        if (payloadLength + 2 > ushort.MaxValue) return null;
        var segment = new byte[payloadLength + 4];
        segment[0] = 0xFF;
        segment[1] = 0xE1;
        BinaryPrimitives.WriteUInt16BigEndian(segment.AsSpan(2, 2), (ushort)(payloadLength + 2));
        xmpHeader.CopyTo(segment, 4);
        xmp.CopyTo(segment, 4 + xmpHeader.Length);
        return Insert(source, 2, segment);
    }

    private static byte[]? EmbedGif(byte[] source, byte[] json)
    {
        if (source.Length < 7 || !source.AsSpan(0, 3).SequenceEqual("GIF"u8)) return null;
        var trailer = Array.LastIndexOf(source, (byte)0x3B);
        if (trailer < 0) return null;
        var payload = Encoding.UTF8.GetBytes("CyCapture\0" + Encoding.UTF8.GetString(json));
        using var extension = new MemoryStream(payload.Length + payload.Length / 255 + 4);
        extension.WriteByte(0x21);
        extension.WriteByte(0xFE);
        var offset = 0;
        while (offset < payload.Length)
        {
            var length = Math.Min(255, payload.Length - offset);
            extension.WriteByte((byte)length);
            extension.Write(payload, offset, length);
            offset += length;
        }
        extension.WriteByte(0);
        return Insert(source, trailer, extension.ToArray());
    }

    private static byte[]? EmbedMp4(byte[] source, byte[] json)
    {
        if (source.Length < 12 || !source.AsSpan(4, 4).SequenceEqual("ftyp"u8)) return null;
        var size = 8L + Mp4MetadataUuid.Length + json.Length;
        if (size > uint.MaxValue) return null;
        var box = new byte[size];
        BinaryPrimitives.WriteUInt32BigEndian(box.AsSpan(0, 4), (uint)size);
        "uuid"u8.CopyTo(box.AsSpan(4, 4));
        Mp4MetadataUuid.CopyTo(box, 8);
        json.CopyTo(box, 8 + Mp4MetadataUuid.Length);
        return Insert(source, source.Length, box);
    }

    private static byte[] BuildPngChunk(ReadOnlySpan<byte> type, byte[] data)
    {
        var chunk = new byte[data.Length + 12];
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(0, 4), (uint)data.Length);
        type.CopyTo(chunk.AsSpan(4, 4));
        data.CopyTo(chunk, 8);
        var crc = Crc32(chunk.AsSpan(4, data.Length + 4));
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(data.Length + 8, 4), crc);
        return chunk;
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc >> 1) ^ (0xEDB88320u & (uint)-(int)(crc & 1));
        }
        return ~crc;
    }

    private static byte[] Insert(byte[] source, int offset, byte[] addition)
    {
        var result = new byte[source.Length + addition.Length];
        Buffer.BlockCopy(source, 0, result, 0, offset);
        Buffer.BlockCopy(addition, 0, result, offset, addition.Length);
        Buffer.BlockCopy(source, offset, result, offset + addition.Length, source.Length - offset);
        return result;
    }

    private static bool Contains(byte[] source, byte[] value)
    {
        if (value.Length == 0 || value.Length > source.Length) return false;
        for (var index = 0; index <= source.Length - value.Length; index++)
        {
            if (source.AsSpan(index, value.Length).SequenceEqual(value)) return true;
        }
        return false;
    }
}
