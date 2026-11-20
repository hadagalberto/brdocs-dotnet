namespace BrDocs.QrCode;

/// <summary>
/// Optional low-level controls for <see cref="QrCode.Encode(string, QrErrorCorrection, QrEncoderOptions)"/>.
/// By default the encoder chooses the smallest version and the most compact mode, and searches all
/// eight mask patterns using the four ISO penalty rules.
/// </summary>
public sealed class QrEncoderOptions
{
    /// <summary>
    /// Forces a version in the range 1–40, or <see langword="null"/> to let the encoder choose the
    /// smallest version that fits. Forcing a version that is too small throws <see cref="ArgumentException"/>.
    /// </summary>
    public int? Version { get; init; }

    /// <summary>
    /// Forces a mask pattern in the range 0–7, or <see langword="null"/> to select the mask with the
    /// lowest combined penalty score. Useful for reproducing a fixed reference symbol.
    /// </summary>
    public int? Mask { get; init; }

    /// <summary>
    /// Forces an encoding mode, or <see langword="null"/> to choose automatically between byte and
    /// alphanumeric mode (whichever produces the smaller symbol).
    /// </summary>
    public QrEncoding? Encoding { get; init; }

    /// <summary>A shared instance with all automatic defaults.</summary>
    public static QrEncoderOptions Default { get; } = new();
}
