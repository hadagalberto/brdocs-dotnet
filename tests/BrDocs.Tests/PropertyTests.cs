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
}
