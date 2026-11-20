using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using BrDocs.QrCode;
using Xunit;

namespace BrDocs.QrCode.Tests;

public class RenderTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly string[] PngChunkTypes = ["IHDR", "IDAT", "IEND"];

    [Fact]
    public void Svg_IsSinglePathCrispAndIncludesQuietZone()
    {
        string svg = QrCode.Encode("HELLO WORLD").ToSvg();

        Assert.StartsWith("<svg xmlns=\"http://www.w3.org/2000/svg\"", svg);
        Assert.Contains("shape-rendering=\"crispEdges\"", svg);
        Assert.Contains("viewBox=\"0 0 29 29\"", svg); // 21 modules + 2 * 4 quiet modules.
        Assert.Contains("<rect width=\"29\" height=\"29\" fill=\"#FFFFFF\"/>", svg);
        Assert.Equal(1, CountOccurrences(svg, "<path "));

        bool[,] matrix = QrCode.Encode("HELLO WORLD").Matrix;
        int darkCount = 0;
        foreach (bool module in matrix)
        {
            if (module)
            {
                darkCount++;
            }
        }
        Assert.Equal(darkCount, CountOccurrences(svg, "M"));
        Assert.EndsWith("</svg>", svg);
    }

    [Fact]
    public void Svg_HonoursRenderOptions()
    {
        QrRenderOptions options = new()
        {
            QuietZoneModules = 8,
            Foreground = "#123456",
            Background = "#FEDCBA",
        };
        string svg = QrCode.Encode("HELLO WORLD").ToSvg(options);

        Assert.Contains("viewBox=\"0 0 37 37\"", svg);
        Assert.Contains("fill=\"#123456\"", svg);
        Assert.Contains("fill=\"#FEDCBA\"", svg);
        Assert.Contains("M8 8h1v1h-1z", svg); // Top-left finder module shifted by the quiet zone.
    }

    [Fact]
    public void RenderOptions_AreValidated()
    {
        QrCode code = QrCode.Encode("HELLO WORLD");
        Assert.Throws<ArgumentOutOfRangeException>(() => code.ToSvg(new QrRenderOptions { QuietZoneModules = 3 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => code.ToPng(new QrRenderOptions { ModulePixels = 0 }));
        Assert.Throws<ArgumentException>(() => code.ToSvg(new QrRenderOptions { Foreground = "red" }));
        Assert.Throws<ArgumentException>(() => code.ToSvg(new QrRenderOptions { Background = "#GGGGGG" }));
    }

    [Fact]
    public void Png_HasValidChunksAndExpectedPixels()
    {
        byte[] png = QrCode.Encode("HELLO WORLD").ToPng();

        Assert.Equal(PngSignature, png[..8]);

        List<(string Type, byte[] Data)> chunks = ReadChunks(png);
        Assert.Equal(PngChunkTypes, chunks.Select(chunk => chunk.Type));
        Assert.Empty(chunks[2].Data);

        ReadOnlySpan<byte> header = chunks[0].Data;
        Assert.Equal(116, BinaryPrimitives.ReadInt32BigEndian(header[..4])); // (21 + 8) * 4 pixels.
        Assert.Equal(116, BinaryPrimitives.ReadInt32BigEndian(header.Slice(4, 4)));
        Assert.Equal((byte)8, header[8]);  // Bit depth.
        Assert.Equal((byte)0, header[9]);  // Grayscale.
        Assert.Equal((byte)0, header[12]); // No interlacing.

        using MemoryStream rawStream = new();
        using (ZLibStream zlib = new(new MemoryStream(chunks[1].Data), CompressionMode.Decompress))
        {
            zlib.CopyTo(rawStream);
        }
        byte[] raw = rawStream.ToArray();
        Assert.Equal(116 * (116 + 1), raw.Length);

        // First pixel row is entirely inside the quiet zone: filter byte 0 followed by white pixels.
        Assert.Equal((byte)0, raw[0]);
        Assert.All(raw.Skip(1).Take(116), pixel => Assert.Equal((byte)255, pixel));

        // The top-left module of the finder pattern is dark: 4 quiet modules * 4 px = pixel 16.
        int darkRowOffset = 16 * (116 + 1);
        Assert.Equal((byte)0, raw[darkRowOffset + 1 + 16]);
    }

    [Fact]
    public void Encoding_IsDeterministic()
    {
        QrCode first = QrCode.Encode("PIX determinism check 123", QrErrorCorrection.Q);
        QrCode second = QrCode.Encode("PIX determinism check 123", QrErrorCorrection.Q);

        Assert.Equal(ToString(first.Matrix), ToString(second.Matrix));
        Assert.Equal(first.ToSvg(), second.ToSvg());
        Assert.Equal(first.ToPng(), second.ToPng());
        Assert.Equal(first.Mask, second.Mask);
    }

    [Fact]
    public void Png_SizeFollowsModulePixels()
    {
        QrCode code = QrCode.Encode("HELLO WORLD");
        byte[] png = code.ToPng(new QrRenderOptions { QuietZoneModules = 4, ModulePixels = 2 });
        List<(string Type, byte[] Data)> chunks = ReadChunks(png);
        Assert.Equal(58, BinaryPrimitives.ReadInt32BigEndian(chunks[0].Data.AsSpan(0, 4)));
    }

    private static List<(string Type, byte[] Data)> ReadChunks(byte[] png)
    {
        List<(string, byte[])> chunks = [];
        int position = 8;
        while (position < png.Length)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(position, 4));
            string type = Encoding.ASCII.GetString(png, position + 4, 4);
            byte[] data = png.AsSpan(position + 8, length).ToArray();
            uint expectedCrc = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(position + 8 + length, 4));
            Assert.Equal(Crc32(png.AsSpan(position + 4, 4 + length)), expectedCrc);
            chunks.Add((type, data));
            position += 12 + length;
        }
        return chunks;
    }

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte value in data)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
            }
        }
        return crc ^ 0xFFFFFFFF;
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    private static string ToString(bool[,] matrix)
    {
        StringBuilder builder = new();
        foreach (bool module in matrix)
        {
            builder.Append(module ? '1' : '0');
        }
        return builder.ToString();
    }
}
