namespace BrDocs;

/// <summary>
/// The FEBRABAN check-digit (DV) algorithms shared by <see cref="Boleto"/> and
/// <see cref="Arrecadacao"/>.
/// </summary>
/// <remarks>
/// Source: FEBRABAN, "Layout - Código de Barras" (módulo 10 and módulo 11 definitions) and
/// Banco Central do Brasil, Carta-Circular nº 2.926 de 25/07/2000 (cobrança barcode and
/// linha digitável layout).
/// </remarks>
internal static class FebrabanCheckDigit
{
    /// <summary>
    /// Módulo 10: multiply the digits alternately by 2 and 1 from right to left, add the decimal
    /// digits of each product, and the DAC is <c>(10 − resto) mod 10</c> — when the remainder is
    /// 0 the DAC is 0. Used by the linha digitável field DVs of both families and by the
    /// arrecadação general/block DVs when the identifier digit is 6 or 7.
    /// </summary>
    /// <param name="digits">The digits to check, in positional order.</param>
    /// <returns>The módulo 10 check digit (0–9).</returns>
    internal static int Mod10(ReadOnlySpan<char> digits)
    {
        int sum = 0;
        int weight = 2;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int product = (digits[i] - '0') * weight;
            sum += product > 9 ? product - 9 : product;
            weight = weight == 2 ? 1 : 2;
        }
        int remainder = sum % 10;
        return remainder == 0 ? 0 : 10 - remainder;
    }

    /// <summary>
    /// The bank-slip (cobrança) general DV: multiply the 43 barcode digits (everything but the
    /// DV position) by the weights 2, 3, …, 9 cycling from the right, and the DV is
    /// <c>11 − resto</c>; when the result is 0, 1, 10 or 11 the DV is 1
    /// (Carta-Circular BCB nº 2.926/2000).
    /// </summary>
    /// <param name="digits">The 43 digits covered by the general DV, in positional order.</param>
    /// <returns>The general check digit (1–9).</returns>
    internal static int BankBarcodeDv(ReadOnlySpan<char> digits)
    {
        int dv = 11 - Mod11Remainder(digits);
        return dv > 9 ? 1 : dv;
    }

    /// <summary>
    /// The arrecadação/convenio DV (used by every block DV and by the general DV when the
    /// identifier digit is 8 or 9): multiply the digits by the weights 2, 3, …, 9 cycling from
    /// the right; remainder 0 or 1 produces DV 0, remainder 10 produces DV 1, and otherwise the
    /// DV is <c>11 − resto</c> (FEBRABAN, "Layout - Código de Barras", módulo 11).
    /// </summary>
    /// <param name="digits">The digits covered by the DV, in positional order.</param>
    /// <returns>The módulo 11 check digit (0–9).</returns>
    internal static int ArrecadacaoDv(ReadOnlySpan<char> digits)
    {
        int remainder = Mod11Remainder(digits);
        return remainder <= 1 ? 0 : 11 - remainder;
    }

    private static int Mod11Remainder(ReadOnlySpan<char> digits)
    {
        int sum = 0;
        int weight = 2;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            sum += (digits[i] - '0') * weight;
            weight = weight == 9 ? 2 : weight + 1;
        }
        return sum % 11;
    }
}
