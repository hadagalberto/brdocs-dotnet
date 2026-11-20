using Xunit;

namespace BrDocs.DataAnnotations.Tests;

/// <summary>
/// The pt-BR message table is process-wide state (<see cref="ValidationMessages.SetMessage"/>), so
/// the attribute tests are serialised to keep global overrides from leaking between test classes.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DataAnnotationsTestGroup
{
    public const string Name = "DataAnnotations";
}
