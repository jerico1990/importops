namespace ImportOps.Application.Quotations;

public static class QuotationProformaCalculator
{
    public static decimal CustomsValue(decimal exw, decimal freight, decimal insurance) => Round(exw + freight + insurance);
    public static decimal ImportTaxBase(decimal customsValue, decimal adValorem) => Round(customsValue + adValorem);
    public static decimal CustomsValueTax(decimal customsValue, decimal rate, int roundingScale = 0) => RoundTo(customsValue * rate / 100m, roundingScale);
    public static decimal PerceptionBase(decimal customsValue, decimal adValorem, decimal importIgv, decimal ipm) => Round(customsValue + adValorem + importIgv + ipm);
    public static decimal Perception(decimal customsValue, decimal adValorem, decimal importIgv, decimal ipm, decimal rate, int roundingScale = 0) => RoundTo(PerceptionBase(customsValue, adValorem, importIgv, ipm) * rate / 100m, roundingScale);
    public static decimal LineTax(decimal taxableBase, decimal taxRate, bool isTaxable) => isTaxable ? Round(taxableBase * taxRate / 100m) : 0m;
    public static decimal Sum(params decimal[] values) => Round(values.Sum());
    public static decimal Total(decimal customsDuties, decimal expenses, decimal expenseTax) => Round(customsDuties + expenses + expenseTax);
    private static decimal Round(decimal value) => decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    private static decimal RoundTo(decimal value, int scale) => decimal.Round(value, scale, MidpointRounding.AwayFromZero);
}
