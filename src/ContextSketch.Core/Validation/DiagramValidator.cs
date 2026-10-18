using ContextSketch.Core.Model;

namespace ContextSketch.Core.Validation;

public static class DiagramValidator
{
    /// <summary>Returns every problem found; an empty list means the diagram is valid.</summary>
    public static List<string> Validate(Diagram diagram)
    {
        var errors = new List<string>();

        if (diagram.SchemaVersion != Diagram.CurrentSchemaVersion)
            errors.Add($"Unsupported schema version {diagram.SchemaVersion} (this app reads version {Diagram.CurrentSchemaVersion}).");

        var systems = diagram.Elements.Where(e => e.Type == ElementType.System).ToList();
        if (systems.Count == 0) errors.Add("The diagram has no system element.");
        else if (systems.Count > 1) errors.Add($"The diagram has {systems.Count} system elements; exactly one is required.");

        var ids = new HashSet<string>();
        foreach (var id in diagram.Elements.Select(e => e.Id).Concat(diagram.Relationships.Select(r => r.Id)))
        {
            if (string.IsNullOrWhiteSpace(id)) errors.Add("An element or relationship has no id.");
            else if (!ids.Add(id)) errors.Add($"Duplicate id '{id}'.");
        }

        foreach (var e in diagram.Elements.Where(e => string.IsNullOrWhiteSpace(e.Name)))
            errors.Add($"Element '{e.Id}' has no name.");

        var elementIds = diagram.Elements.Select(e => e.Id).ToHashSet();
        var systemId = systems.Count == 1 ? systems[0].Id : null;
        var linkCount = new Dictionary<string, int>();

        foreach (var r in diagram.Relationships)
        {
            var fromOk = elementIds.Contains(r.From);
            var toOk = elementIds.Contains(r.To);
            if (!fromOk) errors.Add($"Relationship '{r.Id}' refers to unknown element '{r.From}' (from).");
            if (!toOk) errors.Add($"Relationship '{r.Id}' refers to unknown element '{r.To}' (to).");
            if (!fromOk || !toOk) continue;

            if (r.From == r.To)
            {
                errors.Add($"Relationship '{r.Id}' connects element '{r.From}' to itself.");
                continue;
            }
            if (systemId is not null && r.From != systemId && r.To != systemId)
                errors.Add($"Relationship '{r.Id}' must have the system as one end.");

            foreach (var end in new[] { r.From, r.To })
                linkCount[end] = linkCount.GetValueOrDefault(end) + 1;
        }

        foreach (var e in diagram.Elements.Where(e => e.Type != ElementType.System))
        {
            var count = linkCount.GetValueOrDefault(e.Id);
            if (count != 1)
                errors.Add($"Element '{e.Id}' ({e.Name}) must have exactly one relationship to the system but has {count}.");
        }

        return errors;
    }
}
