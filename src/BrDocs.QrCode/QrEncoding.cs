namespace BrDocs.QrCode;

/// <summary>The data encoding mode used for a QR Code payload.</summary>
public enum QrEncoding
{
    /// <summary>
    /// Byte mode (ISO/IEC 18004 mode indicator <c>0100</c>). Text is encoded as UTF-8 bytes; every
    /// character is supported.
    /// </summary>
    Byte = 0,

    /// <summary>
    /// Alphanumeric mode (mode indicator <c>0010</c>). Only digits 0–9, uppercase A–Z and the nine
    /// characters <c>$%*+-./:</c> plus space are allowed. Two characters pack into 11 bits, so this
    /// mode is roughly 30% more compact than byte mode whenever the input allows it.
    /// </summary>
    Alphanumeric = 1,
}
