using ContextSketch.Core.Model;

namespace ContextSketch.Core.Geometry;

public static class EdgeGeometry
{
    /// <summary>Where the line from a rectangle centre towards <paramref name="toward"/> leaves the rectangle.</summary>
    public static Point Clip(Point center, Size size, Point toward)
    {
        var dx = toward.X - center.X;
        var dy = toward.Y - center.Y;
        if (dx == 0 && dy == 0) return center;

        var scaleX = dx == 0 ? double.PositiveInfinity : size.Width / 2 / Math.Abs(dx);
        var scaleY = dy == 0 ? double.PositiveInfinity : size.Height / 2 / Math.Abs(dy);
        var scale = Math.Min(scaleX, scaleY);
        return new Point(center.X + dx * scale, center.Y + dy * scale);
    }

    /// <summary>The visible line for a relationship, clipped to both element edges.</summary>
    public static (Point Start, Point End) LineBetween(Element from, Element to)
    {
        var a = new Point(from.X, from.Y);
        var b = new Point(to.X, to.Y);
        return (Clip(a, ElementShapes.SizeOf(from.Type), b), Clip(b, ElementShapes.SizeOf(to.Type), a));
    }

    public static Point Midpoint(Point a, Point b) => new((a.X + b.X) / 2, (a.Y + b.Y) / 2);
}
