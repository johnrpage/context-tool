using System.Globalization;
using System.Xml.Linq;
using ContextSketch.Core.Geometry;
using ContextSketch.Core.Model;
using ContextSketch.Core.Serialization;
using ContextSketch.Core.Svg;

namespace ContextSketch.Core.Tests;

public class SvgExporterTests
{
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    private static Diagram Shop() => DiagramJson.Load(SampleDiagrams.Shop).Diagram!;

    private static XElement Parse(Diagram d) => XDocument.Parse(SvgExporter.Export(d)).Root!;

    private static IEnumerable<XElement> Elements(XElement root) =>
        root.Elements(Svg + "g").Where(g => (string?)g.Attribute("class") == "element");

    private static IEnumerable<XElement> Relationships(XElement root) =>
        root.Elements(Svg + "g").Where(g => (string?)g.Attribute("class") == "relationship");

    [Fact]
    public void Output_is_well_formed_svg()
    {
        var root = Parse(Shop());
        Assert.Equal(Svg + "svg", root.Name);
        Assert.NotNull(root.Attribute("viewBox"));
    }

    [Fact]
    public void Draws_one_group_per_element_and_per_relationship()
    {
        var root = Parse(Shop());
        Assert.Equal(6, Elements(root).Count());
        Assert.Equal(5, Relationships(root).Count());
        Assert.All(Relationships(root), g => Assert.Single(g.Elements(Svg + "line")));
    }

    [Fact]
    public void Contains_names_and_plain_technology()
    {
        var text = Parse(Shop()).Descendants(Svg + "text").Select(t => t.Value).ToList();
        Assert.Contains("Customer", text);
        Assert.Contains("Web browser", text);
        Assert.Contains("Stripe", text);
        Assert.Contains("Web shop and order handling", text);
        Assert.DoesNotContain(text, t => t.StartsWith('['));
    }

    [Fact]
    public void Technology_is_drawn_faded()
    {
        var pay = Elements(Parse(Shop())).First(g => (string)g.Attribute("data-id")! == "pay");
        var tech = pay.Elements(Svg + "text").Single(t => t.Value == "Stripe");
        Assert.Equal("0.8", (string?)tech.Attribute("fill-opacity"));
    }

    [Fact]
    public void Relationship_labels_only_show_the_label()
    {
        var r3 = Relationships(Parse(Shop())).First(g => (string)g.Attribute("data-id")! == "r3");
        Assert.Equal("Takes payment", string.Join(" ", r3.Elements(Svg + "text").Select(t => t.Value)));
    }

    [Fact]
    public void Long_relationship_labels_wrap_onto_at_most_three_lines()
    {
        var d = Shop();
        d.Relationships[0].Label = "Browses the catalogue, fills a basket, pays for it and then tracks the order until it arrives";

        var rows = Relationships(Parse(d)).First(g => (string)g.Attribute("data-id")! == "r1").Elements(Svg + "text").Select(t => t.Value).ToList();

        Assert.Equal(3, rows.Count);
        Assert.All(rows, row => Assert.True(row.Length <= TextWrap.LabelMaxChars, row));
        Assert.EndsWith("…", rows[^1]);
    }

    [Fact]
    public void The_system_has_a_heavier_outline()
    {
        var sys = Elements(Parse(Shop())).First(g => (string)g.Attribute("data-type")! == "system");
        Assert.Equal("3", (string?)sys.Element(Svg + "rect")!.Attribute("stroke-width"));
    }

    [Fact]
    public void Two_way_relationships_have_arrows_at_both_ends()
    {
        var lines = Relationships(Parse(Shop())).ToDictionary(g => (string)g.Attribute("data-id")!, g => g.Element(Svg + "line")!);

        Assert.NotNull(lines["r3"].Attribute("marker-start"));
        Assert.NotNull(lines["r3"].Attribute("marker-end"));
        Assert.Null(lines["r1"].Attribute("marker-start"));
        Assert.NotNull(lines["r1"].Attribute("marker-end"));
    }

    [Fact]
    public void Arrow_marker_is_defined()
    {
        var root = Parse(Shop());
        Assert.Single(root.Descendants(Svg + "marker"), m => (string?)m.Attribute("id") == "arrow");
    }

