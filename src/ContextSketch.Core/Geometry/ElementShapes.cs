using ContextSketch.Core.Model;

namespace ContextSketch.Core.Geometry;

public static class ElementShapes
{
    public static readonly Size SystemSize = new(240, 130);
    public static readonly Size DefaultSize = new(170, 90);

    public static Size SizeOf(ElementType type) => type == ElementType.System ? SystemSize : DefaultSize;

    public static Rect BoundsOf(Element element) =>
        Rect.Around(new Point(element.X, element.Y), SizeOf(element.Type));
}
