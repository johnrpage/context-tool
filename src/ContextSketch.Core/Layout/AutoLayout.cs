using ContextSketch.Core.Geometry;
using ContextSketch.Core.Model;

namespace ContextSketch.Core.Layout;

public static class AutoLayout
{
    private static readonly ElementType[] RightGroups =
        [ElementType.SecondaryActor, ElementType.ExternalSystem, ElementType.Storage];

    /// <summary>
    /// Pure layout: returns centre positions for every unpinned element. Pinned elements are
    /// never moved and are treated as obstacles. Deterministic for a given diagram.
    /// </summary>
    public static Dictionary<string, Point> Compute(Diagram diagram, LayoutOptions? options = null)
    {
        options ??= new LayoutOptions();
        var result = new Dictionary<string, Point>();

        var system = diagram.System;
        var anchor = system.Pinned ? new Point(system.X, system.Y) : new Point(0, 0);
        if (!system.Pinned) result[system.Id] = anchor;

        // Everything that must not be overlapped: the system and all pinned elements.
        var occupied = new List<Rect> { Rect.Around(anchor, ElementShapes.SystemSize) };
        occupied.AddRange(diagram.Elements.Where(e => e.Pinned && e.Type != ElementType.System).Select(ElementShapes.BoundsOf));

        var sysW = ElementShapes.SystemSize.Width;
        var size = ElementShapes.DefaultSize;
        var pitch = size.Height + options.RowGap;

        // Left zone: one tall column of primary actors, centred on the y of the system.
        var primaries = Unpinned(diagram, ElementType.PrimaryActor);
        var leftX = anchor.X - sysW / 2 - GapFor(diagram, primaries, options) - size.Width / 2;
        for (var i = 0; i < primaries.Count; i++)
        {
            var ideal = new Point(leftX, anchor.Y + (i - (primaries.Count - 1) / 2.0) * pitch);
            Place(primaries[i], ideal, size, pitch, options, occupied, result);
        }

        // Right zone: secondary actors, then external systems, then storage, top to bottom.
        var groups = RightGroups.Select(t => Unpinned(diagram, t)).Where(g => g.Count > 0).ToList();
        var rows = groups.Select(g => Math.Min(g.Count, options.MaxPerColumn)).ToList();
        var totalHeight = rows.Sum(r => r * size.Height + (r - 1) * options.RowGap)
                          + (groups.Count - 1) * (options.RowGap + options.GroupGap);
        var top = anchor.Y - totalHeight / 2;
        var firstColumnX = anchor.X + sysW / 2 + GapFor(diagram, groups.SelectMany(g => g), options) + size.Width / 2;

        for (var g = 0; g < groups.Count; g++)
        {
            for (var i = 0; i < groups[g].Count; i++)
            {
                var column = i / options.MaxPerColumn;
                var row = i % options.MaxPerColumn;
                var ideal = new Point(
                    firstColumnX + column * (size.Width + options.ColumnGap),
                    top + size.Height / 2 + row * pitch);
                Place(groups[g][i], ideal, size, pitch, options, occupied, result);
            }
            top += rows[g] * size.Height + (rows[g] - 1) * options.RowGap + options.RowGap + options.GroupGap;
        }

        return result;
    }

    /// <summary>Applies <see cref="Compute"/> to the diagram. With <paramref name="clearPins"/>, every element is laid out again.</summary>
    public static void Apply(Diagram diagram, bool clearPins = false, LayoutOptions? options = null)
    {
        if (clearPins)
            foreach (var e in diagram.Elements) e.Pinned = false;

        foreach (var (id, p) in Compute(diagram, options))
        {
            var element = diagram.Elements.First(e => e.Id == id);
            element.X = p.X;
            element.Y = p.Y;
        }
    }

    /// <summary>
    /// The gap between the system and a zone: the usual one, widened (up to a cap) so the longest label in the
    /// zone fits without being cut. The horizontal gap is the shortest any of the zone's arrows can be.
    /// </summary>
    private static double GapFor(Diagram diagram, IEnumerable<Element> zone, LayoutOptions options) =>
        zone.Select(e => TextWrap.ArrowLengthFor(diagram.RelationshipOf(e.Id)?.Label, options.ZoneGap, options.MaxZoneGap))
            .DefaultIfEmpty(options.ZoneGap)
            .Max();

    private static List<Element> Unpinned(Diagram diagram, ElementType type) =>
        diagram.Elements.Where(e => e.Type == type && !e.Pinned).ToList();

    /// <summary>Puts the element at its ideal slot, nudging it down past anything already in the way.</summary>
    private static void Place(Element element, Point ideal, Size size, double step, LayoutOptions options,
        List<Rect> occupied, Dictionary<string, Point> result)
    {
        var position = ideal;
        var clearance = options.Padding / 2;
        while (occupied.Any(o => o.Inflate(clearance).Intersects(Rect.Around(position, size).Inflate(clearance))))
            position = position with { Y = position.Y + step };

        occupied.Add(Rect.Around(position, size));
        result[element.Id] = position;
    }
}
