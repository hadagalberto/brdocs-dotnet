using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace BrDocs.QrCode;

/// <summary>
/// An immutable ISO/IEC 18004 QR Code symbol. The encoder uses byte mode or alphanumeric mode
/// (whichever produces the smaller symbol), all versions 1–40, all four error correction levels,
/// Reed–Solomon error correction over GF(256), block interleaving and all eight mask patterns with
/// the four standard penalty rules selecting the lowest-scoring one.
/// </summary>
/// <remarks>
/// Byte-mode text is encoded as UTF-8. The <see cref="Matrix"/> is the symbol itself, without the
/// quiet zone; the quiet zone (four modules by default) is added by <see cref="ToSvg"/> and
/// <see cref="ToPng"/>, as ISO/IEC 18004 requires.
/// </remarks>
public sealed class QrCode
{
    private readonly bool[,] _matrix;
    private static readonly QrRenderOptions DefaultRenderOptions = new();

    private QrCode(bool[,] matrix, QrErrorCorrection errorCorrection, QrEncoding mode, int version, int mask)
    {
        _matrix = matrix;
        ErrorCorrection = errorCorrection;
        Mode = mode;
        Version = version;
        Mask = mask;
    }

    /// <summary>The symbol's version (1–40); the module side length is <c>4 * Version + 17</c>.</summary>
    public int Version { get; }

    /// <summary>The error correction level used for this symbol.</summary>
    public QrErrorCorrection ErrorCorrection { get; }

    /// <summary>The encoding mode chosen for the payload.</summary>
    public QrEncoding Mode { get; }

    /// <summary>The mask pattern (0–7) applied to the symbol.</summary>
    public int Mask { get; }

    /// <summary>The number of modules on one side of the symbol, excluding the quiet zone.</summary>
    public int Size => (4 * Version) + 17;

    /// <summary>The number of data codewords in the symbol.</summary>
    public int DataCodewordCount => QrTables.DataCodewords(ErrorCorrection, Version);

    /// <summary>The total number of Reed–Solomon error correction codewords in the symbol.</summary>
    public int ErrorCorrectionCodewordCount =>
        QrTables.EccPerBlockCodewords(ErrorCorrection, Version) * QrTables.BlockCount(ErrorCorrection, Version);

    /// <summary>The number of Reed–Solomon blocks the data was split into.</summary>
    public int BlockCount => QrTables.BlockCount(ErrorCorrection, Version);

