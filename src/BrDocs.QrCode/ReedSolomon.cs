namespace BrDocs.QrCode;

/// <summary>
/// Reed–Solomon arithmetic over GF(256) as required by ISO/IEC 18004: primitive polynomial
/// <c>x^8 + x^4 + x^3 + x^2 + 1</c> (0x11D), with α = 2. Addition is XOR; multiplication is carried
/// out with the log/antilog tables. The QR Code generator polynomial has roots α^0 … α^(n-1).
/// </summary>
public static class ReedSolomon
{
    private const int PrimitivePolynomial = 0x11D;

    private static readonly byte[] Antilog = new byte[512];
    private static readonly byte[] Logarithm = new byte[256];

    static ReedSolomon()
    {
        int value = 1;
        for (int exponent = 0; exponent < 255; exponent++)
        {
            Antilog[exponent] = (byte)value;
            Logarithm[value] = (byte)exponent;
            value <<= 1;
            if ((value & 0x100) != 0)
            {
                value ^= PrimitivePolynomial;
            }
        }

        // α^255 == α^0; duplicating the table avoids a modulo on every multiplication.
        for (int exponent = 255; exponent < Antilog.Length; exponent++)
        {
            Antilog[exponent] = Antilog[exponent - 255];
        }
    }

    /// <summary>Multiplies two GF(256) elements.</summary>
    /// <param name="left">The first factor.</param>
    /// <param name="right">The second factor.</param>
    /// <returns>The product in GF(256).</returns>
    public static byte Multiply(byte left, byte right) =>
        left == 0 || right == 0 ? (byte)0 : Antilog[Logarithm[left] + Logarithm[right]];

    /// <summary>Divides one GF(256) element by another.</summary>
    /// <param name="dividend">The dividend.</param>
    /// <param name="divisor">The divisor; must not be zero.</param>
    /// <returns>The quotient in GF(256).</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="divisor"/> is zero.</exception>
    public static byte Divide(byte dividend, byte divisor)
    {
        if (divisor == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(divisor), "Division by zero is undefined in GF(256).");
        }
        return dividend == 0 ? (byte)0 : Antilog[(Logarithm[dividend] + 255 - Logarithm[divisor]) % 255];
    }

    /// <summary>
    /// Builds the QR Code generator polynomial of the given degree: the coefficients of
    /// <c>(x - α^0)(x - α^1)…(x - α^(degree-1))</c> from the highest remaining power down to the
    /// constant term, omitting the leading 1.
    /// </summary>
    /// <param name="degree">The number of error correction codewords (1–68).</param>
    /// <returns><paramref name="degree"/> coefficients.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The degree is outside 1–68.</exception>
    public static byte[] GeneratorPolynomial(int degree)
    {
        if (degree is < 1 or > 68)
        {
            throw new ArgumentOutOfRangeException(nameof(degree), degree, "The generator degree must be between 1 and 68.");
        }

        byte[] coefficients = [1]; // Descending powers; the leading coefficient is always 1.
        for (int root = 0; root < degree; root++)
        {
            byte alpha = Antilog[root];
            byte[] next = new byte[coefficients.Length + 1];
            for (int i = 0; i < coefficients.Length; i++)
            {
                // (x - α^root) == (x + α^root) in characteristic 2.
                next[i] ^= coefficients[i];
                next[i + 1] ^= Multiply(coefficients[i], alpha);
            }
            coefficients = next;
        }

        // Drop the leading 1: the divisor routine only needs the remaining coefficients.
        return coefficients[1..];
    }

    /// <summary>
    /// Computes the Reed–Solomon remainder (the error correction codewords) for one block of data.
    /// </summary>
    /// <param name="data">The data codewords of the block.</param>
    /// <param name="degree">The number of error correction codewords to produce (1–68).</param>
    /// <returns>The error correction codewords in transmission order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The degree is outside 1–68.</exception>
    public static byte[] ComputeErrorCorrectionCodewords(ReadOnlySpan<byte> data, int degree)
    {
        byte[] generator = GeneratorPolynomial(degree);
        byte[] remainder = new byte[degree];
        foreach (byte codeword in data)
        {
            byte factor = (byte)(codeword ^ remainder[0]);
            Array.Copy(remainder, 1, remainder, 0, degree - 1);
            remainder[degree - 1] = 0;
            for (int i = 0; i < degree; i++)
            {
                remainder[i] ^= Multiply(generator[i], factor);
            }
        }
        return remainder;
    }
}
