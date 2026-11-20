namespace BrDocs.QrCode;

/// <summary>
/// Renders a Pix BR Code payload as a QR Code. Lives in <c>BrDocs.QrCode</c> so the core
/// <c>BrDocs</c> package keeps its zero-dependency, QR-free design.
/// </summary>
public static class PixPayloadQrExtensions
{
    /// <summary>
    /// Encodes the BR Code text into a QR Code and renders it as a standalone SVG document with the
    /// default settings (error correction M, four-module quiet zone).
    /// </summary>
    /// <param name="payload">The Pix payload.</param>
    /// <returns>The SVG string, ready to embed or write to disk.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/>.</exception>
    public static string ToQrSvg(this PixPayload payload) => ToQrSvg(payload, QrErrorCorrection.M, null);

    /// <summary>Encodes and renders the payload with a chosen error correction level.</summary>
    /// <param name="payload">The Pix payload.</param>
    /// <param name="errorCorrection">The QR error correction level.</param>
    /// <returns>The SVG string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/>.</exception>
    public static string ToQrSvg(this PixPayload payload, QrErrorCorrection errorCorrection) =>
        ToQrSvg(payload, errorCorrection, null);

    /// <summary>Encodes and renders the payload with chosen error correction and rendering options.</summary>
    /// <param name="payload">The Pix payload.</param>
    /// <param name="errorCorrection">The QR error correction level.</param>
    /// <param name="renderOptions">Quiet zone, size and colours; defaults apply when <see langword="null"/>.</param>
    /// <returns>The SVG string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/>.</exception>
    public static string ToQrSvg(this PixPayload payload, QrErrorCorrection errorCorrection, QrRenderOptions? renderOptions)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return payload.ToQrCode(errorCorrection).ToSvg(renderOptions);
    }

    /// <summary>Encodes the payload's BR Code text as a <see cref="QrCode"/> symbol.</summary>
    /// <param name="payload">The Pix payload.</param>
    /// <param name="errorCorrection">The QR error correction level.</param>
    /// <returns>The encoded symbol.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/>.</exception>
    public static QrCode ToQrCode(this PixPayload payload, QrErrorCorrection errorCorrection = QrErrorCorrection.M)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return QrCode.Encode(payload.ToString(), errorCorrection);
    }
}
