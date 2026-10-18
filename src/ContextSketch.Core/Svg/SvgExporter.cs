using System.Globalization;
using System.Xml.Linq;
using ContextSketch.Core.Geometry;
using ContextSketch.Core.Model;

namespace ContextSketch.Core.Svg;

/// <summary>Draws a diagram as a standalone SVG, built from the model rather than from the WPF canvas.</summary>
public static class SvgExporter
{
    private static readonly XNamespace Ns = "http://www.w3.org/2000/svg";

    private const double Margin = 40;
    private const double LabelFontSize = 12;
    private const double SmallFontSize = 11;
    private const double NameFontSize = 14;
    private const double LineHeight = 1.25;
    private const double GlyphSize = 26;

    public static string Export(Diagram diagram)
    {
        var bounds = diagram.Elements.Select(ElementShapes.BoundsOf).Aggregate(Rect.Union).Inflate(Margin);
        var byId = diagram.Elements.ToDictionary(e => e.Id);

        var svg = new XElement(Ns + "svg",
            new XAttribute("viewBox", Num(bounds.Left) + " " + Num(bounds.Top) + " " + Num(bounds.Width) + " " + Num(bounds.Height)),
            new XAttribute("width", Num(bounds.Width)),
            new XAttribute("height", Num(bounds.Height)),
            new XAttribute("font-family", DiagramStyle.FontFamily),
            Defs(),
            new XElement(Ns + "title", diagram.Title),
            new XElement(Ns + "rect",
                new XAttribute("x", Num(bounds.Left)), new XAttribute("y", Num(bounds.Top)),
                new XAttribute("width", Num(bounds.Width)), new XAttribute("height", Num(bounds.Height)),
                new XAttribute("fill", DiagramStyle.Background)));

        // Same order as the screen: relationships underneath, elements on top.
        foreach (var r in diagram.Relationships)
            svg.Add(Relationship(r, byId[r.From], byId[r.To]));
        foreach (var e in diagram.Elements)
            svg.Add(Element(e));

        return new XDocument(svg).ToString();
    }

    // ---- Relationships ---------------------------------------------------------------------

    private static XElement Defs()
    {
        var arrow = new XElement(Ns + "marker",
            new XAttribute("id", "arrow"),
            new XAttribute("viewBox", "0 0 12 10"),
            new XAttribute("markerWidth", "12"), new XAttribute("markerHeight", "10"),
            new XAttribute("refX", "12"), new XAttribute("refY", "5"),
            new XAttribute("orient", "auto-start-reverse"),
            new XAttribute("markerUnits", "userSpaceOnUse"),
            new XElement(Ns + "path", new XAttribute("d", "M0,0 L12,5 L0,10 Z"), new XAttribute("fill", DiagramStyle.LineColor)));
        return new XElement(Ns + "defs", arrow);
    }

    private static XElement Relationship(Relationship r, Element from, Element to)
    {
        var (start, end) = EdgeGeometry.LineBetween(from, to);
        var line = new XElement(Ns + "line",
            new XAttribute("x1", Num(start.X)), new XAttribute("y1", Num(start.Y)),
            new XAttribute("x2", Num(end.X)), new XAttribute("y2", Num(end.Y)),
            new XAttribute("stroke", DiagramStyle.LineColor), new XAttribute("stroke-width", "1.5"),
            new XAttribute("marker-end", "url(#arrow)"));
        if (r.Direction == RelationshipDirection.TwoWay)
            line.Add(new XAttribute("marker-start", "url(#arrow)"));

        var group = new XElement(Ns + "g",
            new XAttribute("class", "relationship"), new XAttribute("data-id", r.Id), line);

        var mid = EdgeGeometry.Midpoint(start, end);
        var lines = new List<(string Text, double Size, string Color)>();
        if (!string.IsNullOrWhiteSpace(r.Label)) lines.Add((r.Label, LabelFontSize, DiagramStyle.LabelText));
        if (!string.IsNullOrWhiteSpace(r.Technology)) lines.Add(("[" + r.Technology + "]", SmallFontSize, DiagramStyle.LineColor));

        // Centre the label block on the line, each row on a white patch so the line does not strike through it.
        var y = mid.Y - lines.Sum(l => l.Size * LineHeight) / 2;
        foreach (var (text, size, color) in lines)
        {
            var width = EstimateWidth(text, size) + 8;
            var rowHeight = size * LineHeight;
            group.Add(new XElement(Ns + "rect",
                new XAttribute("x", Num(mid.X - width / 2)), new XAttribute("y", Num(y)),
                new XAttribute("width", Num(width)), new XAttribute("height", Num(rowHeight)),
                new XAttribute("fill", DiagramStyle.Background)));
            group.Add(Text(text, mid.X, y + rowHeight / 2, size, color, bold: false));
            y += rowHeight;
        }
        return group;
    }