    [Fact]
    public void Each_element_type_gets_its_own_look()
    {
        var byType = Elements(Parse(Shop())).GroupBy(g => (string)g.Attribute("data-type")!).ToDictionary(g => g.Key, g => g.First());

        Assert.Equal(2, byType["storage"].Descendants(Svg + "path").Count(p => ((string)p.Attribute("d")!).Contains('A')));
        Assert.Single(byType["secondaryActor"].Elements(Svg + "rect"), r => r.Attribute("stroke-dasharray") is not null);
        Assert.Null(byType["primaryActor"].Element(Svg + "rect")!.Attribute("stroke-dasharray"));
        Assert.NotNull(byType["primaryActor"].Element(Svg + "circle"));
        Assert.Null(byType["externalSystem"].Element(Svg + "circle"));
        Assert.Equal("0", (string?)byType["externalSystem"].Element(Svg + "rect")!.Attribute("rx"));
    }

    [Fact]
    public void ViewBox_encloses_every_element_with_a_margin()
    {
        var d = Shop();
        var parts = ((string)Parse(d).Attribute("viewBox")!).Split(' ').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        var view = new Rect(parts[0], parts[1], parts[2], parts[3]);

        foreach (var e in d.Elements)
        {
            var box = ElementShapes.BoundsOf(e);
            Assert.True(view.Left < box.Left && view.Top < box.Top && view.Right > box.Right && view.Bottom > box.Bottom, e.Name);
        }

        var content = d.Elements.Select(ElementShapes.BoundsOf).Aggregate(Rect.Union);
        Assert.Equal(40, content.Left - view.Left, 2);
        Assert.Equal(40, view.Bottom - content.Bottom, 2);
    }

    [Fact]
    public void Cropping_follows_the_content_not_the_origin()
    {
        var d = Shop();
        foreach (var e in d.Elements) { e.X += 5000; e.Y -= 3000; }

        var parts = ((string)Parse(d).Attribute("viewBox")!).Split(' ').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();

        Assert.True(parts[0] > 4000);
        Assert.True(parts[1] < -3000);
        Assert.True(parts[2] < 1200);
    }

    [Fact]
    public void Special_characters_are_escaped_and_survive_parsing()
    {
        var d = Shop();
        d.Elements[1].Name = "R&D <Team> \"A\"";
        d.Relationships[0].Label = "a & b";

        var text = Parse(d).Descendants(Svg + "text").Select(t => t.Value).ToList();

        Assert.Contains("R&D <Team> \"A\"", text);
        Assert.Contains("a & b", text);
    }

    [Fact]
    public void Long_names_are_wrapped_or_cut_instead_of_overflowing()
    {
        var d = Shop();
        d.Elements[1].Name = "A very long customer role name that cannot possibly fit in one box";

        var rows = Elements(Parse(d)).First(g => (string)g.Attribute("data-id")! == "cust").Elements(Svg + "text").Where(t => t.Attribute("fill-opacity") is null).Select(t => t.Value).ToList();

        Assert.True(rows.Count <= 2);
        Assert.EndsWith("…", rows[^1]);
    }

    [Fact]
    public void A_new_empty_diagram_exports()
    {
        var root = Parse(Diagram.CreateNew("Empty"));
        Assert.Single(Elements(root));
        Assert.Empty(Relationships(root));
    }

    [Fact]
    public void Relationship_without_a_label_draws_only_the_line()
    {
        var d = Diagram.CreateNew();
        d.AddElement(ElementType.Storage, "DB");
        AutoLayoutAll(d);

        var group = Relationships(Parse(d)).Single();

        Assert.Single(group.Elements());
    }

    [Fact]
    public void Export_is_deterministic()
    {
        Assert.Equal(SvgExporter.Export(Shop()), SvgExporter.Export(Shop()));
    }

    [Theory]
    [InlineData("one two three", 9, 2, "one two|three")]
    [InlineData("short", 20, 2, "short")]
    [InlineData("alpha beta gamma delta", 10, 2, "alpha beta|gamma…")]
    [InlineData("supercalifragilistic", 8, 2, "superca…")]
    public void Wrap_breaks_on_words_and_truncates(string text, int maxChars, int maxLines, string expected)
    {
        Assert.Equal(expected, string.Join("|", TextWrap.Wrap(text, maxChars, maxLines)));
    }

    private static void AutoLayoutAll(Diagram d) => Layout.AutoLayout.Apply(d, clearPins: true);
}
