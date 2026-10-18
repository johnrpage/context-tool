namespace ContextSketch.Core.Geometry;

public readonly record struct Point(double X, double Y);

public readonly record struct Size(double Width, double Height);

public readonly record struct Rect(double Left, double Top, double Width, double Height)
{
    public double Right => Left + Width;
    public double Bottom => Top + Height;
    public Point Center => new(Left + Width / 2, Top + Height / 2);

    public static Rect Around(Point center, Size size) =>
        new(center.X - size.Width / 2, center.Y - size.Height / 2, size.Width, size.Height);

    public Rect Inflate(double amount) =>
        new(Left - amount, Top - amount, Width + 2 * amount, Height + 2 * amount);

    /// <summary>True when the interiors overlap; rectangles that merely touch do not intersect.</summary>
    public bool Intersects(Rect other) =>
        Left < other.Right && other.Left < Right && Top < other.Bottom && other.Top < Bottom;

    public static Rect Union(Rect a, Rect b)
    {
        var left = Math.Min(a.Left, b.Left);
        var top = Math.Min(a.Top, b.Top);
        return new Rect(left, top, Math.Max(a.Right, b.Right) - left, Math.Max(a.Bottom, b.Bottom) - top);
    }
}