    /// <summary>
    /// The module matrix, <see langword="true"/> for dark modules. Row 0 is the top row and column 0
    /// is the left column. The returned array is a copy and may be freely modified by the caller.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays",
        Justification = "The two-dimensional bool matrix is the documented output format of this type.")]
    public bool[,] Matrix => (bool[,])_matrix.Clone();

    /// <summary>
    /// Encodes <paramref name="text"/> with error correction level M, automatically choosing the
    /// smallest version, the most compact mode and the best mask.
    /// </summary>
    /// <param name="text">The text to encode; use <see cref="QrEncoderOptions"/> when a fixed version or mask is needed.</param>
    /// <param name="errorCorrection">The error correction level; M is used by callers of the one-argument overload.</param>
    /// <returns>The encoded symbol.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The text does not fit in a QR Code (version 40 is the maximum).</exception>
    public static QrCode Encode(string text, QrErrorCorrection errorCorrection = QrErrorCorrection.M) =>
        Encode(text, errorCorrection, QrEncoderOptions.Default);

    /// <summary>Encodes <paramref name="text"/> with explicit error correction and encoder options.</summary>
    /// <param name="text">The text to encode.</param>
    /// <param name="errorCorrection">The error correction level.</param>
    /// <param name="options">Version, mask and mode overrides; <see cref="QrEncoderOptions.Default"/> is all-automatic.</param>
    /// <returns>The encoded symbol.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The level, version, mask or mode is invalid.</exception>
    /// <exception cref="ArgumentException">The text does not fit in the requested version (or any version).</exception>
    public static QrCode Encode(string text, QrErrorCorrection errorCorrection, QrEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(options);
        ValidateErrorCorrection(errorCorrection);

        if (options.Version is int version && version is < 1 or > 40)
        {
            throw new ArgumentOutOfRangeException(nameof(options), version, "The version must be between 1 and 40.");
        }
        if (options.Mask is int mask && mask is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(options), mask, "The mask must be between 0 and 7.");
        }
        if (options.Encoding is QrEncoding encoding && !Enum.IsDefined(encoding))
        {
            throw new ArgumentOutOfRangeException(nameof(options), encoding, "Unknown encoding mode.");
        }

        int firstVersion = options.Version ?? 1;
        int lastVersion = options.Version ?? 40;
        bool alphanumericAllowed = QrDataEncoder.CanEncodeAlphanumeric(text.AsSpan());

        QrEncoding? bestMode = null;
        int bestVersion = int.MaxValue;
        int bestBits = int.MaxValue;
        foreach (QrEncoding mode in new[] { QrEncoding.Alphanumeric, QrEncoding.Byte })
        {
            if (options.Encoding is QrEncoding forced && forced != mode)
            {
                continue;
            }
            if (mode == QrEncoding.Alphanumeric && !alphanumericAllowed)
            {
                continue;
            }
            for (int candidate = firstVersion; candidate <= lastVersion; candidate++)
            {
                int bits = QrDataEncoder.SegmentBitLength(text, mode, candidate);
                if (bits > QrTables.DataCodewords(errorCorrection, candidate) * 8)
                {
                    continue;
                }
                if (candidate < bestVersion || (candidate == bestVersion && bits < bestBits))
                {
                    bestMode = mode;
                    bestVersion = candidate;
                    bestBits = bits;
                }
                break; // A larger version can never use fewer bits for the same mode.
            }
        }

        if (bestMode is null)
        {
            throw new ArgumentException(
                $"Text of {text.Length.ToString(CultureInfo.InvariantCulture)} characters does not fit in " +
                (options.Version is int forcedVersion
                    ? $"QR version {forcedVersion.ToString(CultureInfo.InvariantCulture)}."
                    : "the largest QR Code (version 40)."),
                nameof(text));
        }

        (bool[,] modules, int selectedMask) =
            QrMatrixBuilder.Build(text, errorCorrection, bestVersion, bestMode.Value, options.Mask);
        return new QrCode(modules, errorCorrection, bestMode.Value, bestVersion, selectedMask);
    }

    /// <summary>
    /// Renders the symbol as an SVG document. Dark modules are emitted as one single
    /// <c>&lt;path&gt;</c>, the background as one rectangle, and <c>shape-rendering="crispEdges"</c>
    /// keeps module boundaries pixel-exact at any size.
    /// </summary>
    /// <param name="options">Quiet zone, size and colours; defaults apply when <see langword="null"/>.</param>
    /// <returns>A complete, standalone SVG string.</returns>
    public string ToSvg(QrRenderOptions? options = null)
    {
        QrRenderOptions effective = options ?? DefaultRenderOptions;
        ValidateRenderOptions(effective);
        int side = Size + (2 * effective.QuietZoneModules);

        StringBuilder path = new();
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                if (_matrix[row, col])
                {
                    path.Append('M')
                        .Append((col + effective.QuietZoneModules).ToString(CultureInfo.InvariantCulture))
                        .Append(' ')
                        .Append((row + effective.QuietZoneModules).ToString(CultureInfo.InvariantCulture))
                        .Append("h1v1h-1z");
                }
            }
        }

        string sideText = side.ToString(CultureInfo.InvariantCulture);
        return new StringBuilder()
            .Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"").Append(sideText).Append("px\" height=\"")
            .Append(sideText).Append("px\" viewBox=\"0 0 ").Append(sideText).Append(' ').Append(sideText)
            .Append("\" shape-rendering=\"crispEdges\">")
            .Append("<rect width=\"").Append(sideText).Append("\" height=\"").Append(sideText)
            .Append("\" fill=\"").Append(effective.Background).Append("\"/>")
            .Append("<path fill=\"").Append(effective.Foreground).Append("\" d=\"").Append(path).Append("\"/>")
            .Append("</svg>")
            .ToString();
    }

    /// <summary>
    /// Renders the symbol as PNG bytes: an 8-bit grayscale image written entirely with the base
    /// class library (<see cref="System.IO.Compression.ZLibStream"/> and an in-box PNG chunk writer).
    /// </summary>
    /// <param name="options">Quiet zone and module size; defaults apply when <see langword="null"/>.</param>
    /// <returns>The bytes of a complete PNG file.</returns>
    public byte[] ToPng(QrRenderOptions? options = null)
    {
        QrRenderOptions effective = options ?? DefaultRenderOptions;
        ValidateRenderOptions(effective);
        return PngWriter.Write(_matrix, effective.QuietZoneModules, effective.ModulePixels);
    }

    /// <summary>The 15-bit format information value for an error correction level and mask.</summary>
    /// <param name="level">The error correction level.</param>
    /// <param name="mask">The mask pattern (0–7).</param>
    /// <returns>The bit string value, most significant bit first.</returns>
    public static int GetFormatInformationBits(QrErrorCorrection level, int mask)
    {
        ValidateErrorCorrection(level);
        if (mask is < 0 or > 7)
        {
            throw new ArgumentOutOfRangeException(nameof(mask), mask, "The mask must be between 0 and 7.");
        }
        return QrTables.FormatInformationBits(level, mask);
    }

    /// <summary>The 18-bit version information value for a version 7–40 symbol.</summary>
    /// <param name="version">The version (7–40).</param>
    /// <returns>The bit string value, most significant bit first.</returns>
    public static int GetVersionInformationBits(int version)
    {
        if (version is < 7 or > 40)
        {
            throw new ArgumentOutOfRangeException(nameof(version), version, "Version information exists only for versions 7 to 40.");
        }
        return QrTables.VersionInformationBits(version);
    }

    private static void ValidateErrorCorrection(QrErrorCorrection level)
    {
        if (!Enum.IsDefined(level))
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown error correction level.");
        }
    }

    private static void ValidateRenderOptions(QrRenderOptions options)
    {
        if (options.QuietZoneModules < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.QuietZoneModules,
                "ISO/IEC 18004 requires a quiet zone of at least four modules.");
        }
        if (options.ModulePixels < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.ModulePixels,
                "The module pixel size must be positive.");
        }
        ValidateColor(options.Foreground, nameof(options));
        ValidateColor(options.Background, nameof(options));
    }

    private static void ValidateColor(string color, string paramName)
    {
        if (color is null || color.Length is not (4 or 7) || color[0] != '#')
        {
            throw new ArgumentException("SVG colours must be '#RGB' or '#RRGGBB'.", paramName);
        }
        for (int i = 1; i < color.Length; i++)
        {
            if (!Uri.IsHexDigit(color[i]))
            {
                throw new ArgumentException("SVG colours must be '#RGB' or '#RRGGBB'.", paramName);
            }
        }
    }

}
