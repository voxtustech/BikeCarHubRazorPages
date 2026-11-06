using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using BikeCarHub.Domain;

namespace BikeCarHub.Services;

/// <summary>An image from the Word file, matched to a published web image.</summary>
public sealed record DocxImage(string Url, string VirtualPath, int Width, int Height);
public sealed record DocxFigure(DocxImage Image, string Alt, string? Caption);

public sealed class DocxArticle
{
    public string? Title { get; set; }
    /// <summary>The Word "Title" document property, a shorter title meant for search results.</summary>
    public string? DocumentTitle { get; set; }
    public string? Author { get; set; }
    public DateTime? Created { get; set; }
    /// <summary>The small line above the title, such as "BIKECARHUB · NEW BIKES · ROYAL ENFIELD".</summary>
    public string? Kicker { get; set; }
    /// <summary>The first paragraph under the title: the intro shown as the lead and used as the meta description.</summary>
    public string? Standfirst { get; set; }
    public DocxFigure? Cover { get; set; }
    public string BodyHtml { get; set; } = "";
    public int TextLength { get; set; }
    public List<ArticleFaq> Faqs { get; } = new();
    public HashSet<string> UsedImages { get; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>Embedded images with no matching web file; they are left out of the page.</summary>
    public int MissingImages { get; set; }
    /// <summary>The template's colour theme ("orange", "blue"), from the accent colour the file uses most.</summary>
    public string? Theme { get; set; }
}

/// <summary>
/// Turns a Word article laid out on the BikeCarHub template into page HTML: kicker, title, intro, cover, headings,
/// figures with captions, image-and-text blocks, image grids, a stat strip, quotes, callouts, data tables and FAQs.
/// The template's colours (cell shading and coloured text) are kept as CSS classes, because the site's
/// Content-Security-Policy does not allow inline styles. Every piece of text is HTML-encoded.
/// </summary>
public static partial class RichDocxReader
{
    private static readonly XNamespace W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace A = "http://schemas.openxmlformats.org/drawingml/2006/main";
    private static readonly XNamespace WP = "http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing";
    private static readonly XNamespace PR = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace DC = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace DCT = "http://purl.org/dc/terms/";

    // The BikeCarHub article template palettes -> CSS classes (site.css, "Articles laid out in Word").
    // Each palette uses the same roles (dark, accent, light box, tint), so one set of classes serves them all;
    // the page's theme class (word-theme-*) sets the actual colours.
    private static readonly Dictionary<string, string> FillClass = new(StringComparer.OrdinalIgnoreCase)
    {
        // Orange template
        ["1E2328"] = "fill-dark",
        ["C4501B"] = "fill-accent",
        ["F6EFE6"] = "fill-cream",
        ["FBE3D6"] = "fill-peach",
        // Blue template
        ["0B2545"] = "fill-dark",
        ["0077B6"] = "fill-accent",
        ["EEF4FA"] = "fill-cream",
        ["DCEBF7"] = "fill-peach",
        ["F2F2F2"] = "fill-grey",
        // Red template
        ["161616"] = "fill-dark",
        ["D21F2B"] = "fill-accent",
        ["F5F1EC"] = "fill-cream",
        ["F6D9DB"] = "fill-peach",
        ["FBE3E4"] = "fill-peach",
        ["000000"] = "fill-black"
    };

    private static readonly Dictionary<string, string> TextClass = new(StringComparer.OrdinalIgnoreCase)
    {
        ["C4501B"] = "text-accent",
        ["0077B6"] = "text-accent",
        ["D21F2B"] = "text-accent",
        ["6B6B6B"] = "text-muted"
    };

    /// <summary>Accent colour -> theme name.</summary>
    private static readonly Dictionary<string, string> ThemeByAccent = new(StringComparer.OrdinalIgnoreCase)
    {
        ["0077B6"] = "blue",
        ["D21F2B"] = "red",
        ["C4501B"] = "orange"
    };

    [GeneratedRegex(@"^heading\s?(\d)$", RegexOptions.IgnoreCase)]
    private static partial Regex HeadingStyle();

