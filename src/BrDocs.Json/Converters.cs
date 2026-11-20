namespace BrDocs.Json;

/// <summary>Serializes a <see cref="Cpf"/> as formatted text or 11 digits.</summary>
public sealed class CpfJsonConverter : BrDocsJsonConverter<Cpf>
{
    /// <summary>Creates a converter using <see cref="BrDocsJsonOptions.Default"/>.</summary>
    public CpfJsonConverter() : this(BrDocsJsonOptions.Default.WriteFormatted)
    {
    }

    /// <summary>Creates a converter with an explicit output style.</summary>
    /// <param name="writeFormatted">Whether to write the presentation format.</param>
    public CpfJsonConverter(bool writeFormatted) : base(writeFormatted)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => "CPF";
}

/// <summary>Serializes a <see cref="Cnpj"/> as formatted text or 14 significant characters.</summary>
public sealed class CnpjJsonConverter : BrDocsJsonConverter<Cnpj>
{
    /// <summary>Creates a converter using <see cref="BrDocsJsonOptions.Default"/>.</summary>
    public CnpjJsonConverter() : this(BrDocsJsonOptions.Default.WriteFormatted)
    {
    }

    /// <summary>Creates a converter with an explicit output style.</summary>
    /// <param name="writeFormatted">Whether to write the presentation format.</param>
    public CnpjJsonConverter(bool writeFormatted) : base(writeFormatted)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => "CNPJ";
}

/// <summary>Serializes a <see cref="Cep"/> as formatted text or 8 digits.</summary>
public sealed class CepJsonConverter : BrDocsJsonConverter<Cep>
{
    /// <summary>Creates a converter using <see cref="BrDocsJsonOptions.Default"/>.</summary>
    public CepJsonConverter() : this(BrDocsJsonOptions.Default.WriteFormatted)
    {
    }

    /// <summary>Creates a converter with an explicit output style.</summary>
    /// <param name="writeFormatted">Whether to write the presentation format.</param>
    public CepJsonConverter(bool writeFormatted) : base(writeFormatted)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => "CEP";
}

/// <summary>Serializes a <see cref="Pis"/> as formatted text or 11 digits.</summary>
public sealed class PisJsonConverter : BrDocsJsonConverter<Pis>
{
    /// <summary>Creates a converter using <see cref="BrDocsJsonOptions.Default"/>.</summary>
    public PisJsonConverter() : this(BrDocsJsonOptions.Default.WriteFormatted)
    {
    }

    /// <summary>Creates a converter with an explicit output style.</summary>
    /// <param name="writeFormatted">Whether to write the presentation format.</param>
    public PisJsonConverter(bool writeFormatted) : base(writeFormatted)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => "PIS/PASEP/NIT";
}

/// <summary>Serializes a <see cref="TituloEleitor"/> as formatted text or 12 digits.</summary>
public sealed class TituloEleitorJsonConverter : BrDocsJsonConverter<TituloEleitor>
{
    /// <summary>Creates a converter using <see cref="BrDocsJsonOptions.Default"/>.</summary>
    public TituloEleitorJsonConverter() : this(BrDocsJsonOptions.Default.WriteFormatted)
    {
    }

    /// <summary>Creates a converter with an explicit output style.</summary>
    /// <param name="writeFormatted">Whether to write the presentation format.</param>
    public TituloEleitorJsonConverter(bool writeFormatted) : base(writeFormatted)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => "Título de Eleitor";
}

/// <summary>Serializes a <see cref="Cnh"/> as formatted text or 11 digits.</summary>
public sealed class CnhJsonConverter : BrDocsJsonConverter<Cnh>
{
    /// <summary>Creates a converter using <see cref="BrDocsJsonOptions.Default"/>.</summary>
    public CnhJsonConverter() : this(BrDocsJsonOptions.Default.WriteFormatted)
    {
    }

    /// <summary>Creates a converter with an explicit output style.</summary>
    /// <param name="writeFormatted">Whether to write the presentation format.</param>
    public CnhJsonConverter(bool writeFormatted) : base(writeFormatted)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => "CNH";
}

/// <summary>Serializes a <see cref="Renavam"/> as its 11 canonical digits.</summary>
public sealed class RenavamJsonConverter : BrDocsJsonConverter<Renavam>
{
    /// <summary>Creates a converter using <see cref="BrDocsJsonOptions.Default"/>.</summary>
    public RenavamJsonConverter() : this(BrDocsJsonOptions.Default.WriteFormatted)
    {
    }

    /// <summary>Creates a converter with an explicit output style.</summary>
    /// <param name="writeFormatted">Whether to write the presentation format.</param>
    public RenavamJsonConverter(bool writeFormatted) : base(writeFormatted)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => "RENAVAM";
}

/// <summary>Serializes a <see cref="Placa"/> as display text or 7 significant characters.</summary>
public sealed class PlacaJsonConverter : BrDocsJsonConverter<Placa>
{
    /// <summary>Creates a converter using <see cref="BrDocsJsonOptions.Default"/>.</summary>
    public PlacaJsonConverter() : this(BrDocsJsonOptions.Default.WriteFormatted)
    {
    }

    /// <summary>Creates a converter with an explicit output style.</summary>
    /// <param name="writeFormatted">Whether to write the presentation format.</param>
    public PlacaJsonConverter(bool writeFormatted) : base(writeFormatted)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => "placa veicular";
}

/// <summary>Serializes a <see cref="TelefoneBr"/> as display text or national digits.</summary>
public sealed class TelefoneBrJsonConverter : BrDocsJsonConverter<TelefoneBr>
{
    /// <summary>Creates a converter using <see cref="BrDocsJsonOptions.Default"/>.</summary>
    public TelefoneBrJsonConverter() : this(BrDocsJsonOptions.Default.WriteFormatted)
    {
    }

    /// <summary>Creates a converter with an explicit output style.</summary>
    /// <param name="writeFormatted">Whether to write the presentation format.</param>
    public TelefoneBrJsonConverter(bool writeFormatted) : base(writeFormatted)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => "telefone brasileiro";
}

/// <summary>Serializes a <see cref="Boleto"/> as a formatted linha digitável or 47 digits.</summary>
public sealed class BoletoJsonConverter : BrDocsJsonConverter<Boleto>
{
    /// <summary>Creates a converter using <see cref="BrDocsJsonOptions.Default"/>.</summary>
    public BoletoJsonConverter() : this(BrDocsJsonOptions.Default.WriteFormatted)
    {
    }

    /// <summary>Creates a converter with an explicit output style.</summary>
    /// <param name="writeFormatted">Whether to write the presentation format.</param>
    public BoletoJsonConverter(bool writeFormatted) : base(writeFormatted)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => "boleto";
}

/// <summary>Serializes an <see cref="Arrecadacao"/> as a formatted linha digitável or 48 digits.</summary>
public sealed class ArrecadacaoJsonConverter : BrDocsJsonConverter<Arrecadacao>
{
    /// <summary>Creates a converter using <see cref="BrDocsJsonOptions.Default"/>.</summary>
    public ArrecadacaoJsonConverter() : this(BrDocsJsonOptions.Default.WriteFormatted)
    {
    }

    /// <summary>Creates a converter with an explicit output style.</summary>
    /// <param name="writeFormatted">Whether to write the presentation format.</param>
    public ArrecadacaoJsonConverter(bool writeFormatted) : base(writeFormatted)
    {
    }

    /// <inheritdoc />
    protected override string DocumentName => "arrecadação";
}
