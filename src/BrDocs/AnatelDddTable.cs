namespace BrDocs;

/// <summary>
/// The 67 Códigos Nacionais (CN, the DDD codes) of the Brazilian numbering plan, per ANATEL.
/// </summary>
/// <remarks>
/// Source: ANATEL — Plano Geral de Códigos Nacionais por município (PGCN), anexo ao Despacho
/// Decisório nº 20/2026/PRRE/SPR; the numbering plan itself is approved by Resolução ANATEL
/// nº 749/2022 (Regulamento de Numeração dos Serviços de Telecomunicações), which follows
/// ITU-T recommendation E.164. Each CN identifies one Área de Numeração; unassigned two-digit
/// codes (10, 20, 23, 25, 26, 29, 30, 36, 39, 40, 50, 52, 56–59, 60, 70, 72, 76, 78, 80, 90
/// and 00–09) are not valid DDDs.
/// </remarks>
internal static class AnatelDddTable
{
    // The 67 assigned Códigos Nacionais, sorted.
    internal static readonly int[] Codes =
    {
        11, 12, 13, 14, 15, 16, 17, 18, 19,
        21, 22, 24, 27, 28,
        31, 32, 33, 34, 35, 37, 38,
        41, 42, 43, 44, 45, 46, 47, 48, 49,
        51, 53, 54, 55,
        61, 62, 63, 64, 65, 66, 67, 68, 69,
        71, 73, 74, 75, 77, 79,
        81, 82, 83, 84, 85, 86, 87, 88, 89,
        91, 92, 93, 94, 95, 96, 97, 98, 99,
    };

    internal const int Count = 67;

    private static readonly bool[] s_assigned = Build();

    /// <summary>Returns <see langword="true"/> when <paramref name="ddd"/> is one of the 67 assigned codes.</summary>
    internal static bool Contains(int ddd) => (uint)ddd < (uint)s_assigned.Length && s_assigned[ddd];

    /// <summary>Returns a random assigned Código Nacional, for <see cref="TelefoneBr.Generate(Random?)"/>.</summary>
    internal static int GetRandom(Random random) => Codes[random.Next(Count)];

    private static bool[] Build()
    {
        bool[] assigned = new bool[100];
        foreach (int code in Codes)
        {
            assigned[code] = true;
        }
        return assigned;
    }
}
