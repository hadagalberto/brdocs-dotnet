namespace BrDocs;

/// <summary>
/// The Correios CEP ranges per federative unit (UF), keyed by the 5-digit prefix.
/// </summary>
/// <remarks>
/// Source: Correios, "Busca Faixa de CEP" per-state search
/// (https://buscacepinter.correios.com.br/app/faixa_cep_uf_localidade/index.php).
/// Notable boundaries: SP is a single block (01000–19999) even though no city uses 10xxx;
/// MT ends at 78899, and 78900–78999 belongs to no state (legacy Porto Velho/RO CEPs may still
/// use it); AM, DF and GO are each split into two blocks; 00000–00999 is not assigned to any
/// state. A range is the block assigned to a state, not a promise that every CEP inside it is
/// in use.
/// </remarks>
internal static class CepUfTable
{
    private readonly record struct Range(int Low, int High, string Uf, int Start);

    // Sorted by Low; Start is the cumulative count of prefixes in the ranges before it.
    private static readonly (int Low, int High, string Uf)[] s_source =
    {
        (1000, 19999, "SP"), (20000, 28999, "RJ"), (29000, 29999, "ES"), (30000, 39999, "MG"),
        (40000, 48999, "BA"), (49000, 49999, "SE"), (50000, 56999, "PE"), (57000, 57999, "AL"),
        (58000, 58999, "PB"), (59000, 59999, "RN"), (60000, 63999, "CE"), (64000, 64999, "PI"),
        (65000, 65999, "MA"), (66000, 68899, "PA"), (68900, 68999, "AP"), (69000, 69299, "AM"),
        (69300, 69399, "RR"), (69400, 69899, "AM"), (69900, 69999, "AC"), (70000, 72799, "DF"),
        (72800, 72999, "GO"), (73000, 73699, "DF"), (73700, 76799, "GO"), (76800, 76999, "RO"),
        (77000, 77999, "TO"), (78000, 78899, "MT"), (79000, 79999, "MS"), (80000, 87999, "PR"),
        (88000, 89999, "SC"), (90000, 99999, "RS"),
    };

    private static readonly Range[] s_ranges = BuildRanges();

    /// <summary>The total number of prefixes assigned to a state (used by <see cref="Cep.Generate"/>).</summary>
    internal static readonly int TotalAssignedPrefixes =
        s_ranges[^1].Start + (s_ranges[^1].High - s_ranges[^1].Low + 1);

    /// <summary>Returns the UF that owns the range containing <paramref name="prefix"/>, or <see langword="null"/>.</summary>
    internal static string? LookupUf(int prefix)
    {
        // Binary search for the last range whose Low is <= prefix.
        int low = 0, high = s_ranges.Length - 1;
        while (low <= high)
        {
            int mid = (low + high) >> 1;
            if (s_ranges[mid].Low <= prefix)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }
        if (high < 0)
        {
            return null;
        }
        Range candidate = s_ranges[high];
        return prefix <= candidate.High ? candidate.Uf : null;
    }

    /// <summary>
    /// Maps a uniform index in [0, <see cref="TotalAssignedPrefixes"/>) to the low end of its range
    /// plus the cumulative start of that range, so the caller can recover the prefix as
    /// <c>low + index - start</c>.
    /// </summary>
    internal static (int Low, int Start) RangeForIndex(int index)
    {
        // Binary search for the last range whose Start is <= index.
        int low = 0, high = s_ranges.Length - 1;
        while (low <= high)
        {
            int mid = (low + high) >> 1;
            if (s_ranges[mid].Start <= index)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }
        Range range = s_ranges[high];
        return (range.Low, range.Start);
    }

    private static Range[] BuildRanges()
    {
        Range[] ranges = new Range[s_source.Length];
        int start = 0;
        for (int i = 0; i < s_source.Length; i++)
        {
            (int low, int high, string uf) = s_source[i];
            ranges[i] = new Range(low, high, uf, start);
            start += high - low + 1;
        }
        return ranges;
    }
}
