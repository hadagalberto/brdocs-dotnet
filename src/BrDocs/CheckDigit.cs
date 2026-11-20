namespace BrDocs;

/// <summary>
/// Shared mod-11 check-digit (DV) helpers used by the BrDocs document types.
/// </summary>
internal static class CheckDigit
{
    /// <summary>
    /// Computes Σ (charValue[i] × weight[i]) where each char value is <c>(int)c - 48</c>
    /// (so '0'..'9' contribute 0..9 and 'A'..'Z' contribute 17..42, as required by the
    /// alphanumeric CNPJ of IN RFB nº 2.229/2024).
    /// </summary>
    /// <param name="chars">The digits/letters to weigh; must have the same length as <paramref name="weights"/>.</param>
    /// <param name="weights">The weights, in the official order for the document being validated.</param>
    /// <returns>The weighted sum.</returns>
    internal static int WeightedSum(ReadOnlySpan<char> chars, ReadOnlySpan<int> weights)
    {
        int sum = 0;
        for (int i = 0; i < chars.Length; i++)
        {
            sum += (chars[i] - 48) * weights[i];
        }
        return sum;
    }

    /// <summary>
    /// Applies the standard mod-11 DV rule used by CPF and CNPJ: remainder &lt; 2 produces digit 0,
    /// otherwise the digit is 11 − remainder.
    /// </summary>
    /// <param name="weightedSum">The weighted sum of the base characters.</param>
    /// <returns>The check digit (0–9).</returns>
    internal static int Mod11CheckDigit(int weightedSum)
    {
        int remainder = weightedSum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
