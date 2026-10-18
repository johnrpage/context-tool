namespace ContextSketch.Core.Model;

public sealed class Element
{
    public required string Id { get; set; }
    public required ElementType Type { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Technology { get; set; }

    /// <summary>Centre of the element, in diagram units.</summary>
    public double X { get; set; }
    public double Y { get; set; }

    /// <summary>True once the user has dragged the element; auto-layout leaves it alone.</summary>
    public bool Pinned { get; set; }
}
