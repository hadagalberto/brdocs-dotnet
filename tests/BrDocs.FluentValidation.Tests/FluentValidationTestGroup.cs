using Xunit;

namespace BrDocs.FluentValidation.Tests;

/// <summary>The pt-BR message table is process-wide state, so message overrides are serialised.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class FluentValidationTestGroup
{
    public const string Name = "FluentValidation";
}
