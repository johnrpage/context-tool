namespace ContextSketch.Core.Model;

public sealed class Relationship
{
    public required string Id { get; set; }
    public required string From { get; set; }
    public required string To { get; set; }
    public string Label { get; set; } = "";
    public RelationshipDirection Direction { get; set; } = RelationshipDirection.OneWay;
}
