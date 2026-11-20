namespace BrDocs;

/// <summary>
/// The "identificação do valor real ou referência" digit — the third digit of an
/// arrecadação/convenio código de barras — which selects both the meaning of the 11-digit value
/// field and the module used by every check digit of the code.
/// </summary>
/// <remarks>
/// Per the FEBRABAN "Layout - Código de Barras": 6 = valor efetivo (an amount in reais) with
/// DVs computed by módulo 10; 7 = quantidade de moeda (a reference value) with módulo 10;
/// 8 = valor efetivo with módulo 11; 9 = quantidade de moeda with módulo 11.
/// The zero value is not a valid identifier and exists so that <c>default</c> maps to a named,
/// explicitly-invalid value.
/// </remarks>
public enum ArrecadacaoValueType
{
    /// <summary>Not a valid identifier (the third digit of a valid code is always 6, 7, 8 or 9).</summary>
    Unknown = 0,

    /// <summary>Valor efetivo: an amount in reais; every DV of the code uses módulo 10.</summary>
    EffectiveValueMod10 = 6,

    /// <summary>Quantidade de moeda: a reference value; every DV of the code uses módulo 10.</summary>
    ReferenceValueMod10 = 7,

    /// <summary>Valor efetivo: an amount in reais; every DV of the code uses módulo 11.</summary>
    EffectiveValueMod11 = 8,

    /// <summary>Quantidade de moeda: a reference value; every DV of the code uses módulo 11.</summary>
    ReferenceValueMod11 = 9,
}
