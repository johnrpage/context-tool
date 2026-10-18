using ContextSketch.Core.Geometry;
using ContextSketch.Core.Model;

namespace ContextSketch.Core.Tests;

public class EdgeGeometryTests
{
    private static readonly Size Box = new(100, 50);

    [Fact]
    public void Horizontal_line_leaves_through_the_side()
    {
        var p = EdgeGeometry.Clip(new Point(0, 0), Box, new Point(300, 0));
        Assert.Equal(new Point(50, 0), p);
    }

    [Fact]
    public void Vertical_line_leaves_through_the_top()
    {
        var p = EdgeGeometry.Clip(new Point(0, 0), Box, new Point(0, -300));
        Assert.Equal(new Point(0, -25), p);
    }

    [Fact]
    public void Diagonal_line_leaves_on_the_boundary()
    {
        var p = EdgeGeometry.Clip(new Point(0, 0), Box, new Point(200, 200));
        Assert.Equal(25, p.X, 6);
        Assert.Equal(25, p.Y, 6);
    }

    [Fact]
    public void Same_point_returns_the_centre()
    {
        Assert.Equal(new Point(5, 5), EdgeGeometry.Clip(new Point(5, 5), Box, new Point(5, 5)));
    }

    [Fact]
    public void Relationship_line_runs_between_element_edges()
    {
        var a = new Element { Id = "a", Type = ElementType.System, X = 0, Y = 0 };
        var b = new Element { Id = "b", Type = ElementType.Storage, X = 500, Y = 0 };
        var (start, end) = EdgeGeometry.LineBetween(a, b);
        Assert.Equal(ElementShapes.SystemSize.Width / 2, start.X);
        Assert.Equal(500 - ElementShapes.DefaultSize.Width / 2, end.X);
    }

    [Fact]
    public void Rects_that_only_touch_do_not_intersect()
    {
        var a = new Rect(0, 0, 10, 10);
        Assert.False(a.Intersects(new Rect(10, 0, 10, 10)));
        Assert.True(a.Intersects(new Rect(9, 9, 10, 10)));
    }
}
