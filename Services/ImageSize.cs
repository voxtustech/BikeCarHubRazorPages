using System.Buffers.Binary;

namespace BikeCarHub.Services;

/// <summary>Reads the pixel size from a PNG, JPEG or WebP header, so img tags get real width/height (no layout shift).</summary>
public static class ImageSize
{
    public static (int Width, int Height)? Read(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            var buffer = new byte[Math.Min(fs.Length, 512 * 1024)];
            var read = fs.Read(buffer, 0, buffer.Length);
            return Read(buffer.AsSpan(0, read));
        }
        catch (IOException)
        {
            return null;
        }
    }

    public static (int Width, int Height)? Read(ReadOnlySpan<byte> b)
    {
        if (b.Length >= 24 && b[0] == 0x89 && b[1] == 'P' && b[2] == 'N' && b[3] == 'G')
            return (BinaryPrimitives.ReadInt32BigEndian(b[16..]), BinaryPrimitives.ReadInt32BigEndian(b[20..]));

        if (b.Length >= 30 && b[..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WEBP"u8))
        {
            var chunk = b[12..16];
            if (chunk.SequenceEqual("VP8 "u8))
                return (BinaryPrimitives.ReadUInt16LittleEndian(b[26..]) & 0x3FFF, BinaryPrimitives.ReadUInt16LittleEndian(b[28..]) & 0x3FFF);
            if (chunk.SequenceEqual("VP8L"u8))
            {
                var bits = BinaryPrimitives.ReadUInt32LittleEndian(b[21..]);
                return ((int)(bits & 0x3FFF) + 1, (int)((bits >> 14) & 0x3FFF) + 1);
            }
            if (chunk.SequenceEqual("VP8X"u8))
                return (1 + (b[24] | b[25] << 8 | b[26] << 16), 1 + (b[27] | b[28] << 8 | b[29] << 16));
            return null;
        }

        if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xD8)
        {
            var i = 2;
            while (i + 9 < b.Length)
            {
                if (b[i] != 0xFF) { i++; continue; }
                var marker = b[i + 1];
                if (marker == 0xD8 || marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7)) { i += 2; continue; }
                var length = BinaryPrimitives.ReadUInt16BigEndian(b[(i + 2)..]);
                // SOF0..SOF15 hold the frame size; C4 (DHT), C8 (JPG) and CC (DAC) are not frames.
                if (marker >= 0xC0 && marker <= 0xCF && marker != 0xC4 && marker != 0xC8 && marker != 0xCC)
                    return (BinaryPrimitives.ReadUInt16BigEndian(b[(i + 7)..]), BinaryPrimitives.ReadUInt16BigEndian(b[(i + 5)..]));
                i += 2 + length;
            }
        }
        return null;
    }
}
