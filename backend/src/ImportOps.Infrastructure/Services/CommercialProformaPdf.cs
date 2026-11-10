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
        p.Text(left, 782, Clip(d.Company.ToUpperInvariant(), 45), 15, true);
        p.Text(left, 764, "ImportOps", 9);
        p.Text(left, 751, Clip(d.Address, 55), 8);
        p.Text(left, 739, Clip(d.Phone is null ? d.Email : $"TELEF. {d.Phone}", 60), 8);
        p.Text(right, 777, $"CALLAO {d.Date:dd/MM/yyyy}", 9, true);
        p.Text(right, 763, "Señores:", 9, true);
        p.Text(right, 750, Clip(d.Client, 49), 8);
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
        p.Text(right, y - 2, "TOTAL DERECHOS + PERCEPCION US$", 8, true);
        p.Rule(far - 81, y + 9, far, 1);
        p.Right(far, y - 14, Money(d.Duties), 8, true);
        y -= 40;
        p.Rule(right, y, far, 2);
        y -= 22;
        var lastGroup = "";
        foreach (var e in d.Expenses)
        {
            var group = string.IsNullOrWhiteSpace(e.Group) ? "GASTOS Y SERVICIOS" : e.Group.ToUpperInvariant();
            if (!string.Equals(lastGroup, group, StringComparison.OrdinalIgnoreCase))
            {
                if (lastGroup.Length > 0) y -= 10;
                if (y < 100) { p = new Page(); pages.Add(p); y = 780; }
                p.Text(right, y, Clip(group, 43), 9, true);
                p.Rule(right, y - 3, right + 140);
                y -= 16;
                lastGroup = group;
            }
            if (y < 90) { p = new Page(); pages.Add(p); y = 780; }
            p.Text(right, y, Clip(e.Name.ToUpperInvariant(), 40) + (e.NonTaxable ? " **" : ""), 8);
            p.Right(far, y, Money(e.Amount), 8);
            y -= 13;
        }
        if (y < 125) { p = new Page(); pages.Add(p); y = 780; }
        p.Rule(far - 82, y + 8, far, 1);
        p.Text(right, y - 4, "TOTAL GASTOS US$", 9, true); p.Right(far, y - 4, Money(d.ExpensesTotal), 9, true);
        y -= 28;
        p.Text(right, y, "I.G.V. 18 %", 9, true); p.Right(far, y, Money(d.ExpenseTax), 9);
        y -= 28;
        p.Text(right, y, "TOTAL PROFORMA US$", 10, true); p.Right(far, y, Money(d.Total), 11, true);
        // Información complementaria, en la columna izquierda, sin alterar el bloque económico.
        p = pages[0];
        var ly = top - 116;
        p.Text(left, ly, "DATOS BANCARIOS PARA TRANSFERENCIAS", 8, true); ly -= 17;
        if (d.Banks.Count > 0)
        {
            foreach (var bank in d.Banks)
            {
                if (ly < 205) break;
                p.Text(left, ly, Clip(bank.Holder ?? d.Company, 46), 8, true); ly -= 12;
                p.Text(left, ly, $"BANCO: {Clip(bank.Bank, 37)}", 8); ly -= 12;
                p.Text(left, ly, $"CUENTA {bank.Currency}: {bank.Number}", 8); ly -= 12;
                p.Text(left, ly, "CCI: 00000", 8); ly -= 19;
            }
        }
        else
        {
            p.Text(left, ly, "BANCO: 00000", 8); ly -= 13;
            p.Text(left, ly, "CUENTA PEN: 00000", 8); ly -= 13;
            p.Text(left, ly, "CUENTA USD: 00000", 8); ly -= 13;
            p.Text(left, ly, "CCI: 00000", 8); ly -= 15;
        }
        if (!string.IsNullOrWhiteSpace(d.BankInstructions))
            foreach (var segment in d.BankInstructions.Split('\n').Take(3))
            { p.Text(left, ly, Clip(segment, 49), 7); ly -= 12; }
        var notesY = Math.Min(190, ly - 20);
        p.Text(left, notesY, "Proforma", 9);
        p.Text(left, notesY - 20, $"Fecha:  {d.Date:dd/MM/yyyy}", 9);
        p.Rule(left, notesY - 34, left + 180);
        p.Text(left, notesY - 48, "Proforma hecha por:", 8);
        p.Text(left, notesY - 72, "NOTA:", 8, true);
        p.Text(left, notesY - 87, d.ExpenseTax > 0 ? "IGV SOBRE SERVICIOS SEGUN DETALLE" : "SERVICIOS SIN IGV APLICADO", 9, true);
        if (!string.IsNullOrWhiteSpace(d.Notes)) p.Text(left, notesY - 112, Clip(d.Notes, 51), 7);
        if (!string.IsNullOrWhiteSpace(d.Terms)) p.Text(left, notesY - 125, Clip(d.Terms, 51), 7);
        return Encode(pages);
    }

    private static byte[] Encode(IReadOnlyList<Page> pages)
    {
        var objects = new List<byte[]>();
        void Add(string s) => objects.Add(Encoding.Latin1.GetBytes(s));
        Add("<< /Type /Catalog /Pages 2 0 R >>");
        Add($"<< /Type /Pages /Kids [{string.Join(" ", Enumerable.Range(0, pages.Count).Select(i => $"{5 + 2 * i} 0 R"))}] /Count {pages.Count} >>");
        Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
        foreach (var page in pages)
        {
            var contentId = objects.Count + 2;
            Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentId} 0 R >>");
            var bytes = Encoding.Latin1.GetBytes(page.Commands.ToString());
            objects.Add(Combine(Encoding.ASCII.GetBytes($"<< /Length {bytes.Length} >>\nstream\n"), bytes, Encoding.ASCII.GetBytes("\nendstream")));
        }
        using var ms = new MemoryStream();
        void Write(string s) => ms.Write(Encoding.Latin1.GetBytes(s));
        Write("%PDF-1.4\n%âãÏÓ\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(ms.Position); Write($"{i + 1} 0 obj\n"); ms.Write(objects[i]); Write("\nendobj\n");
        }
        var xref = ms.Position;
        Write($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var off in offsets.Skip(1)) Write($"{off:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return ms.ToArray();
    }
    private static byte[] Combine(params byte[][] chunks)
    {
        using var ms = new MemoryStream(); foreach (var chunk in chunks) ms.Write(chunk); return ms.ToArray();
    }
}
