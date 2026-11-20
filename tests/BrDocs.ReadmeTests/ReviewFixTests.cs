using Xunit;

namespace BrDocsReadmeTests;

/// <summary>
/// Regression tests for the documentation/project-file defects reported by the independent
/// reviewer. The expectations come from the claims made in the READMEs and from the FEBRABAN
/// arrecadação layout, not from the code under test.
/// </summary>
public sealed class ReviewFixTests
{
    [Fact]
    public void Every_Library_Opts_In_To_Aot_And_Trimming_Compatibility()
    {
        // The READMEs advertise AOT/trimming compatibility; the SDK analyzers only enforce it
        // when the libraries declare it in their project files.
        foreach (string project in Directory.GetFiles(Path.Combine(RepoRoot, "src"), "*.csproj", SearchOption.AllDirectories))
        {
            string xml = File.ReadAllText(project);
            Assert.Contains("<IsAotCompatible>true</IsAotCompatible>", xml);
            Assert.Contains("<IsTrimmable>true</IsTrimmable>", xml);
        }
    }

    [Fact]
    public void Readmes_Describe_The_Arrecadacao_Layout_Correctly()
    {
        // FEBRABAN: the arrecadação código de barras is ALWAYS 44 digits (the 48-digit form is the
        // linha digitável). Both the general DV and the four block DVs use mod 10 when the third
        // digit is 6/7 and mod 11 when it is 8/9 — not mod 10/11 x 4 plus a separate mod 11.
        string en = File.ReadAllText(Path.Combine(RepoRoot, "README.md"));
        string pt = File.ReadAllText(Path.Combine(RepoRoot, "README.pt-BR.md"));

        Assert.DoesNotContain("44/48-digit barcode", en, StringComparison.Ordinal);
        Assert.DoesNotContain("barras 44/48", pt, StringComparison.Ordinal);
        Assert.Contains("44-digit barcode", en, StringComparison.Ordinal);
        Assert.Contains("third digit", en, StringComparison.Ordinal);
        Assert.Contains("terceiro dígito", pt, StringComparison.Ordinal);
    }

    private static string RepoRoot
    {
        get
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BrDocs.sln")))
            {
                directory = directory.Parent;
            }
            Assert.NotNull(directory);
            return directory.FullName;
        }
    }
}
