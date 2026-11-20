namespace BrDocs.QrCode;

/// <summary>
/// The ISO/IEC 18004 error correction level of a QR Code. Higher levels recover a larger fraction of
/// damaged modules at the cost of a denser symbol.
/// </summary>
public enum QrErrorCorrection
{
    /// <summary>Level L — recovers approximately 7% of codewords.</summary>
    L = 0,

    /// <summary>Level M — recovers approximately 15% of codewords. This is the default.</summary>
    M = 1,

    /// <summary>Level Q — recovers approximately 25% of codewords.</summary>
    Q = 2,

    /// <summary>Level H — recovers approximately 30% of codewords.</summary>
    H = 3,
}
