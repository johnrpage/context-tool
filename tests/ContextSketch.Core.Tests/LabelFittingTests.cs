using ContextSketch.Core.Layout;
using ContextSketch.Core.Model;

namespace ContextSketch.Core.Tests;

public class LabelFittingTests
{
    private const string Long = "Browses the catalogue, fills a basket, pays for it and then tracks the order";

    [Fact]
    public void Longer_arrows_get_wider_label_lines_within_limits()
    {
        Assert.Equal(TextWrap.LabelMinChars, TextWrap.LabelCharsFor(10));
        Assert.True(TextWrap.LabelCharsFor(300) > TextWrap.LabelCharsFor(140));
        Assert.Equal(TextWrap.LabelMaxChars, TextWrap.LabelCharsFor(5000));
    }

    [Fact]
    public void A_short_label_on_a_long_arrow_stays_on_one_line()
    {
        Assert.Equal(["Imports context diagram"], TextWrap.LabelLines("Imports context diagram", 270));
    }

    [Fact]
    public void The_same_label_wraps_on_a_short_arrow()
    {
        Assert.True(TextWrap.LabelLines("Imports context diagram", 140).Count > 1);
    }

    [Fact]
    public void Arrow_length_grows_until_the_label_is_not_cut()
    {
        var length = TextWrap.ArrowLengthFor(Long, 140, 300);

        Assert.InRange(length, 141, 299);
        Assert.DoesNotContain("…", string.Concat(TextWrap.LabelLines(Long, length)));
        Assert.Contains("…", string.Concat(TextWrap.LabelLines(Long, length - 10)));
    }

    [Fact]
    public void Arrow_length_stops_at_the_cap()
    {
        Assert.Equal(300, TextWrap.ArrowLengthFor(string.Join(' ', Enumerable.Repeat(Long, 4)), 140, 300));
        Assert.Equal(140, TextWrap.ArrowLengthFor("Short", 140, 300));
        Assert.Equal(140, TextWrap.ArrowLengthFor(null, 140, 300));
    }

    [Fact]
    public void Layout_keeps_the_usual_gap_for_short_labels()
    {
        var d = Diagram.CreateNew();
        var (actor, r) = d.AddElement(ElementType.PrimaryActor, "Customer");
        r.Label = "Places orders";
        AutoLayout.Apply(d);

        Assert.Equal(-(120 + 140 + 85), actor.X, 6);
    }

    [Fact]
    public void Layout_widens_the_gap_so_a_long_label_is_not_cut()
    {
        var d = Diagram.CreateNew();
        var (actor, r) = d.AddElement(ElementType.PrimaryActor, "Customer");
        var (db, s) = d.AddElement(ElementType.Storage, "Orders");
        r.Label = Long;
        s.Label = Long;
        AutoLayout.Apply(d);

        var gap = -actor.X - 120 - 85;
        Assert.InRange(gap, 141, 300);
        Assert.Equal(gap, db.X - 120 - 85, 6);

        var (start, end) = Geometry.EdgeGeometry.LineBetween(actor, d.System);
        Assert.DoesNotContain("…", string.Concat(TextWrap.LabelLines(Long, start, end)));
    }

    [Fact]
    public void Layout_never_widens_the_gap_past_the_cap()
    {
        var d = Diagram.CreateNew();
        var (actor, r) = d.AddElement(ElementType.PrimaryActor, "Customer");
        r.Label = string.Join(' ', Enumerable.Repeat(Long, 4));
        AutoLayout.Apply(d);

        Assert.Equal(-(120 + 300 + 85), actor.X, 6);
    }
}
