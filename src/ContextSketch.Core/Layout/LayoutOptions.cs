namespace ContextSketch.Core.Layout;

public sealed record LayoutOptions
{
    /// <summary>Space between the edge of the system and the nearest column.</summary>
    public double ZoneGap { get; init; } = 140;
    /// <summary>The zone gap grows, up to this, when a relationship label would otherwise be cut short.</summary>
    public double MaxZoneGap { get; init; } = 300;
    public double ColumnGap { get; init; } = 60;
    public double RowGap { get; init; } = 40;
    /// <summary>Extra space between the secondary / external / storage groups.</summary>
    public double GroupGap { get; init; } = 50;
    public int MaxPerColumn { get; init; } = 5;
    /// <summary>Clearance kept around pinned elements and already-placed ones.</summary>
    public double Padding { get; init; } = 20;
}
