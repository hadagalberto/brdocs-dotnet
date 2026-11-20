using System.Diagnostics.CodeAnalysis;

namespace BrDocs;

/// <summary>
/// The Justiça Eleitoral federative-unit codes used by the Título de Eleitor (digits 9–10),
/// per the table of Resolução TSE nº 23.659/2021, art. 36.
/// </summary>
/// <remarks>
/// The codes are historical assignment codes, not IBGE codes: 01 is São Paulo, 02 Minas
/// Gerais and so on, ordered by the size of each state's electorate when the numbering was
/// created; 28 is "ZZ", the voters registered abroad.
/// </remarks>
internal static class TituloEleitorUfTable
{
    internal const int MinCode = 1;
    internal const int MaxCode = 28;

    // Index is the code minus 1.
    private static readonly string[] s_ufs =
    {
        "SP", "MG", "RJ", "RS", "BA", "PR", "CE", "PE", "SC", "GO", "MA", "PB", "PA", "ES",
        "PI", "RN", "AL", "MT", "MS", "DF", "SE", "AM", "RO", "AC", "AP", "RR", "TO", "ZZ",
    };

    /// <summary>
    /// Resolves the UF sigla for a código de UF (01–28), where 28 is "ZZ" (exterior);
    /// returns <see langword="false"/> for codes outside that range.
    /// </summary>
    internal static bool TryGetUf(int code, [NotNullWhen(true)] out string? uf)
    {
        if (code is < MinCode or > MaxCode)
        {
            uf = null;
            return false;
        }
        uf = s_ufs[code - 1];
        return true;
    }
}
