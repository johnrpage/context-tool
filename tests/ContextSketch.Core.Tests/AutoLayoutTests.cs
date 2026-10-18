using ContextSketch.Core.Geometry;
using ContextSketch.Core.Layout;
using ContextSketch.Core.Model;

namespace ContextSketch.Core.Tests;

public class AutoLayoutTests
{
    private static Diagram Build(int primary = 0, int secondary = 0, int external = 0, int storage = 0)
    {
        var d = Diagram.CreateNew();
        void Add(ElementType t, int n)
        {
            for (var i = 0; i < n; i++) d.AddElement(t, t.ToString() + " " + i);
        }
        Add(ElementType.PrimaryActor, primary);
        Add(ElementType.SecondaryActor, secondary);
        Add(ElementType.ExternalSystem, external);
        Add(ElementType.Storage, storage);
        return d;
    }

    private static List<Element> Of(Diagram d, ElementType t) => d.Elements.Where(e => e.Type == t).ToList();

    private static void AssertNoOverlaps(Diagram d)
    {
        var rects = d.Elements.Select(e => (e.Name, Rect: ElementShapes.BoundsOf(e))).ToList();
        for (var i = 0; i < rects.Count; i++)
            for (var j = i + 1; j < rects.Count; j++)
                Assert.False(rects[i].Rect.Intersects(rects[j].Rect), rects[i].Name + " overlaps " + rects[j].Name);
    }

    [Fact]
    public void System_is_placed_at_the_origin()
    {
        var d = Build();
        AutoLayout.Apply(d);
        Assert.Equal(0, d.System.X);
        Assert.Equal(0, d.System.Y);
    }

    [Fact]
    public void Primary_actors_are_left_and_everything_else_is_right()
    {
        var d = Build(3, 2, 2, 2);
        AutoLayout.Apply(d);

        var systemLeft = ElementShapes.BoundsOf(d.System).Left;
        var systemRight = ElementShapes.BoundsOf(d.System).Right;
        Assert.All(Of(d, ElementType.PrimaryActor), e => Assert.True(ElementShapes.BoundsOf(e).Right < systemLeft));
        foreach (var t in new[] { ElementType.SecondaryActor, ElementType.ExternalSystem, ElementType.Storage })
            Assert.All(Of(d, t), e => Assert.True(ElementShapes.BoundsOf(e).Left > systemRight));
    }

    [Fact]
    public void Right_side_groups_are_ordered_top_to_bottom()
    {
        var d = Build(secondary: 3, external: 3, storage: 3);
        AutoLayout.Apply(d);

        var secondary = Of(d, ElementType.SecondaryActor).Max(e => e.Y);
        var external = Of(d, ElementType.ExternalSystem);
        var storage = Of(d, ElementType.Storage).Min(e => e.Y);
        Assert.True(secondary < external.Min(e => e.Y));
        Assert.True(external.Max(e => e.Y) < storage);
    }

    [Fact]
    public void Primary_column_is_centred_on_the_system()
    {
        var d = Build(primary: 4);
        AutoLayout.Apply(d);
        Assert.Equal(0, Of(d, ElementType.PrimaryActor).Average(e => e.Y), 6);
        Assert.Single(Of(d, ElementType.PrimaryActor).Select(e => e.X).Distinct());
    }

    [Fact]
    public void Stacked_groups_are_centred_on_the_system()
    {
        var d = Build(secondary: 1, external: 1, storage: 1);
        AutoLayout.Apply(d);
        Assert.Equal(0, Of(d, ElementType.ExternalSystem)[0].Y, 6);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 1, 1, 1)]
    [InlineData(8, 0, 0, 0)]
    [InlineData(0, 7, 6, 12)]
    [InlineData(9, 11, 3, 14)]
    public void Nothing_overlaps(int primary, int secondary, int external, int storage)
    {
        var d = Build(primary, secondary, external, storage);
        AutoLayout.Apply(d);
        AssertNoOverlaps(d);
    }

    [Fact]
    public void Overflowing_group_wraps_into_a_second_column_further_right()
    {
        var options = new LayoutOptions { MaxPerColumn = 3 };
        var d = Build(storage: 4);
        AutoLayout.Apply(d, options: options);

        var xs = Of(d, ElementType.Storage).Select(e => e.X).Distinct().OrderBy(x => x).ToList();
        Assert.Equal(2, xs.Count);
        Assert.Equal(3, Of(d, ElementType.Storage).Count(e => e.X == xs[0]));
        AssertNoOverlaps(d);
    }

    [Fact]
    public void Many_primary_actors_stay_in_one_column()
    {
        var d = Build(primary: 12);
        AutoLayout.Apply(d);
        Assert.Single(Of(d, ElementType.PrimaryActor).Select(e => e.X).Distinct());
    }

    [Fact]
    public void Pinned_elements_are_not_moved()
    {
        var d = Build(2, 2, 1, 1);
        var pinned = Of(d, ElementType.ExternalSystem)[0];
        pinned.X = -40; pinned.Y = 300; pinned.Pinned = true;

        AutoLayout.Apply(d);

        Assert.Equal(-40, pinned.X);
        Assert.Equal(300, pinned.Y);
        Assert.DoesNotContain(pinned.Id, AutoLayout.Compute(d).Keys);
    }

    [Fact]
    public void Unpinned_elements_avoid_pinned_ones()
    {
        var d = Build(primary: 3, secondary: 2);
        // Drop a pinned storage right where the middle primary actor would go.
        var (blocker, _) = d.AddElement(ElementType.Storage, "Blocker");
        var slot = AutoLayout.Compute(d)[Of(d, ElementType.PrimaryActor)[1].Id];
        blocker.X = slot.X; blocker.Y = slot.Y; blocker.Pinned = true;

        AutoLayout.Apply(d);

        AssertNoOverlaps(d);
        Assert.Equal(slot.X, blocker.X);
    }

    [Fact]
    public void Pinned_system_is_the_anchor_for_the_zones()
    {
        var d = Build(primary: 1, storage: 1);
        d.System.X = 1000; d.System.Y = 500; d.System.Pinned = true;

        AutoLayout.Apply(d);

        Assert.Equal(1000, d.System.X);
        Assert.True(Of(d, ElementType.PrimaryActor)[0].X < 1000);
        Assert.True(Of(d, ElementType.Storage)[0].X > 1000);
        Assert.Equal(500, Of(d, ElementType.PrimaryActor)[0].Y, 6);
        AssertNoOverlaps(d);
    }

    [Fact]
    public void Layout_is_deterministic()
    {
        var d = Build(4, 3, 2, 5);
        var first = AutoLayout.Compute(d);
        var second = AutoLayout.Compute(d);
        Assert.Equal(first.OrderBy(p => p.Key), second.OrderBy(p => p.Key));
    }

    [Fact]
    public void Order_within_a_group_follows_creation_order()
    {
        var d = Build(primary: 4);
        AutoLayout.Apply(d);
        var ys = Of(d, ElementType.PrimaryActor).Select(e => e.Y).ToList();
        Assert.Equal(ys.OrderBy(y => y), ys);
    }

    [Fact]
    public void Clearing_pins_lays_everything_out_again()
    {
        var d = Build(primary: 2);
        foreach (var e in Of(d, ElementType.PrimaryActor)) { e.X = 5; e.Y = 5; e.Pinned = true; }

        AutoLayout.Apply(d, clearPins: true);

        Assert.All(d.Elements, e => Assert.False(e.Pinned));
        Assert.All(Of(d, ElementType.PrimaryActor), e => Assert.True(e.X < 0));
        AssertNoOverlaps(d);
    }
}
