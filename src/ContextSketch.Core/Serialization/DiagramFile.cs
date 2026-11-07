namespace ContextSketch.Core.Serialization;

// Property order here is the order written to disk.

internal sealed class DiagramFile
{
    public int SchemaVersion { get; set; }
    public string? Title { get; set; }
    public List<ElementFile>? Elements { get; set; }
    public List<RelationshipFile>? Relationships { get; set; }
}

internal sealed class ElementFile
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Technology { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public bool? Pinned { get; set; }
}

internal sealed class RelationshipFile
{
    public string? Id { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
    public string? Label { get; set; }
    public string? Direction { get; set; }
}
