using FsCheck.Xunit;
using Xunit;

namespace BrDocs.Tests;

/// <summary>
/// Property-based tests (FsCheck). Every property compares the library against an independently
/// implemented oracle (<see cref="Oracles"/>) or checks structural invariants that must hold
/// for arbitrary inputs.
/// </summary>
public sealed class PropertyTests
{
    // ---- CPF ---------------------------------------------------------------

    [Property(MaxTest = 1_000)]
    public void Cpf_Matches_The_Oracle_For_Arbitrary_Eleven_Digit_Strings(int seed)
    {
        string value = Oracles.RandomString(seed, 11, Oracles.DigitsAlphabet);
        Assert.Equal(Oracles.IsValidCpf(value), Cpf.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Cpf_Matches_The_Oracle_For_Scattered_Input(int seed)
    {
        string core = Oracles.RandomString(seed, 11, Oracles.DigitsAlphabet);
        string value = Oracles.ScatterIgnored(seed, core);
        Assert.Equal(Oracles.IsValidCpf(core), Cpf.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Cpf_Matches_The_Oracle_For_Arbitrary_Garbage(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 40, Oracles.GarbageAlphabet);
        Assert.Equal(Oracles.IsValidCpf(value), Cpf.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Cpf_TryParse_And_IsValid_Always_Agree(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 30, Oracles.GarbageAlphabet);
        bool parsed = Cpf.TryParse(value, out Cpf cpf);
        Assert.Equal(Cpf.IsValid(value), parsed);
        if (parsed)
        {
            string digits = cpf.ToString("N");
            Assert.Equal(11, digits.Length);
            Assert.True(Cpf.IsValid(digits));
            Assert.Equal(cpf, Cpf.Parse(digits));
            Assert.Equal(cpf, Cpf.Parse(cpf.ToString()));
            Assert.Equal(digits, cpf.ToString("N"));
            Assert.Equal((CpfFiscalRegion)(digits[8] - '0'), cpf.FiscalRegion);
        }
        else
        {
            Assert.Equal(default, cpf);
        }
    }

    [Property(MaxTest = 500)]
    public void Cpf_Generate_Produces_Valid_Documents(int seed)
    {
        Cpf cpf = Cpf.Generate(new Random(seed));
        string digits = cpf.ToString("N");
        Assert.Equal(11, digits.Length);
        Assert.True(Cpf.IsValid(digits), $"generated {digits} must be valid");
        Assert.Equal(cpf, Cpf.Parse(digits));
        Assert.True(cpf.FiscalRegion is >= CpfFiscalRegion.Rs and <= CpfFiscalRegion.PrSc);
        Assert.NotEmpty(cpf.FiscalRegion.Ufs());
    }

    [Property(MaxTest = 500)]
    public void Cpf_Generate_Ignores_Punctuation_On_The_Way_Back(int seed)
    {
        Cpf original = Cpf.Generate(new Random(seed));
        string scattered = Oracles.ScatterIgnored(seed, original.ToString("N"));
        Assert.True(Cpf.TryParse(scattered, out Cpf parsed));
        Assert.Equal(original, parsed);
    }

    [Property(MaxTest = 500)]
    public void Cpf_Equality_Follows_The_Digits(int seedA, int seedB)
    {
        Cpf a = Cpf.Generate(new Random(seedA));
        Cpf b = Cpf.Generate(new Random(seedB));
        Assert.Equal(a.ToString("N") == b.ToString("N"), a == b);
        Assert.Equal(a == b, a.Equals(b));
        Assert.Equal(a == b, a.GetHashCode() == b.GetHashCode());
    }

    // ---- CNPJ --------------------------------------------------------------

    [Property(MaxTest = 1_000)]
    public void Cnpj_Matches_The_Oracle_For_Arbitrary_Numeric_Strings(int seed)
    {
        string value = Oracles.RandomString(seed, 14, Oracles.DigitsAlphabet);
        Assert.Equal(Oracles.IsValidCnpj(value), Cnpj.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Cnpj_Matches_The_Oracle_For_Arbitrary_Upper_Alphanumeric_Strings(int seed)
    {
        string value = Oracles.RandomString(seed, 14, Oracles.UpperAlphanumericAlphabet);
        Assert.Equal(Oracles.IsValidCnpj(value), Cnpj.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Cnpj_Matches_The_Oracle_For_Lowercase_Input(int seed)
    {
        string value = Oracles.RandomString(seed, 14, Oracles.LowerAlphanumericAlphabet);
        Assert.Equal(Oracles.IsValidCnpj(value), Cnpj.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Cnpj_Matches_The_Oracle_For_Scattered_Input(int seed)
    {
        string core = Oracles.RandomString(seed, 14, Oracles.UpperAlphanumericAlphabet);
        string value = Oracles.ScatterIgnored(seed, core);
        Assert.Equal(Oracles.IsValidCnpj(core), Cnpj.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Cnpj_Matches_The_Oracle_For_Arbitrary_Garbage(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 40, Oracles.GarbageAlphabet);
        Assert.Equal(Oracles.IsValidCnpj(value), Cnpj.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Cnpj_TryParse_And_IsValid_Always_Agree(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 30, Oracles.GarbageAlphabet);
        bool parsed = Cnpj.TryParse(value, out Cnpj cnpj);
        Assert.Equal(Cnpj.IsValid(value), parsed);
        if (parsed)
        {
            string digits = cnpj.ToString("N");
            Assert.Equal(14, digits.Length);
            Assert.True(Cnpj.IsValid(digits));
            Assert.Equal(cnpj, Cnpj.Parse(digits));
            Assert.Equal(cnpj, Cnpj.Parse(cnpj.ToString()));
            Assert.Equal(digits[..8], cnpj.Root);
            Assert.Equal(digits[8..12], cnpj.Branch);
            Assert.Equal(digits[12..14] == "0001", cnpj.IsHeadOffice);
            Assert.Equal(digits[..12].Any(c => c >= 'A'), cnpj.IsAlphanumeric);
        }
        else
        {
            Assert.Equal(default, cnpj);
        }
    }

    [Property(MaxTest = 500)]
    public void Cnpj_Generate_Produces_Valid_Numeric_Documents(int seed)
    {
        Cnpj cnpj = Cnpj.Generate(new Random(seed));
        string digits = cnpj.ToString("N");
        Assert.Equal(14, digits.Length);
        Assert.True(Cnpj.IsValid(digits), $"generated {digits} must be valid");
        Assert.False(cnpj.IsAlphanumeric);
        Assert.All(digits, c => Assert.True(char.IsAsciiDigit(c)));
        Assert.Equal(cnpj, Cnpj.Parse(digits));
    }

    [Property(MaxTest = 500)]
    public void Cnpj_GenerateAlphanumeric_Produces_Valid_Alphanumeric_Documents(int seed)
    {
        Cnpj cnpj = Cnpj.GenerateAlphanumeric(new Random(seed));
        string digits = cnpj.ToString("N");
        Assert.True(Cnpj.IsValid(digits), $"generated {digits} must be valid");
        Assert.True(cnpj.IsAlphanumeric, $"generated {digits} must contain a letter");
        Assert.Equal(8, cnpj.Root.Length);
        Assert.Equal(4, cnpj.Branch.Length);
        Assert.Contains(digits[..12], c => c is >= 'A' and <= 'Z');
        Assert.True(char.IsAsciiDigit(digits[12]));
        Assert.True(char.IsAsciiDigit(digits[13]));
        Assert.Equal(cnpj, Cnpj.Parse(digits));
    }

    // ---- CEP ---------------------------------------------------------------

    [Property(MaxTest = 1_000)]
    public void Cep_Matches_The_Oracle_For_Arbitrary_Eight_Digit_Strings(int seed)
    {
        string value = Oracles.RandomString(seed, 8, Oracles.DigitsAlphabet);
        Assert.Equal(Oracles.IsValidCep(value), Cep.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Cep_Matches_The_Oracle_For_Arbitrary_Garbage(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 30, Oracles.GarbageAlphabet);
        Assert.Equal(Oracles.IsValidCep(value), Cep.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Cep_TryParse_And_IsValid_Always_Agree(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 30, Oracles.GarbageAlphabet);
        bool parsed = Cep.TryParse(value, out Cep cep);
        Assert.Equal(Cep.IsValid(value), parsed);
        if (parsed)
        {
            string digits = cep.ToString("N");
            Assert.Equal(8, digits.Length);
            Assert.True(Cep.IsValid(digits));
            Assert.Equal(cep, Cep.Parse(digits));
            Assert.Equal(cep, Cep.Parse(cep.ToString()));
            // The UF, when present, must match the prefix of the digits.
            Assert.Equal(cep.Uf, Cep.Parse(digits).Uf);
        }
        else
        {
            Assert.Equal(default, cep);
        }
    }

    [Property(MaxTest = 500)]
    public void Cep_Generate_Produces_Valid_Ceps_With_A_Uf(int seed)
    {
        Cep cep = Cep.Generate(new Random(seed));
        string digits = cep.ToString("N");
        Assert.Equal(8, digits.Length);
        Assert.True(Cep.IsValid(digits), $"generated {digits} must be valid");
        Assert.NotNull(cep.Uf);
        Assert.Equal(cep, Cep.Parse(digits));
    }

    // ---- PIS ---------------------------------------------------------------

    [Property(MaxTest = 1_000)]
    public void Pis_Matches_The_Oracle_For_Arbitrary_Eleven_Digit_Strings(int seed)
    {
        string value = Oracles.RandomString(seed, 11, Oracles.DigitsAlphabet);
        Assert.Equal(Oracles.IsValidPis(value), Pis.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Pis_Matches_The_Oracle_For_Scattered_Input(int seed)
    {
        string core = Oracles.RandomString(seed, 11, Oracles.DigitsAlphabet);
        string value = Oracles.ScatterIgnored(seed, core);
        Assert.Equal(Oracles.IsValidPis(core), Pis.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Pis_Matches_The_Oracle_For_Arbitrary_Garbage(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 40, Oracles.GarbageAlphabet);
        Assert.Equal(Oracles.IsValidPis(value), Pis.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Pis_TryParse_And_IsValid_Always_Agree(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 30, Oracles.GarbageAlphabet);
        bool parsed = Pis.TryParse(value, out Pis pis);
        Assert.Equal(Pis.IsValid(value), parsed);
        if (parsed)
        {
            string digits = pis.ToString("N");
            Assert.Equal(11, digits.Length);
            Assert.True(Pis.IsValid(digits));
            Assert.Equal(pis, Pis.Parse(digits));
            Assert.Equal(pis, Pis.Parse(pis.ToString()));
        }
        else
        {
            Assert.Equal(default, pis);
        }
    }

    [Property(MaxTest = 500)]
    public void Pis_Generate_Produces_Valid_Documents(int seed)
    {
        Pis pis = Pis.Generate(new Random(seed));
        string digits = pis.ToString("N");
        Assert.Equal(11, digits.Length);
        Assert.True(Pis.IsValid(digits), $"generated {digits} must be valid");
        Assert.True(Oracles.IsValidPis(digits), $"generated {digits} must satisfy the oracle too");
        Assert.Equal(pis, Pis.Parse(digits));
    }

    [Property(MaxTest = 500)]
    public void Pis_Generate_Ignores_Punctuation_On_The_Way_Back(int seed)
    {
        Pis original = Pis.Generate(new Random(seed));
        string scattered = Oracles.ScatterIgnored(seed, original.ToString("N"));
        Assert.True(Pis.TryParse(scattered, out Pis parsed));
        Assert.Equal(original, parsed);
    }

    [Property(MaxTest = 500)]
    public void Pis_Equality_Follows_The_Digits(int seedA, int seedB)
    {
        Pis a = Pis.Generate(new Random(seedA));
        Pis b = Pis.Generate(new Random(seedB));
        Assert.Equal(a.ToString("N") == b.ToString("N"), a == b);
        Assert.Equal(a == b, a.Equals(b));
        Assert.Equal(a == b, a.GetHashCode() == b.GetHashCode());
    }

    // ---- Título de Eleitor ------------------------------------------------

    [Property(MaxTest = 1_000)]
    public void TituloEleitor_Matches_The_Oracle_For_Arbitrary_Twelve_Digit_Strings(int seed)
    {
        string value = Oracles.RandomString(seed, 12, Oracles.DigitsAlphabet);
        Assert.Equal(Oracles.IsValidTituloEleitor(value), TituloEleitor.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void TituloEleitor_Matches_The_Oracle_For_Scattered_Input(int seed)
    {
        string core = Oracles.RandomString(seed, 12, Oracles.DigitsAlphabet);
        string value = Oracles.ScatterIgnored(seed, core);
        Assert.Equal(Oracles.IsValidTituloEleitor(core), TituloEleitor.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void TituloEleitor_Matches_The_Oracle_For_Arbitrary_Garbage(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 40, Oracles.GarbageAlphabet);
        Assert.Equal(Oracles.IsValidTituloEleitor(value), TituloEleitor.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void TituloEleitor_TryParse_And_IsValid_Always_Agree(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 30, Oracles.GarbageAlphabet);
        bool parsed = TituloEleitor.TryParse(value, out TituloEleitor titulo);
        Assert.Equal(TituloEleitor.IsValid(value), parsed);
        if (parsed)
        {
            string digits = titulo.ToString("N");
            Assert.Equal(12, digits.Length);
            Assert.True(TituloEleitor.IsValid(digits));
            Assert.Equal(titulo, TituloEleitor.Parse(digits));
            Assert.Equal(titulo, TituloEleitor.Parse(titulo.ToString()));
            Assert.InRange(titulo.UfCode, 1, 28);
            Assert.NotNull(titulo.Uf);
            // The UF must match the digits 9-10 of the value.
            Assert.Equal(int.Parse(digits[8..10], System.Globalization.CultureInfo.InvariantCulture), titulo.UfCode);
        }
        else
        {
            Assert.Equal(default, titulo);
        }
    }

    [Property(MaxTest = 500)]
    public void TituloEleitor_Generate_Produces_Valid_Documents(int seed)
    {
        TituloEleitor titulo = TituloEleitor.Generate(new Random(seed));
        string digits = titulo.ToString("N");
        Assert.Equal(12, digits.Length);
        Assert.True(TituloEleitor.IsValid(digits), $"generated {digits} must be valid");
        Assert.True(Oracles.IsValidTituloEleitor(digits), $"generated {digits} must satisfy the oracle too");
        Assert.Equal(titulo, TituloEleitor.Parse(digits));
        Assert.InRange(titulo.UfCode, 1, 28);
        Assert.NotNull(titulo.Uf);
    }

    [Property(MaxTest = 500)]
    public void TituloEleitor_Equality_Follows_The_Digits(int seedA, int seedB)
    {
        TituloEleitor a = TituloEleitor.Generate(new Random(seedA));
        TituloEleitor b = TituloEleitor.Generate(new Random(seedB));
        Assert.Equal(a.ToString("N") == b.ToString("N"), a == b);
        Assert.Equal(a == b, a.Equals(b));
        Assert.Equal(a == b, a.GetHashCode() == b.GetHashCode());
    }

    // ---- CNH --------------------------------------------------------------

    [Property(MaxTest = 1_000)]
    public void Cnh_Matches_The_Oracle_For_Arbitrary_Eleven_Digit_Strings(int seed)
    {
        string value = Oracles.RandomString(seed, 11, Oracles.DigitsAlphabet);
        Assert.Equal(Oracles.IsValidCnh(value), Cnh.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Cnh_Matches_The_Oracle_For_Scattered_Input(int seed)
    {
        string core = Oracles.RandomString(seed, 11, Oracles.DigitsAlphabet);
        string value = Oracles.ScatterIgnored(seed, core);
        Assert.Equal(Oracles.IsValidCnh(core), Cnh.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Cnh_Matches_The_Oracle_For_Arbitrary_Garbage(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 40, Oracles.GarbageAlphabet);
        Assert.Equal(Oracles.IsValidCnh(value), Cnh.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Cnh_TryParse_And_IsValid_Always_Agree(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 30, Oracles.GarbageAlphabet);
        bool parsed = Cnh.TryParse(value, out Cnh cnh);
        Assert.Equal(Cnh.IsValid(value), parsed);
        if (parsed)
        {
            string digits = cnh.ToString("N");
            Assert.Equal(11, digits.Length);
            Assert.True(Cnh.IsValid(digits));
            Assert.Equal(cnh, Cnh.Parse(digits));
            Assert.Equal(cnh, Cnh.Parse(cnh.ToString()));
        }
        else
        {
            Assert.Equal(default, cnh);
        }
    }

    [Property(MaxTest = 500)]
    public void Cnh_Generate_Produces_Valid_Documents(int seed)
    {
        Cnh cnh = Cnh.Generate(new Random(seed));
        string digits = cnh.ToString("N");
        Assert.Equal(11, digits.Length);
        Assert.True(Cnh.IsValid(digits), $"generated {digits} must be valid");
        Assert.True(Oracles.IsValidCnh(digits), $"generated {digits} must satisfy the oracle too");
        Assert.Equal(cnh, Cnh.Parse(digits));
    }

    [Property(MaxTest = 500)]
    public void Cnh_Equality_Follows_The_Digits(int seedA, int seedB)
    {
        Cnh a = Cnh.Generate(new Random(seedA));
        Cnh b = Cnh.Generate(new Random(seedB));
        Assert.Equal(a.ToString("N") == b.ToString("N"), a == b);
        Assert.Equal(a == b, a.Equals(b));
        Assert.Equal(a == b, a.GetHashCode() == b.GetHashCode());
    }

    // ---- RENAVAM ----------------------------------------------------------

    [Property(MaxTest = 1_000)]
    public void Renavam_Matches_The_Oracle_For_Arbitrary_Eleven_Digit_Strings(int seed)
    {
        string value = Oracles.RandomString(seed, 11, Oracles.DigitsAlphabet);
        Assert.Equal(Oracles.IsValidRenavam(value), Renavam.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Renavam_Matches_The_Oracle_For_Arbitrary_Nine_Digit_Legacy_Strings(int seed)
    {
        string value = Oracles.RandomString(seed, 9, Oracles.DigitsAlphabet);
        Assert.Equal(Oracles.IsValidRenavam(value), Renavam.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Renavam_Matches_The_Oracle_For_Scattered_Input(int seed)
    {
        string core = Oracles.RandomString(seed, 11, Oracles.DigitsAlphabet);
        string value = Oracles.ScatterIgnored(seed, core);
        Assert.Equal(Oracles.IsValidRenavam(core), Renavam.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Renavam_Matches_The_Oracle_For_Arbitrary_Garbage(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 40, Oracles.GarbageAlphabet);
        Assert.Equal(Oracles.IsValidRenavam(value), Renavam.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Renavam_TryParse_And_IsValid_Always_Agree(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 30, Oracles.GarbageAlphabet);
        bool parsed = Renavam.TryParse(value, out Renavam renavam);
        Assert.Equal(Renavam.IsValid(value), parsed);
        if (parsed)
        {
            string digits = renavam.ToString("N");
            Assert.Equal(11, digits.Length);
            Assert.True(Renavam.IsValid(digits));
            Assert.Equal(renavam, Renavam.Parse(digits));
            Assert.Equal(renavam, Renavam.Parse(renavam.ToString()));
        }
        else
        {
            Assert.Equal(default, renavam);
        }
    }

    [Property(MaxTest = 500)]
    public void Renavam_Generate_Produces_Valid_Documents(int seed)
    {
        Renavam renavam = Renavam.Generate(new Random(seed));
        string digits = renavam.ToString("N");
        Assert.Equal(11, digits.Length);
        Assert.True(Renavam.IsValid(digits), $"generated {digits} must be valid");
        Assert.True(Oracles.IsValidRenavam(digits), $"generated {digits} must satisfy the oracle too");
        Assert.Equal(renavam, Renavam.Parse(digits));
    }

    [Property(MaxTest = 500)]
    public void Renavam_Legacy_And_Padded_Forms_Are_The_Same_Value(int seed)
    {
        // Any valid 11-digit RENAVAM can be written without its two leading zeros, when both
        // are zero, and both readings must agree.
        Renavam renavam = Renavam.Generate(new Random(seed));
        string digits = renavam.ToString("N");
        if (digits[0] == '0' && digits[1] == '0')
        {
            string legacy = digits[2..];
            Assert.True(Renavam.TryParse(legacy, out Renavam parsed));
            Assert.Equal(renavam, parsed);
            Assert.Equal(digits, parsed.ToString("N"));
        }
    }

    [Property(MaxTest = 500)]
    public void Renavam_Equality_Follows_The_Canonical_Digits(int seedA, int seedB)
    {
        Renavam a = Renavam.Generate(new Random(seedA));
        Renavam b = Renavam.Generate(new Random(seedB));
        Assert.Equal(a.ToString("N") == b.ToString("N"), a == b);
        Assert.Equal(a == b, a.Equals(b));
        Assert.Equal(a == b, a.GetHashCode() == b.GetHashCode());
    }

    // ---- Placa ------------------------------------------------------------

    [Property(MaxTest = 1_000)]
    public void Placa_Matches_The_Oracle_For_Arbitrary_Seven_Char_Strings(int seed)
    {
        string value = Oracles.RandomString(seed, 7, Oracles.UpperAlphanumericAlphabet);
        Assert.Equal(Oracles.IsValidPlaca(value), Placa.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Placa_Matches_The_Oracle_For_Lowercase_Input(int seed)
    {
        string value = Oracles.RandomString(seed, 7, Oracles.LowerAlphanumericAlphabet);
        Assert.Equal(Oracles.IsValidPlaca(value), Placa.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Placa_Matches_The_Oracle_For_Scattered_Input(int seed)
    {
        string core = Oracles.RandomString(seed, 7, Oracles.UpperAlphanumericAlphabet);
        string value = Oracles.ScatterIgnored(seed, core);
        Assert.Equal(Oracles.IsValidPlaca(core), Placa.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Placa_Matches_The_Oracle_For_Arbitrary_Garbage(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 30, Oracles.GarbageAlphabet);
        Assert.Equal(Oracles.IsValidPlaca(value), Placa.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void Placa_TryParse_And_IsValid_Always_Agree(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 20, Oracles.GarbageAlphabet);
        bool parsed = Placa.TryParse(value, out Placa placa);
        Assert.Equal(Placa.IsValid(value), parsed);
        if (parsed)
        {
            string chars = placa.ToString("N");
            Assert.Equal(7, chars.Length);
            Assert.True(Placa.IsValid(chars));
            Assert.Equal(placa, Placa.Parse(chars));
            Assert.Equal(placa, Placa.Parse(placa.ToString()));
            Assert.Equal(placa.IsMercosul, chars[4] >= 'A');
        }
        else
        {
            Assert.Equal(default, placa);
        }
    }

    [Property(MaxTest = 500)]
    public void Placa_ToMercosul_Is_Always_Valid_And_Idempotent(int seed)
    {
        Placa placa = Placa.GenerateLegacy(new Random(seed));
        Placa converted = placa.ToMercosul();
        Assert.True(converted.IsMercosul);
        Assert.True(Placa.IsValid(converted.ToString()), $"converted {converted} must be valid");
        Assert.Equal(converted, converted.ToMercosul());
        Assert.Equal(placa, Placa.Parse(placa.ToString()));
    }

    [Property(MaxTest = 500)]
    public void Placa_ToMercosul_Preserves_The_Other_Characters(int seed)
    {
        Placa placa = Placa.GenerateLegacy(new Random(seed));
        string before = placa.ToString("N");
        string after = placa.ToMercosul().ToString("N");
        Assert.Equal(before[..4], after[..4]);
        Assert.Equal(before[5..], after[5..]);
        Assert.Equal(before[4] - '0', after[4] - 'A');
    }

    [Property(MaxTest = 500)]
    public void Placa_Generate_Produces_Valid_Mercosul_Documents(int seed)
    {
        Placa placa = Placa.Generate(new Random(seed));
        string chars = placa.ToString("N");
        Assert.Equal(7, chars.Length);
        Assert.True(Placa.IsValid(chars), $"generated {chars} must be valid");
        Assert.True(Oracles.IsValidPlaca(chars), $"generated {chars} must satisfy the oracle too");
        Assert.True(placa.IsMercosul);
        Assert.Equal(placa, Placa.Parse(chars));
    }

    [Property(MaxTest = 500)]
    public void Placa_GenerateLegacy_Produces_Valid_Legacy_Documents(int seed)
    {
        Placa placa = Placa.GenerateLegacy(new Random(seed));
        string chars = placa.ToString("N");
        Assert.True(Placa.IsValid(chars), $"generated {chars} must be valid");
        Assert.True(Oracles.IsValidPlaca(chars), $"generated {chars} must satisfy the oracle too");
        Assert.True(placa.IsLegacy);
        Assert.Equal(placa, Placa.Parse(chars));
        Assert.Equal(placa, Placa.Parse(placa.ToString()));
    }

    [Property(MaxTest = 500)]
    public void Placa_Equality_Follows_The_Characters(int seedA, int seedB)
    {
        Placa a = Placa.Generate(new Random(seedA));
        Placa b = Placa.Generate(new Random(seedB));
        Assert.Equal(a.ToString("N") == b.ToString("N"), a == b);
        Assert.Equal(a == b, a.Equals(b));
        Assert.Equal(a == b, a.GetHashCode() == b.GetHashCode());
    }

    // ---- Telefone BR ------------------------------------------------------

    [Property(MaxTest = 1_000)]
    public void TelefoneBr_Matches_The_Oracle_For_Arbitrary_Eleven_Digit_Strings(int seed)
    {
        string value = Oracles.RandomString(seed, 11, Oracles.DigitsAlphabet);
        Assert.Equal(Oracles.IsValidTelefoneBr(value), TelefoneBr.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void TelefoneBr_Matches_The_Oracle_For_Scattered_Input(int seed)
    {
        string core = Oracles.RandomString(seed, 11, Oracles.DigitsAlphabet);
        string value = Oracles.ScatterIgnored(seed, core);
        Assert.Equal(Oracles.IsValidTelefoneBr(core), TelefoneBr.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void TelefoneBr_Matches_The_Oracle_For_Plus_And_Digits(int seed)
    {
        string value = Oracles.RandomString(seed, 12, Oracles.PlusDigitsAlphabet);
        Assert.Equal(Oracles.IsValidTelefoneBr(value), TelefoneBr.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void TelefoneBr_Matches_The_Oracle_For_Arbitrary_Garbage(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 40, Oracles.GarbageAlphabet);
        Assert.Equal(Oracles.IsValidTelefoneBr(value), TelefoneBr.IsValid(value));
    }

    [Property(MaxTest = 1_000)]
    public void TelefoneBr_TryParse_And_IsValid_Always_Agree(int seed)
    {
        string value = Oracles.RandomString(seed, Math.Abs(seed) % 30, Oracles.GarbageAlphabet);
        bool parsed = TelefoneBr.TryParse(value, out TelefoneBr phone);
        Assert.Equal(TelefoneBr.IsValid(value), parsed);
        if (parsed)
        {
            string national = phone.ToString("N");
            Assert.True(TelefoneBr.IsValid(national));
            Assert.Equal(phone, TelefoneBr.Parse(national));
            Assert.Equal(phone, TelefoneBr.Parse(phone.ToString()));
            Assert.Equal(phone, TelefoneBr.Parse(phone.ToE164()));
            Assert.Contains(phone.Ddd, Oracles.ValidDdds);
        }
        else
        {
            Assert.Equal(default, phone);
        }
    }

    [Property(MaxTest = 500)]
    public void TelefoneBr_Generate_Produces_Valid_Mobiles(int seed)
    {
        TelefoneBr phone = TelefoneBr.Generate(new Random(seed));
        string national = phone.ToString("N");
        Assert.True(TelefoneBr.IsValid(national), $"generated {national} must be valid");
        Assert.True(Oracles.IsValidTelefoneBr(national), $"generated {national} must satisfy the oracle too");
        Assert.True(phone.IsMobile);
        Assert.Contains(phone.Ddd, Oracles.ValidDdds);
        Assert.Equal(phone, TelefoneBr.Parse(phone.ToE164()));
    }

    [Property(MaxTest = 500)]
    public void TelefoneBr_GenerateFixed_Produces_Valid_Fixed_Lines(int seed)
    {
        TelefoneBr phone = TelefoneBr.GenerateFixed(new Random(seed));
        string national = phone.ToString("N");
        Assert.True(TelefoneBr.IsValid(national), $"generated {national} must be valid");
        Assert.True(Oracles.IsValidTelefoneBr(national), $"generated {national} must satisfy the oracle too");
        Assert.True(phone.IsFixed);
        Assert.Contains(phone.Ddd, Oracles.ValidDdds);
        Assert.Equal(phone, TelefoneBr.Parse(phone.ToE164()));
    }

    [Property(MaxTest = 500)]
    public void TelefoneBr_Equality_Follows_The_National_Digits(int seedA, int seedB)
    {
        TelefoneBr a = TelefoneBr.Generate(new Random(seedA));
        TelefoneBr b = TelefoneBr.Generate(new Random(seedB));
        Assert.Equal(a.ToString("N") == b.ToString("N"), a == b);
        Assert.Equal(a == b, a.Equals(b));
        Assert.Equal(a == b, a.GetHashCode() == b.GetHashCode());
    }
}
