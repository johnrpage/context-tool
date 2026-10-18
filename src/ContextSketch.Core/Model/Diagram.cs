namespace ContextSketch.Core.Model;

public sealed class Diagram
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public string Title { get; set; } = "";
    public List<Element> Elements { get; } = [];
    public List<Relationship> Relationships { get; } = [];

    public Element System => Elements.Single(e => e.Type == ElementType.System);

    public static Diagram CreateNew(string title = "Untitled")
    {
        var diagram = new Diagram { Title = title };
        diagram.Elements.Add(new Element { Id = NewId(), Type = ElementType.System, Name = "My System" });
        return diagram;
    }

    /// <summary>
    /// Adds an element together with its one relationship to the system.
    /// Primary actors point at the system; everything else is pointed at by it.
    /// </summary>
    public (Element Element, Relationship Relationship) AddElement(ElementType type, string name)
    {
        if (type == ElementType.System)
            throw new ArgumentException("A diagram has exactly one system element.", nameof(type));

        var element = new Element { Id = NewId(), Type = type, Name = name };
        var system = System;
        var incoming = type == ElementType.PrimaryActor;
        var relationship = new Relationship
        {
            Id = NewId(),
            From = incoming ? element.Id : system.Id,
            To = incoming ? system.Id : element.Id,
        };
        Elements.Add(element);
        Relationships.Add(relationship);
        return (element, relationship);
    }

    /// <summary>Removes an element and its relationship. The system cannot be removed.</summary>
    public bool RemoveElement(string id)
    {
        var element = Elements.Find(e => e.Id == id);
        if (element is null || element.Type == ElementType.System)
            return false;

        Relationships.RemoveAll(r => r.From == id || r.To == id);
        Elements.Remove(element);
        return true;
    }

    public Relationship? RelationshipOf(string elementId) =>
        Relationships.Find(r => r.From == elementId || r.To == elementId);

    private static string NewId() => Guid.NewGuid().ToString("N")[..8];
}
