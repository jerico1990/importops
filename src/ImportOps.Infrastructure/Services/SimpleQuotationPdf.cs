using System.Globalization;
using System.Text;

namespace ImportOps.Infrastructure.Services;

internal static class SimpleQuotationPdf
{
    internal sealed record Line(string Text, int Size = 9, bool Bold = false, int Indent = 0, bool RuleAfter = false);

    public static byte[] Build(IReadOnlyList<Line> lines)
    {
        var pages = Paginate(lines, 47);
        var objects = new List<byte[]>();
        void Add(string value) => objects.Add(Encoding.ASCII.GetBytes(value));
        Add("<< /Type /Catalog /Pages 2 0 R >>");
        var pageIds = Enumerable.Range(0, pages.Count).Select(i => 5 + i * 2).ToArray();
        Add($"<< /Type /Pages /Kids [{string.Join(" ", pageIds.Select(x => $"{x} 0 R"))}] /Count {pages.Count} >>");
        Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
        foreach (var page in pages)
        {
            var pageId = objects.Count + 1;
            var contentId = pageId + 1;
            Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {contentId} 0 R >>");
            var stream = Content(page);
            var bytes = Encoding.Latin1.GetBytes(stream);
            objects.Add(Concat(Encoding.ASCII.GetBytes($"<< /Length {bytes.Length} >>\nstream\n"), bytes, Encoding.ASCII.GetBytes("\nendstream")));
        }
        using var ms = new MemoryStream();
        Write(ms, "%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++) { offsets.Add(ms.Position); Write(ms, $"{i + 1} 0 obj\n"); ms.Write(objects[i]); Write(ms, "\nendobj\n"); }
        var xref = ms.Position; Write(ms, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var off in offsets.Skip(1)) Write(ms, $"{off:0000000000} 00000 n \n");
        Write(ms, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return ms.ToArray();
    }

    private static List<List<Line>> Paginate(IReadOnlyList<Line> lines, int max)
    { var pages=new List<List<Line>>(); for(var i=0;i<lines.Count;i+=max) pages.Add(lines.Skip(i).Take(max).ToList()); return pages.Count==0?[new List<Line>()]:pages; }
    private static string Content(IReadOnlyList<Line> lines)
    {
        var sb=new StringBuilder(); var y=800; sb.Append("0.8 w 48 815 m 547 815 l S\n");
        foreach(var l in lines){ var font=l.Bold?"F2":"F1"; var safe=Escape(l.Text); sb.Append(CultureInfo.InvariantCulture,$"BT /{font} {l.Size} Tf {52+l.Indent} {y} Td ({safe}) Tj ET\n"); y-=l.Size>=14?22:16; if(l.RuleAfter){sb.Append(CultureInfo.InvariantCulture,$"0.4 w 52 {y+7} m 543 {y+7} l S\n");y-=5;} }
        return sb.ToString();
    }
    private static string Escape(string s)=>s.Replace("\\","\\\\").Replace("(","\\(").Replace(")","\\)").Replace("\r"," ").Replace("\n"," ");
    private static byte[] Concat(params byte[][] xs){using var m=new MemoryStream();foreach(var x in xs)m.Write(x);return m.ToArray();}
    private static void Write(Stream s,string v)=>s.Write(Encoding.Latin1.GetBytes(v));
}
