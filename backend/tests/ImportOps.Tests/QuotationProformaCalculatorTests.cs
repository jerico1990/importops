using ImportOps.Application.Quotations;
using Xunit;

namespace ImportOps.Tests;

public sealed class QuotationProformaCalculatorTests
{
    [Fact]
    public void Real_proforma_customs_duties_match_reference_case()
    {
        var customsValue = QuotationProformaCalculator.CustomsValue(51920.780m, 4270m, 238.830m);
        var adValorem = 0m;
        var importIgv = QuotationProformaCalculator.CustomsValueTax(customsValue, 16m);
        var ipm = QuotationProformaCalculator.CustomsValueTax(customsValue, 2m);
        var perceptionBase = QuotationProformaCalculator.PerceptionBase(customsValue, adValorem, importIgv, ipm);
        var perception = QuotationProformaCalculator.Perception(customsValue, adValorem, importIgv, ipm, 3.5m);

        Assert.Equal(56429.610m, customsValue);
        Assert.Equal(9029m, importIgv);
        Assert.Equal(1129m, ipm);
        Assert.Equal(66587.610m, perceptionBase);
        Assert.Equal(2331m, perception);
    }

    [Fact]
    public void Real_proforma_display_rounded_values_reproduce_document_total()
    {
        var duties = QuotationProformaCalculator.Sum(0m, 9029m, 1129m, 2331m);
        var total = QuotationProformaCalculator.Total(duties, 6381m, 379.98m);
        Assert.Equal(12489m, duties);
        Assert.Equal(19249.98m, total);
    }

    [Fact]
    public void Ad_valorem_is_included_in_perception_base()
    {
        Assert.Equal(1170m, QuotationProformaCalculator.PerceptionBase(1000m, 50m, 100m, 20m));
        Assert.Equal(41m, QuotationProformaCalculator.Perception(1000m, 50m, 100m, 20m, 3.5m));
    }

    [Fact]
    public void Tax_is_zero_for_non_taxable_concept() => Assert.Equal(0m, QuotationProformaCalculator.LineTax(1960m, 18m, false));

    [Fact]
    public void Tax_is_calculated_only_for_taxable_concept() => Assert.Equal(180m, QuotationProformaCalculator.LineTax(1000m, 18m, true));
}