    [GeneratedRegex(@"^(image|photo|picture|source)\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex CreditOnly();

    [GeneratedRegex(@"\b(image|photo|source)\s*:", RegexOptions.IgnoreCase)]
    private static partial Regex HasCredit();

    [GeneratedRegex(@"frequently asked|\bfaqs?\b", RegexOptions.IgnoreCase)]
    private static partial Regex FaqHeading();

    [GeneratedRegex(@"^q\s*[.:)]\s*", RegexOptions.IgnoreCase)]
    private static partial Regex QuestionPrefix();

    private sealed record Drawing(string RelId, string? Alt, int WidthPx);

    private sealed record Para(string Text, string Html, string Style, int Level, bool IsList, bool AllBold, bool Centered, List<Drawing> Drawings);

    private sealed record Cell(List<Para> Paras, string? Fill)
    {
        public List<Para> Text => Paras.Where(p => p.Text.Length > 0).ToList();
        public List<Drawing> Drawings => Paras.SelectMany(p => p.Drawings).ToList();
        public bool Centered => Text.Count > 0 && Text.All(p => p.Centered);
    }

    public static DocxArticle? Read(string docxPath, Func<byte[], DocxImage?> resolveImage, string placeholder) =>
        Read(docxPath, (_, bytes) => resolveImage(bytes), placeholder);

    /// <param name="resolveImage">Gets the picture's file name inside the Word file (for example "image1.jpg") and its bytes.</param>
    public static DocxArticle? Read(string docxPath, Func<string, byte[], DocxImage?> resolveImage, string placeholder)
    {
        XDocument doc;
        var rels = new Dictionary<string, (string Target, bool External)>();
        var imageBytes = new Dictionary<string, byte[]>();
        var result = new DocxArticle();
        try
        {
            using var zip = ZipFile.OpenRead(docxPath);
            var entry = zip.GetEntry("word/document.xml");
            if (entry is null) return null;
            using (var s = entry.Open()) doc = XDocument.Load(s);

            if (zip.GetEntry("word/_rels/document.xml.rels") is { } relEntry)
            {
                using var s = relEntry.Open();
                foreach (var rel in XDocument.Load(s).Root!.Elements(PR + "Relationship"))
                {
                    var id = (string?)rel.Attribute("Id");
                    var target = (string?)rel.Attribute("Target");
                    if (id is null || target is null) continue;
                    var external = string.Equals((string?)rel.Attribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase);
                    rels[id] = (target, external);
                    if (!external && zip.GetEntry("word/" + target.TrimStart('/')) is { } media && media.Length < 20_000_000)
                    {
                        using var ms = new MemoryStream();
                        using (var ms2 = media.Open()) ms2.CopyTo(ms);
                        imageBytes[id] = ms.ToArray();
                    }
                }
            }

            if (zip.GetEntry("docProps/core.xml") is { } core)
            {
                using var s = core.Open();
                var props = XDocument.Load(s).Root!;
                result.DocumentTitle = Clean(props.Element(DC + "title")?.Value);
                result.Author = Clean(props.Element(DC + "creator")?.Value);
                if (DateTime.TryParse(props.Element(DCT + "created")?.Value, null, System.Globalization.DateTimeStyles.AdjustToUniversal, out var created))
                    result.Created = created;
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Xml.XmlException)
        {
            return null;
        }

        var body = doc.Root?.Element(W + "body");
        if (body is null) return null;

        result.Theme = body.Descendants()
            .Where(e => e.Name == W + "shd" || e.Name == W + "color")
            .Select(e => (string?)e.Attribute(W + "fill") ?? (string?)e.Attribute(W + "val"))
            .Where(v => v is not null && ThemeByAccent.ContainsKey(v))
            .GroupBy(v => ThemeByAccent[v!])
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        var resolved = new Dictionary<string, DocxImage?>();
        DocxImage? Image(string relId)
        {
            if (resolved.TryGetValue(relId, out var hit)) return hit;
            var img = imageBytes.TryGetValue(relId, out var bytes) ? resolveImage(Path.GetFileName(rels[relId].Target), bytes) : null;
            if (img is null) result.MissingImages++;
            else result.UsedImages.Add(img.VirtualPath);
            return resolved[relId] = img;
        }

        var elements = body.Elements().Where(e => e.Name == W + "p" || e.Name == W + "tbl").ToList();
        var paras = elements.ToDictionary(e => e, e => e.Name == W + "p" ? ParsePara(e, rels) : null);

        var titleIndex = elements.FindIndex(e => paras[e] is { Level: 1 });
        var titleFromStyle = false;
        if (titleIndex >= 0)
        {
            result.Title = paras[elements[titleIndex]]!.Text;
            titleFromStyle = paras[elements[titleIndex]]!.Style.Equals("Title", StringComparison.OrdinalIgnoreCase);
            // A short line above the title is the kicker ("BIKECARHUB · NEW BIKES · ROYAL ENFIELD").
            result.Kicker = elements.Take(titleIndex).Select(e => paras[e]).LastOrDefault(p => p is { Text.Length: > 0 and <= 80 })?.Text;
        }
        else result.Title = result.DocumentTitle;

        var sb = new StringBuilder();
        var ids = new HashSet<string>();
        var inList = false;
        var inFaq = false;
        var faqOpen = false;
        string? question = null;
        var answer = new List<Para>();
        var photoNumber = 0;
        var started = false; // the first section heading has been written

        void CloseList()
        {
            if (inList) sb.Append("</ul>");
            inList = false;
        }

        void FlushQuestion()
        {
            if (question is null) return;
            if (!faqOpen) { sb.Append("<div class=\"faq-list\">"); faqOpen = true; }
            sb.Append("<div class=\"faq-item\"><h3><span class=\"faq-q\" aria-hidden=\"true\">Q.</span> ").Append(Enc(question)).Append("</h3>");
            foreach (var a in answer) sb.Append("<p>").Append(a.Html).Append("</p>");
            sb.Append("</div>");
            if (answer.Count > 0) result.Faqs.Add(new ArticleFaq(question, string.Join(" ", answer.Select(a => a.Text))));
            question = null;
            answer.Clear();
        }

        void CloseFaq()
        {
            FlushQuestion();
            if (faqOpen) sb.Append("</div>");
            faqOpen = false;
        }

        string AltFor(Drawing d, string? caption)
        {
            var alt = Clean(d.Alt);
            if (alt is not null && !CreditOnly().IsMatch(alt)) return alt;
            if (caption is not null && !CreditOnly().IsMatch(caption)) return caption;
            // "Royal Enfield Himalayan 750: Specs, ..." -> "Royal Enfield Himalayan 750 – photo 1"
            return $"{result.Title?.Split(':')[0].Trim()} – photo {++photoNumber}";
        }

        string ImgTag(DocxImage img, string alt, bool eager = false) =>
            $"<img src=\"{Attr(img.Url)}\" alt=\"{Attr(alt)}\" width=\"{img.Width}\" height=\"{img.Height}\"" +
            (eager ? "" : " loading=\"lazy\" decoding=\"async\"") + $" data-fallback=\"{Attr(placeholder)}\">";

        for (var i = titleIndex + 1; i < elements.Count; i++)
        {
            var el = elements[i];
            if (paras[el] is not { } p)
            {
                CloseList();
                if (inFaq) CloseFaq();
                result.TextLength += AppendTable(el, rels, sb, Image, AltFor, ImgTag);
                continue;
            }

            if (p.Drawings.Count > 0)
            {
                // A short line straight after a picture ("... Image: Royal Enfield") is its caption.
                string? caption = null;
                if (i + 1 < elements.Count && paras[elements[i + 1]] is { Drawings.Count: 0, Level: 0 } next && IsCaption(next))
                {
                    caption = next.Text;
                    i++;
                }
                CloseList();
                foreach (var d in p.Drawings)
                {
                    if (Image(d.RelId) is not { } img) continue;
                    var figure = new DocxFigure(img, AltFor(d, caption), caption);
                    if (!started && result.Cover is null && sb.Length == 0)
                    {
                        result.Cover = figure;
                        continue;
                    }
                    sb.Append("<figure class=\"article-figure\">").Append(ImgTag(img, figure.Alt));
                    if (caption is not null) sb.Append("<figcaption>").Append(Enc(caption)).Append("</figcaption>");
                    sb.Append("</figure>");
                    caption = null;
                }
                if (p.Text.Length == 0) continue;
            }
            if (p.Text.Length == 0) continue;

            result.TextLength += p.Text.Length;

            if (p.Level > 0)
            {
                CloseList();
                if (inFaq) CloseFaq();
                var level = Math.Clamp(p.Level + (titleFromStyle ? 1 : 0), 2, 4);
                if (level == 2)
                {
                    var id = Unique(Slugger.Slugify(p.Text), ids);
                    sb.Append("<h2 id=\"").Append(id).Append("\">").Append(Enc(p.Text)).Append("</h2>");
                    inFaq = FaqHeading().IsMatch(p.Text);
                }
                else sb.Append("<h").Append(level).Append('>').Append(Enc(p.Text)).Append("</h").Append(level).Append('>');
                started = true;
                continue;
            }

            if (!started && result.Standfirst is null && sb.Length == 0 && result.Cover is null && !p.IsList)
            {
                result.Standfirst = p.Text;
                continue;
            }

            if (inFaq && !p.IsList)
            {
                if (p.AllBold) { FlushQuestion(); question = QuestionPrefix().Replace(p.Text, ""); continue; }
                if (question is not null) { answer.Add(p); continue; }
            }

            if (p.IsList)
            {
                if (!inList) sb.Append("<ul class=\"square-list\">");
                inList = true;
                sb.Append("<li>").Append(p.Html).Append("</li>");
                continue;
            }

            CloseList();
            sb.Append("<p>").Append(p.Html).Append("</p>");
        }
        CloseList();
        CloseFaq();

        result.BodyHtml = sb.ToString();
        return result.Title is null || result.TextLength == 0 ? null : result;
    }

    private static int AppendTable(XElement table, Dictionary<string, (string Target, bool External)> rels, StringBuilder sb,
        Func<string, DocxImage?> image, Func<Drawing, string?, string> altFor, Func<DocxImage, string, bool, string> imgTag)
    {
        var rows = table.Elements(W + "tr")
            .Select(tr => tr.Elements(W + "tc").Select(tc => new Cell(
                tc.Elements(W + "p").Select(p => ParsePara(p, rels)).ToList(),
                FillOf(tc.Element(W + "tcPr")?.Element(W + "shd")))).ToList())
            .Where(r => r.Count > 0)
            .ToList();
        if (rows.Count == 0) return 0;

        var cells = rows.SelectMany(r => r).ToList();
        var length = cells.Sum(c => c.Text.Sum(p => p.Text.Length));

        string Figure(Drawing d, string? caption, string css)
        {
            if (image(d.RelId) is not { } img) return "";
            var f = new StringBuilder($"<figure class=\"{css}\">").Append(imgTag(img, altFor(d, caption), false));
            if (caption is not null) f.Append("<figcaption>").Append(Enc(caption)).Append("</figcaption>");
            return f.Append("</figure>").ToString();
        }

        static string Classes(params string?[] names) =>
            names.Any(n => n is not null) ? $" class=\"{string.Join(' ', names.Where(n => n is not null))}\"" : "";

        static string Paragraphs(IEnumerable<Para> ps) => string.Concat(ps.Select(p => p.IsList ? $"<ul class=\"square-list\"><li>{p.Html}</li></ul>" : $"<p>{p.Html}</p>"));

        // Picture in one cell, text in the other: an image-and-text block.
        if (rows.Count == 1 && rows[0].Count == 2)
        {
            var (left, right) = (rows[0][0], rows[0][1]);
            var leftIsImage = left.Drawings.Count > 0 && left.Text.Count == 0 && right.Drawings.Count == 0 && right.Text.Count > 0;
            var rightIsImage = right.Drawings.Count > 0 && right.Text.Count == 0 && left.Drawings.Count == 0 && left.Text.Count > 0;
            if (leftIsImage || rightIsImage)
            {
                var (pics, text) = leftIsImage ? (left, right) : (right, left);
                sb.Append(leftIsImage ? "<div class=\"media-text\">" : "<div class=\"media-text media-text-reverse\">");
                foreach (var d in pics.Drawings) sb.Append(Figure(d, null, $"media-text-figure {pics.Fill}".Trim()));
                sb.Append("<div class=\"media-text-body\">").Append(Paragraphs(text.Text)).Append("</div></div>");
                return length;
            }
        }

        // Every filled cell holds a picture: an image grid, with the cell text as the caption.
        var filled = cells.Where(c => c.Drawings.Count > 0 || c.Text.Count > 0).ToList();
        if (filled.Count >= 2 && filled.All(c => c.Drawings.Count > 0))
        {
            var small = filled.SelectMany(c => c.Drawings).All(d => d.WidthPx is > 0 and <= 160);
            // Keep the Word table's column count (2x2 gallery, row of 5 colour swatches).
            var cols = Math.Clamp(rows.Max(r => r.Count), 1, 6);
            sb.Append("<div class=\"figure-grid cols-").Append(cols).Append(small ? " figure-grid-swatches\">" : "\">");
            foreach (var c in filled)
            {
                var caption = string.Join(" ", c.Text.Select(p => p.Text));
                foreach (var d in c.Drawings) sb.Append(Figure(d, caption.Length > 0 ? caption : null, $"grid-figure {c.Fill}".Trim()));
            }
            sb.Append("</div>");
            return length;
        }

        var noImages = cells.All(c => c.Drawings.Count == 0);

        // One row of "value / label" cells: the at-a-glance stat strip, keeping each box's colour.
        if (noImages && rows.Count == 1 && rows[0].Count >= 3 && rows[0].All(c => c.Text.Count == 2))
        {
            sb.Append("<ul class=\"stat-strip\">");
            foreach (var c in rows[0])
            {
                sb.Append("<li").Append(Classes(c.Fill)).Append("><strong>").Append(Enc(c.Text[0].Text)).Append("</strong><span>")
                    .Append(Enc(c.Text[1].Text)).Append("</span></li>");
            }
            sb.Append("</ul>");
            return length;
        }

        // A single boxed cell: a pull quote when it opens with a quote mark, otherwise a highlighted note.
        if (noImages && rows.Count == 1 && rows[0].Count == 1)
        {
            var t = rows[0][0].Text;
            if (t.Count == 0) return 0;
            if (t[0].Text[0] is '“' or '"' or '‘')
            {
                var cite = t.Count > 1 && t[^1].Text[0] is not ('“' or '"' or '‘') ? t[^1] : null;
                sb.Append("<figure class=\"pull-quote\"><blockquote>")
                    .Append(string.Concat((cite is null ? t : t.SkipLast(1)).Select(p => $"<p>{Enc(p.Text)}</p>"))).Append("</blockquote>");
                if (cite is not null) sb.Append("<figcaption>").Append(Enc(cite.Text)).Append("</figcaption>");
                sb.Append("</figure>");
            }
            else
            {
                sb.Append("<aside class=\"callout\">");
                if (t.Count > 1 && t[0].Text.Length <= 80)
                {
                    sb.Append("<p class=\"callout-title\">").Append(Enc(t[0].Text)).Append("</p>").Append(Paragraphs(t.Skip(1)));
                }
                else sb.Append(Paragraphs(t));
                sb.Append("</aside>");
            }
            return length;
        }

        // Any other one-row table is only layout: write its cells out in order.
        if (rows.Count == 1)
        {
            foreach (var c in rows[0])
            {
                foreach (var d in c.Drawings) sb.Append(Figure(d, null, "article-figure"));
                sb.Append(Paragraphs(c.Text));
            }
            return length;
        }

        // A real data table: header row, first column as row headers, cell shading and alignment kept,
        // so a highlighted column (for example the bike the article is about) stays highlighted.
        var columns = rows.Max(r => r.Count);
        sb.Append("<div class=\"table-wrap\"><table class=\"article-table\"><thead><tr>");
        foreach (var c in rows[0])
            sb.Append("<th scope=\"col\"").Append(Classes(c.Fill, c.Centered ? "center" : null)).Append('>').Append(CellHtml(c)).Append("</th>");
        sb.Append("</tr></thead><tbody>");
        foreach (var r in rows.Skip(1))
        {
            sb.Append("<tr>");
            for (var i = 0; i < r.Count; i++)
            {
                var c = r[i];
                var tag = i == 0 && columns >= 2 ? "th" : "td";
                sb.Append('<').Append(tag).Append(tag == "th" ? " scope=\"row\"" : "").Append(Classes(c.Fill, c.Centered ? "center" : null))
                    .Append('>').Append(CellHtml(c)).Append("</").Append(tag).Append('>');
            }
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table></div>");
        return length;

        static string CellHtml(Cell cell) => string.Join("<br>", cell.Text.Select(p => p.Html));
    }

    private static string? FillOf(XElement? shd)
    {
        var fill = (string?)shd?.Attribute(W + "fill");
        return fill is not null && FillClass.TryGetValue(fill, out var css) ? css : null;
    }

    private static Para ParsePara(XElement p, Dictionary<string, (string Target, bool External)> rels)
    {
        var props = p.Element(W + "pPr");
        var style = (string?)props?.Element(W + "pStyle")?.Attribute(W + "val") ?? "";
        var heading = HeadingStyle().Match(style);
        var level = style.Equals("Title", StringComparison.OrdinalIgnoreCase) ? 1 : heading.Success ? int.Parse(heading.Groups[1].Value) : 0;
        var centered = (string?)props?.Element(W + "jc")?.Attribute(W + "val") is "center";

        // Runs with the same formatting are merged so the HTML stays clean.
        var segments = new List<(string Text, bool Bold, bool Italic, string? Colour, string? Href)>();
        foreach (var run in p.Descendants(W + "r"))
        {
            if (run.Ancestors().TakeWhile(a => a != p).Any(a => a.Name == W + "del")) continue;
            var text = new StringBuilder();
            foreach (var node in run.Elements())
            {
                if (node.Name == W + "t") text.Append(node.Value);
                else if (node.Name == W + "tab" || node.Name == W + "br") text.Append(' ');
            }
            if (text.Length == 0) continue;
            var rPr = run.Element(W + "rPr");
            var bold = On(rPr?.Element(W + "b")) || ((string?)rPr?.Element(W + "rStyle")?.Attribute(W + "val"))?.Equals("Strong", StringComparison.OrdinalIgnoreCase) == true;
            var italic = On(rPr?.Element(W + "i"));
            var colourValue = (string?)rPr?.Element(W + "color")?.Attribute(W + "val");
            var colour = colourValue is not null && TextClass.TryGetValue(colourValue, out var css) ? css : null;
            string? href = null;
            var link = run.Ancestors(W + "hyperlink").FirstOrDefault();
            if ((string?)link?.Attribute(R + "id") is { } relId && rels.TryGetValue(relId, out var rel) && rel.External
                && (rel.Target.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || rel.Target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
                href = rel.Target;
            if (segments.Count > 0 && segments[^1] is var last && last.Bold == bold && last.Italic == italic && last.Colour == colour && last.Href == href)
                segments[^1] = (last.Text + text, bold, italic, colour, href);
            else segments.Add((text.ToString(), bold, italic, colour, href));
        }

        if (segments.Count > 0)
        {
            segments[0] = segments[0] with { Text = segments[0].Text.TrimStart() };
            segments[^1] = segments[^1] with { Text = segments[^1].Text.TrimEnd() };
        }
        var plain = string.Concat(segments.Select(s => s.Text)).Trim();
        var html = new StringBuilder();
        foreach (var s in segments.Where(s => s.Text.Length > 0))
        {
            var inner = Enc(s.Text);
            if (s.Italic) inner = $"<em>{inner}</em>";
            if (s.Bold && level == 0) inner = $"<strong>{inner}</strong>";
            if (s.Colour is not null) inner = $"<span class=\"{s.Colour}\">{inner}</span>";
            if (s.Href is not null) inner = $"<a href=\"{Attr(s.Href)}\" rel=\"noopener\">{inner}</a>";
            html.Append(inner);
        }
        var allBold = segments.Any(s => s.Text.Trim().Length > 0) && segments.Where(s => s.Text.Trim().Length > 0).All(s => s.Bold);

        var drawings = p.Descendants(W + "drawing").Select(d =>
        {
            var blip = d.Descendants(A + "blip").FirstOrDefault();
            var relId = (string?)blip?.Attribute(R + "embed");
            var docPr = d.Descendants(WP + "docPr").FirstOrDefault();
            var cx = (long?)d.Descendants(WP + "extent").FirstOrDefault()?.Attribute("cx") ?? 0;
            return relId is null ? null : new Drawing(relId, (string?)docPr?.Attribute("descr") ?? (string?)docPr?.Attribute("title"), (int)(cx / 9525));
        }).Where(d => d is not null).Select(d => d!).ToList();

        return new Para(plain, html.ToString(), style, level, props?.Element(W + "numPr") is not null, allBold, centered, drawings);
    }

    private static bool IsCaption(Para p) =>
        p.Text.Length <= 160 && !p.IsList && (p.Style.Contains("caption", StringComparison.OrdinalIgnoreCase) || HasCredit().IsMatch(p.Text));

    private static bool On(XElement? toggle) =>
        toggle is not null && (string?)toggle.Attribute(W + "val") is not ("0" or "false" or "none");

    private static string Unique(string id, HashSet<string> used)
    {
        if (id.Length == 0) id = "section";
        var candidate = id;
        for (var n = 2; !used.Add(candidate); n++) candidate = $"{id}-{n}";
        return candidate;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Enc(string text) => WebUtility.HtmlEncode(text);
    private static string Attr(string text) => WebUtility.HtmlEncode(text);
}
