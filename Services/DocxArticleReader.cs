using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BikeCarHub.Services;

/// <summary>Reads the text of a Word article into simple encoded HTML (headings, paragraphs, lists, tables).</summary>
public static partial class DocxArticleReader
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    [GeneratedRegex(@"^heading\s?(\d)$", RegexOptions.IgnoreCase)]
    private static partial Regex HeadingRegex();

    public static (string? Html, int Length) Read(string docxPath, string? articleTitle)
    {
        XDocument doc;
        try
        {
            using var zip = ZipFile.OpenRead(docxPath);
            var entry = zip.GetEntry("word/document.xml");
            if (entry is null) return (null, 0);
            using var stream = entry.Open();
            doc = XDocument.Load(stream);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Xml.XmlException)
        {
            return (null, 0);
        }

        var body = doc.Root?.Element(W + "body");
        if (body is null) return (null, 0);

        var sb = new StringBuilder();
        var length = 0;
        var inList = false;
        var first = true;

        void CloseList()
        {
            if (inList) sb.Append("</ul>");
            inList = false;
        }

        foreach (var el in body.Elements())
        {
            if (el.Name == W + "tbl")
            {
                CloseList();
                length += AppendTable(sb, el);
                continue;
            }
            if (el.Name != W + "p") continue;

            var text = TextOf(el);
            if (text.Length == 0) continue;

            var props = el.Element(W + "pPr");
            var style = props?.Element(W + "pStyle")?.Attribute(W + "val")?.Value ?? "";
            var heading = HeadingRegex().Match(style);
            var level = style.Equals("Title", StringComparison.OrdinalIgnoreCase) ? 1
                : heading.Success ? int.Parse(heading.Groups[1].Value) : 0;

            // The page already shows the title as its H1.
            if (first && (level == 1 || SameText(text, articleTitle)))
            {
                first = false;
                continue;
            }
            first = false;

            var encoded = WebUtility.HtmlEncode(text);
            length += text.Length;
            if (level > 0)
            {
                CloseList();
                var tag = level switch { 1 => "h2", 2 => "h3", _ => "h4" };
                sb.Append('<').Append(tag).Append('>').Append(encoded).Append("</").Append(tag).Append('>');
            }
            else if (props?.Element(W + "numPr") is not null)
            {
                if (!inList) sb.Append("<ul>");
                inList = true;
                sb.Append("<li>").Append(encoded).Append("</li>");
            }
            else
            {
                CloseList();
                sb.Append("<p>").Append(encoded).Append("</p>");
            }
        }
        CloseList();
        return length == 0 ? (null, 0) : (sb.ToString(), length);
    }

    private static int AppendTable(StringBuilder sb, XElement table)
    {
        var length = 0;
        var rows = table.Elements(W + "tr").ToList();
        if (rows.Count == 0) return 0;
        sb.Append("<div class=\"table-wrap\"><table>");
        for (var r = 0; r < rows.Count; r++)
        {
            sb.Append("<tr>");
            foreach (var cell in rows[r].Elements(W + "tc"))
            {
                var text = string.Join(" ", cell.Elements(W + "p").Select(TextOf).Where(t => t.Length > 0));
                length += text.Length;
                var tag = r == 0 ? "th" : "td";
                sb.Append('<').Append(tag).Append(r == 0 ? " scope=\"col\">" : ">").Append(WebUtility.HtmlEncode(text)).Append("</").Append(tag).Append('>');
            }
            sb.Append("</tr>");
        }
        sb.Append("</table></div>");
        return length;
    }

    private static string TextOf(XElement paragraph)
    {
        var sb = new StringBuilder();
        foreach (var node in paragraph.Descendants())
        {
            if (node.Name == W + "t") sb.Append(node.Value);
            else if (node.Name == W + "tab" || node.Name == W + "br") sb.Append(' ');
        }
        return sb.ToString().Trim();
    }

    private static bool SameText(string a, string? b) =>
        b is not null && Slugger.Slugify(a) == Slugger.Slugify(b);
}
