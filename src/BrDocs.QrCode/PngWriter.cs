using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace BrDocs.QrCode;

/// <summary>
/// A minimal PNG encoder: 8-bit grayscale, no interlacing, filter type 0, with zlib compression from
/// <see cref="ZLibStream"/> (part of the .NET base class library). No third-party dependencies.
/// </summary>
internal static class PngWriter
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static byte[] Write(bool[,] modules, int quietZone, int modulePixels)
    {
        int moduleSize = modules.GetLength(0);
        int pixelSize = (moduleSize + (2 * quietZone)) * modulePixels;
        byte[] raw = new byte[pixelSize * (pixelSize + 1)]; // One filter byte plus one byte per pixel per row.

        int position = 0;
        for (int moduleRow = 0; moduleRow < moduleSize + (2 * quietZone); moduleRow++)
        {
            byte[] scanline = new byte[pixelSize];
            for (int moduleCol = 0; moduleCol < moduleSize + (2 * quietZone); moduleCol++)
            {
                bool dark = moduleRow >= quietZone && moduleRow < quietZone + moduleSize
                    && moduleCol >= quietZone && moduleCol < quietZone + moduleSize
                    && modules[moduleRow - quietZone, moduleCol - quietZone];
                byte value = dark ? (byte)0 : (byte)255;
                for (int px = 0; px < modulePixels; px++)
                {
                    scanline[(moduleCol * modulePixels) + px] = value;
                }
            }
            for (int py = 0; py < modulePixels; py++)
            {
                raw[position++] = 0; // Filter type: None.
                scanline.CopyTo(raw, position);
                position += pixelSize;
            }
        }

        using MemoryStream png = new();
        png.Write(Signature);

        byte[] header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), pixelSize);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), pixelSize);
        header[8] = 8;  // Bit depth.
        header[9] = 0;  // Colour type: grayscale.
        header[10] = 0; // Compression method: deflate.
        header[11] = 0; // Filter method: adaptive.
        header[12] = 0; // Interlace method: none.
        WriteChunk(png, "IHDR", header);

        using (MemoryStream compressed = new())
        {
            using (ZLibStream zlib = new(compressed, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(raw);
            }
            WriteChunk(png, "IDAT", compressed.ToArray());
        }

        WriteChunk(png, "IEND", ReadOnlySpan<byte>.Empty);
        return png.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
    {
        byte[] length = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);

        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        uint crc = 0xFFFFFFFF;
        foreach (byte value in typeBytes)
        {
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }
        foreach (byte value in data)
        {
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }
        crc ^= 0xFFFFFFFF;

        BinaryPrimitives.WriteUInt32BigEndian(length, crc);
        stream.Write(length);
    }

    private static uint[] BuildCrcTable()
    {
        uint[] table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            uint value = i;
            for (int bit = 0; bit < 8; bit++)
            {
                value = (value & 1) != 0 ? 0xEDB88320 ^ (value >> 1) : value >> 1;
            }
            table[i] = value;
        }
        return table;
    }
}
