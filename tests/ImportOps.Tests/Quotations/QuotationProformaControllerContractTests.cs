using System.Reflection;
using ImportOps.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ImportOps.Tests.Quotations;

public sealed class QuotationProformaControllerContractTests
{
    [Theory]
    [InlineData("GetProforma", "{id:long}/proforma")]
    [InlineData("UpdateProforma", "{id:long}/proforma")]
    [InlineData("GetConcepts", "concepts")]
    [InlineData("GetRegimes", "regimes")]
    [InlineData("GetCustomsOffices", "customs-offices")]
    [InlineData("GetCustomsTaxConcepts", "customs-tax-concepts")]
    [InlineData("AddSupplier", "{id:long}/suppliers")]
    [InlineData("AddCargo", "{id:long}/cargo")]
    [InlineData("AddCustomsTax", "{id:long}/customs-taxes")]
    public void Controller_exposes_proforma_contract(string methodName, string template)
    {
        var method = typeof(QuotationsController).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);
        var route = method!.GetCustomAttributes().OfType<HttpMethodAttribute>().Single();
        Assert.Equal(template, route.Template);
    }
}
