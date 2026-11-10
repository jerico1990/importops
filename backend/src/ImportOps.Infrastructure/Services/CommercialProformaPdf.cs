using System.Globalization;
using System.Text;

namespace ImportOps.Infrastructure.Services;

// Plantilla comercial inspirada en la proforma de referencia: columnas paralelas,
// derechos y gastos a la derecha, datos de pago y notas a la izquierda.
internal static class CommercialProformaPdf
{
    internal sealed record AmountLine(string Name, decimal Amount, bool NonTaxable);
    internal sealed record ExpenseLine(string Name, decimal Amount, bool NonTaxable, string Group);
    internal sealed record BankLine(string Bank, string Currency, string Number, string? Holder);
    internal sealed record Data(
        string Company, string? Address, string? Phone, string? Email,
        string Client, string Number, DateTime Date, string? Reference,
        string? Content, string? Regime, string? CustomsOffice,
        DateTime? Arrival, decimal? ExchangeRate, decimal Exw, decimal Freight,
        decimal Insurance, decimal CustomsValue, decimal? Weight, decimal? Packages,
        decimal? Volume, IReadOnlyList<string> Suppliers, IReadOnlyList<string> Cargo,
        IReadOnlyList<AmountLine> Taxes, IReadOnlyList<ExpenseLine> Expenses,
        IReadOnlyList<BankLine> Banks, string? BankInstructions, string? Notes,
        decimal Duties, decimal ExpensesTotal, decimal ExpenseTax, decimal Total,
        string Currency, string? Terms);

    private sealed class Page
    {
        public StringBuilder Commands { get; } = new();
        public void Text(double x, double y, string? value, int size = 8, bool bold = false)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            Commands.Append(CultureInfo.InvariantCulture, $"BT /{(bold ? "F2" : "F1")} {size} Tf {x:0.##} {y:0.##} Td ({Escape(value)}) Tj ET\n");
        }
        public void Right(double x, double y, string? value, int size = 8, bool bold = false)
        {
            if (string.IsNullOrEmpty(value)) return;
            var width = value.Sum(c => c is ' ' ? 0.27 : c is 'i' or 'l' or '.' or ',' or ':' ? 0.25 : c is 'M' or 'W' ? 0.8 : 0.52) * size;
            Text(x - width, y, value, size, bold);
        }
        public void Rule(double x, double y, double end, double thickness = 0.6)
            => Commands.Append(CultureInfo.InvariantCulture, $"{thickness:0.##} w {x:0.##} {y:0.##} m {end:0.##} {y:0.##} l S\n");
        public void Yellow(double x, double y, double width, double height)
            => Commands.Append(CultureInfo.InvariantCulture, $"1 1 0.35 rg {x:0.##} {y:0.##} {width:0.##} {height:0.##} re f 0 0 0 rg\n");
    }
    private static string Money(decimal n) => n.ToString("N2", CultureInfo.GetCultureInfo("en-US"));
    private static string Three(decimal n) => n.ToString("N3", CultureInfo.GetCultureInfo("en-US"));
    private static string Clip(string? s, int length = 49) => string.IsNullOrWhiteSpace(s) ? "" : s!.Length <= length ? s : s[..(length - 3)] + "...";
    private static string Escape(string s) => new string(s.Normalize(NormalizationForm.FormC).Select(c => c <= 255 ? c : '?').ToArray()).Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("\n", " ").Replace("\r", " ");

    public static byte[] Build(Data d)
    {
        var pages = new List<Page> { new() };
        var p = pages[0];
        const double left = 47, right = 332, far = 562;
        // Encabezado comercial fijo. Mantener la columna izquierda dentro de x=47..360
        // y la fecha/destinatario en x=390..562 para evitar superposiciones.
        const double headerRight = 390;
        p.Text(left, 782, "R&B Freight Cargo S.A.C. - ImportOps", 11, true);
        p.Text(left, 765, Clip(d.Address, 50), 8);
        p.Text(left, 752, Clip(d.Phone is null ? d.Email : $"TELEF. {d.Phone}", 50), 8);
        p.Text(headerRight, 782, $"CALLAO {d.Date:dd/MM/yyyy}", 9, true);
        p.Text(headerRight, 765, "Señores:", 9, true);
        p.Text(headerRight, 751, Clip(d.Client, 30), 8);
        p.Text(left, 717, $"PROFORMA N° {d.Number}", 15, true);
        p.Text(left, 700, $"FILE {Clip(d.Reference, 25)}", 9, true);
        p.Text(right, 700, "Contenido:", 8);
        p.Right(far, 700, Clip(d.Content, 30), 8);
        void Pair(double y, string a, string? av, string b, string? bv)
        { p.Text(left, y, a, 8); p.Text(150, y, Clip(av, 26), 8); p.Text(right, y, b, 8); p.Right(far, y, Clip(bv, 28), 8); }
        Pair(683, "Régimen:", d.Regime, "Proveedor:", string.Join(" / ", d.Suppliers));
        Pair(670, "Aduana:", d.CustomsOffice, "Origen:", "");
        Pair(657, "Nave/Avión:", "", "Fch. de Llegada:", d.Arrival?.ToString("dd/MM/yyyy"));
        Pair(644, "Valor Aduanas US$:", Three(d.CustomsValue), "Tipo de Cambio:", d.ExchangeRate?.ToString("N3", CultureInfo.GetCultureInfo("en-US")));
        p.Text(right, 631, "Peso NETO:", 8); p.Right(far, 631, d.Weight.HasValue ? Three(d.Weight.Value) : "", 8);
        p.Rule(left, 612, far);
        p.Text(left, 599, $"EXW US$:  {Three(d.Exw)}", 8, true);
        p.Text(230, 599, $"FLETE US$:  {Money(d.Freight)}", 8);
        p.Text(397, 599, $"SEGURO US$:  {Three(d.Insurance)}", 8);
        p.Rule(left, 589, far);
        var cargoY = 574.0;
        foreach (var c in d.Cargo.Take(2)) { p.Text(left, cargoY, Clip(c, 105), 7); cargoY -= 12; }
        if (d.Packages.HasValue || d.Volume.HasValue)
            p.Text(left, cargoY, $"{d.Packages:0} BULTOS  {d.Volume:0.##} CBM APROX", 8);
        var top = Math.Min(cargoY - 24, 522);
        p.Text(left, top, "OBSERVACIONES:", 9, true);
        p.Text(left, top - 18, "Partida:", 8);
        p.Text(right + 36, top + 1, "DOLARES AMERICANOS", 10, true);
        var y = top - 20;
        p.Text(right, y, "DERECHOS DE ADUANAS", 9, true);
        p.Rule(right, y - 3, right + 144, 0.5);
        y -= 17;
        foreach (var t in d.Taxes)
        {
            p.Text(right, y, Clip(t.Name.ToUpperInvariant(), 35), 8);
            p.Right(far, y, Money(t.Amount), 8);
            y -= 13;
        }