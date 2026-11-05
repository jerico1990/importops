using System.IO;
using Xunit;

namespace ImportOps.Tests;

public sealed class CatalogCodePolicyContractTests
{
    private static string ServiceSource()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var file = Path.Combine(root, "src", "ImportOps.Infrastructure", "Services", "CatalogAdministrationService.cs");
        return File.ReadAllText(file);
    }

    [Fact]
    public void Internal_catalog_codes_are_generated_by_the_backend()
    {
        var source = ServiceSource();
        Assert.Contains("GenerateInternalCode(name,", source);
        Assert.Contains("case \"DOCUMENT-TYPES\"", source);
        Assert.Contains("case \"EXPENSE-CATEGORIES\"", source);
        Assert.Contains("case \"SERVICE-CATEGORIES\"", source);
        Assert.Contains("case \"PAYMENT-METHODS\"", source);
        Assert.Contains("case \"TRANSPORT-MODES\"", source);
    }

    [Fact]
    public void Catalog_codes_are_immutable_after_creation()
    {
        var source = ServiceSource();
        Assert.Contains("EnsureImmutableCode", source);
        Assert.Contains("El código no puede modificarse después de crear el registro.", source);
    }

    [Fact]
    public void Standard_catalogs_are_validated_against_known_codes()
    {
        var source = ServiceSource();
        Assert.Contains("IsoCountryCodes", source);
        Assert.Contains("IsoCurrencyCodes", source);
        Assert.Contains("IncotermCodes", source);
    }
}
