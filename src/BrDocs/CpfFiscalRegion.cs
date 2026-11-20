namespace BrDocs;

/// <summary>
/// The fiscal regions of the CPF, encoded by its 9th digit per the Receita Federal table.
/// </summary>
/// <remarks>
/// The mapping (9th digit → UFs) is: 0=RS; 1=DF, GO, MS, MT, TO; 2=AC, AM, AP, PA, RO, RR;
/// 3=CE, MA, PI; 4=AL, PB, PE, RN; 5=BA, SE; 6=MG; 7=ES, RJ; 8=SP; 9=PR, SC.
/// </remarks>
public enum CpfFiscalRegion
{
    /// <summary>9th digit 0 — Rio Grande do Sul (RS).</summary>
    Rs = 0,

    /// <summary>9th digit 1 — Distrito Federal (DF), Goiás (GO), Mato Grosso do Sul (MS), Mato Grosso (MT) and Tocantins (TO).</summary>
    DfGoMsMtTo = 1,

    /// <summary>9th digit 2 — Acre (AC), Amazonas (AM), Amapá (AP), Pará (PA), Rondônia (RO) and Roraima (RR).</summary>
    AcAmApPaRoRr = 2,

    /// <summary>9th digit 3 — Ceará (CE), Maranhão (MA) and Piauí (PI).</summary>
    CeMaPi = 3,

    /// <summary>9th digit 4 — Alagoas (AL), Paraíba (PB), Pernambuco (PE) and Rio Grande do Norte (RN).</summary>
    AlPbPeRn = 4,

    /// <summary>9th digit 5 — Bahia (BA) and Sergipe (SE).</summary>
    BaSe = 5,

    /// <summary>9th digit 6 — Minas Gerais (MG).</summary>
    Mg = 6,

    /// <summary>9th digit 7 — Espírito Santo (ES) and Rio de Janeiro (RJ).</summary>
    EsRj = 7,

    /// <summary>9th digit 8 — São Paulo (SP).</summary>
    Sp = 8,

    /// <summary>9th digit 9 — Paraná (PR) and Santa Catarina (SC).</summary>
    PrSc = 9,
}

/// <summary>Extension methods for <see cref="CpfFiscalRegion"/>.</summary>
public static class CpfFiscalRegionExtensions
{
    /// <summary>
    /// Returns the UF abbreviations covered by the region, separated by commas
    /// (e.g. "DF, GO, MS, MT, TO").
    /// </summary>
    /// <param name="region">The CPF fiscal region.</param>
    /// <returns>The comma-separated UF list for the region.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="region"/> is not a defined region.</exception>
    public static string Ufs(this CpfFiscalRegion region) => region switch
    {
        CpfFiscalRegion.Rs => "RS",
        CpfFiscalRegion.DfGoMsMtTo => "DF, GO, MS, MT, TO",
        CpfFiscalRegion.AcAmApPaRoRr => "AC, AM, AP, PA, RO, RR",
        CpfFiscalRegion.CeMaPi => "CE, MA, PI",
        CpfFiscalRegion.AlPbPeRn => "AL, PB, PE, RN",
        CpfFiscalRegion.BaSe => "BA, SE",
        CpfFiscalRegion.Mg => "MG",
        CpfFiscalRegion.EsRj => "ES, RJ",
        CpfFiscalRegion.Sp => "SP",
        CpfFiscalRegion.PrSc => "PR, SC",
        _ => throw new ArgumentOutOfRangeException(nameof(region), (int)region, "Unknown CPF fiscal region."),
    };
}