    // ---- Elements --------------------------------------------------------------------------

    private readonly record struct Palette(string Fill, string Stroke, string Text);

    private static Palette Colours(ElementType type) => type switch
    {
        ElementType.System => new(DiagramStyle.SystemFill, DiagramStyle.SystemStroke, DiagramStyle.SystemText),
        ElementType.PrimaryActor => new(DiagramStyle.PrimaryFill, DiagramStyle.PrimaryStroke, DiagramStyle.PrimaryText),
        ElementType.SecondaryActor => new(DiagramStyle.SecondaryFill, DiagramStyle.SecondaryStroke, DiagramStyle.SecondaryText),
        ElementType.Storage => new(DiagramStyle.StorageFill, DiagramStyle.StorageStroke, DiagramStyle.StorageText),
        _ => new(DiagramStyle.ExternalFill, DiagramStyle.ExternalStroke, DiagramStyle.ExternalText),
    };

    private static XElement Element(Element e)
    {
        var box = ElementShapes.BoundsOf(e);
        var colours = Colours(e.Type);
        var group = new XElement(Ns + "g",
            new XAttribute("class", "element"), new XAttribute("data-id", e.Id), new XAttribute("data-type", Camel(e.Type)));

        group.Add(e.Type switch
        {
            ElementType.Storage => Cylinder(box, colours),
            ElementType.PrimaryActor => Box(box, colours, radius: 10, dashed: false),
            ElementType.SecondaryActor => Box(box, colours, radius: 10, dashed: true),
            ElementType.System => Box(box, colours, radius: 6, dashed: false),
            _ => Box(box, colours, radius: 0, dashed: false),
        });

        var isActor = e.Type is ElementType.PrimaryActor or ElementType.SecondaryActor;
        var nameSize = e.Type == ElementType.System ? 16 : NameFontSize;
        var textWidth = box.Width - 20;

        var nameLines = Wrap(e.Name, MaxChars(textWidth, nameSize), maxLines: 2);
        var tag = e.Type == ElementType.System || string.IsNullOrWhiteSpace(e.Technology)
            ? null
            : Wrap("[" + e.Technology + "]", MaxChars(textWidth, SmallFontSize), maxLines: 1)[0];
        var description = e.Type == ElementType.System && !string.IsNullOrWhiteSpace(e.Description)
            ? Wrap(e.Description, MaxChars(textWidth, SmallFontSize), maxLines: 2)
            : [];

        var total = (isActor ? GlyphSize : 0)
                    + nameLines.Count * nameSize * LineHeight
                    + (tag is null ? 0 : SmallFontSize * LineHeight)
                    + (description.Count > 0 ? 4 + description.Count * SmallFontSize * LineHeight : 0);

        // The cylinder lid takes space at the top, so its text sits a little lower.
        var centreY = box.Center.Y + (e.Type == ElementType.Storage ? 6 : 0);
        var y = centreY - total / 2;
        var cx = box.Center.X;

        if (isActor)
        {
            group.Add(new XElement(Ns + "circle",
                new XAttribute("cx", Num(cx)), new XAttribute("cy", Num(y + 5.5)), new XAttribute("r", "5.5"),
                new XAttribute("fill", colours.Text)));
            group.Add(new XElement(Ns + "path",
                new XAttribute("d", "M" + Num(cx - 13) + "," + Num(y + GlyphSize) + " A13,13 0 0 1 " + Num(cx + 13) + "," + Num(y + GlyphSize) + " Z"),
                new XAttribute("fill", colours.Text)));
            y += GlyphSize;
        }
        foreach (var line in nameLines)
        {
            group.Add(Text(line, cx, y + nameSize * LineHeight / 2, nameSize, colours.Text, bold: true));
            y += nameSize * LineHeight;
        }
        if (tag is not null)
        {
            group.Add(Text(tag, cx, y + SmallFontSize * LineHeight / 2, SmallFontSize, colours.Text, bold: false));
            y += SmallFontSize * LineHeight;
        }
        if (description.Count > 0)
        {
            y += 4;
            foreach (var line in description)
            {
                group.Add(Text(line, cx, y + SmallFontSize * LineHeight / 2, SmallFontSize, colours.Text, bold: false));
                y += SmallFontSize * LineHeight;
            }
        }
        return group;
    }

