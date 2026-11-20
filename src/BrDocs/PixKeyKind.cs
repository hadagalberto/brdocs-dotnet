namespace BrDocs;

/// <summary>
/// The type of a Pix key (a DICT key) as it is codified in a BR Code —
/// Banco Central do Brasil, "Manual de Padrões para Iniciação do Pix", section 2.5.1.
/// </summary>
public enum PixKeyKind
{
    /// <summary>A CPF key: 11 digits, e.g. <c>12345678900</c>.</summary>
    Cpf = 1,

    /// <summary>
    /// A CNPJ key: 14 alphanumeric characters, either numeric (<c>00038166000105</c>) or in the
    /// alphanumeric format of IN RFB nº 2.229/2024 (<c>12ABC34501DE35</c>).
    /// </summary>
    Cnpj = 2,

    /// <summary>An e-mail key, e.g. <c>fulano_da_silva.recebedor@example.com</c>.</summary>
    Email = 3,

    /// <summary>
    /// A mobile phone key in international form: "+55" + DDD + 9-digit number, e.g.
    /// <c>+5561912345678</c>.
    /// </summary>
    Phone = 4,

    /// <summary>
    /// A chave aleatória (EVP): a UUID in the canonical 8-4-4-4-12 hyphenated form, e.g.
    /// <c>123e4567-e12b-12d1-a456-426655440000</c>.
    /// </summary>
    Evp = 5,
}