    private static XElement Box(Rect box, Palette colours, double radius, bool dashed)
    {
        var rect = new XElement(Ns + "rect",
            new XAttribute("x", Num(box.Left)), new XAttribute("y", Num(box.Top)),
            new XAttribute("width", Num(box.Width)), new XAttribute("height", Num(box.Height)),
            new XAttribute("rx", Num(radius)), new XAttribute("ry", Num(radius)),
            new XAttribute("fill", colours.Fill), new XAttribute("stroke", colours.Stroke), new XAttribute("stroke-width", "2"));
        if (dashed) rect.Add(new XAttribute("stroke-dasharray", "4 3"));
        return rect;
    }

    /// <summary>A cylinder: body plus the front edge of the lid, matching the on-screen shape.</summary>
    private static XElement Cylinder(Rect box, Palette colours)
    {
        const double lid = 12;
        var rx = box.Width / 2;
        var l = box.Left;
        var r = box.Right;
        var body = "M" + Num(l) + "," + Num(box.Top + lid)
                   + " A" + Num(rx) + "," + Num(lid) + " 0 0 1 " + Num(r) + "," + Num(box.Top + lid)
                   + " L" + Num(r) + "," + Num(box.Bottom - lid)
                   + " A" + Num(rx) + "," + Num(lid) + " 0 0 1 " + Num(l) + "," + Num(box.Bottom - lid) + " Z";
        var rim = "M" + Num(l) + "," + Num(box.Top + lid)
                  + " A" + Num(rx) + "," + Num(lid) + " 0 0 0 " + Num(r) + "," + Num(box.Top + lid);
        return new XElement(Ns + "g",
            new XElement(Ns + "path", new XAttribute("d", body),
                new XAttribute("fill", colours.Fill), new XAttribute("stroke", colours.Stroke), new XAttribute("stroke-width", "2")),
            new XElement(Ns + "path", new XAttribute("d", rim),
                new XAttribute("fill", "none"), new XAttribute("stroke", colours.Stroke), new XAttribute("stroke-width", "2")));
    }

    // ---- Text and number helpers -----------------------------------------------------------

    private static XElement Text(string text, double x, double yMiddle, double size, string colour, bool bold)
    {
        var element = new XElement(Ns + "text", text,
            new XAttribute("x", Num(x)), new XAttribute("y", Num(yMiddle)),
            new XAttribute("text-anchor", "middle"), new XAttribute("dominant-baseline", "central"),
            new XAttribute("font-size", Num(size)), new XAttribute("fill", colour));
        if (bold) element.Add(new XAttribute("font-weight", "600"));
        return element;
    }

    private static double EstimateWidth(string text, double size) => text.Length * size * 0.55;

    private static int MaxChars(double width, double size) => Math.Max(4, (int)(width / (size * 0.55)));

    /// <summary>Greedy word wrap; text that does not fit in <paramref name="maxLines"/> is cut with an ellipsis.</summary>
    internal static List<string> Wrap(string text, int maxChars, int maxLines)
    {
        var lines = new List<string>();
        var current = "";
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = current.Length == 0 ? word : current + " " + word;
            if (candidate.Length <= maxChars || current.Length == 0)
                current = candidate;
            else
            {
                lines.Add(current);
                current = word;
            }
        }
        lines.Add(current);

        if (lines.Count > maxLines)
        {
            lines = lines.Take(maxLines).ToList();
            lines[^1] = Truncate(lines[^1], maxChars - 1) + "…";
        }
        else if (lines[^1].Length > maxChars)
            lines[^1] = Truncate(lines[^1], maxChars - 1) + "…";
        return lines;
    }

    private static string Truncate(string s, int length) => s.Length <= length ? s : s[..Math.Max(0, length)];

    private static string Camel(ElementType type)
    {
        var name = type.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static string Num(double value)
    {
        value = Math.Round(value, 2);
        if (value == 0) value = 0; // avoids "-0"
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
