using System.Text.Json;
using System.Text.Json.Serialization;
using ContextSketch.Core.Model;
using ContextSketch.Core.Validation;

namespace ContextSketch.Core.Serialization;

public sealed record LoadResult(Diagram? Diagram, IReadOnlyList<string> Errors, bool NeedsLayout)
{
    public bool Success => Diagram is not null;
}

public static class DiagramJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Save(Diagram diagram)
    {
        var file = new DiagramFile
        {
            SchemaVersion = diagram.SchemaVersion,
            Title = diagram.Title,
            Elements = diagram.Elements.Select(e => new ElementFile
            {
                Id = e.Id,
                Type = Camel(e.Type),
                Name = e.Name,
                Description = Blank(e.Description),
                Technology = Blank(e.Technology),
                X = Math.Round(e.X),
                Y = Math.Round(e.Y),
                Pinned = e.Pinned,
            }).ToList(),
            Relationships = diagram.Relationships.Select(r => new RelationshipFile
            {
                Id = r.Id,
                From = r.From,
                To = r.To,
                Label = r.Label,
                Technology = Blank(r.Technology),
                Direction = Camel(r.Direction),
            }).ToList(),
        };
        return JsonSerializer.Serialize(file, Options);
    }

    public static LoadResult Load(string json)
    {
        DiagramFile? file;
        try
        {
            file = JsonSerializer.Deserialize<DiagramFile>(json, Options);
        }
        catch (JsonException ex)
        {
            var where = ex.LineNumber is { } line ? $" (line {line + 1})" : "";
            return Fail($"The file is not valid diagram JSON{where}: {ex.Message}");
        }
        if (file is null) return Fail("The file is empty.");

        var errors = new List<string>();
        var diagram = new Diagram { SchemaVersion = file.SchemaVersion, Title = file.Title ?? "" };
        var needsLayout = false;

        foreach (var e in file.Elements ?? [])
        {
            if (!TryParse<ElementType>(e.Type, out var type))
            {
                errors.Add($"Element '{e.Id}' has unknown type '{e.Type}'.");
                continue;
            }
            if (e.X is null || e.Y is null) needsLayout = true;
            diagram.Elements.Add(new Element
            {
                Id = e.Id ?? "",
                Type = type,
                Name = e.Name ?? "",
                Description = e.Description,
                Technology = e.Technology,
                X = e.X ?? 0,
                Y = e.Y ?? 0,
                Pinned = e.Pinned == true && e.X is not null && e.Y is not null,
            });
        }

        foreach (var r in file.Relationships ?? [])
        {
            RelationshipDirection direction = RelationshipDirection.OneWay;
            if (r.Direction is not null && !TryParse(r.Direction, out direction))
            {
                errors.Add($"Relationship '{r.Id}' has unknown direction '{r.Direction}'.");
                continue;
            }
            diagram.Relationships.Add(new Relationship
            {
                Id = r.Id ?? "",
                From = r.From ?? "",
                To = r.To ?? "",
                Label = r.Label ?? "",
                Technology = r.Technology,
                Direction = direction,
            });
        }

        // Only validate structure once every item was understood, so errors aren't doubled up.
        errors.AddRange(DiagramValidator.Validate(diagram));
        return errors.Count > 0 ? new LoadResult(null, errors, false) : new LoadResult(diagram, [], needsLayout);
    }

    private static LoadResult Fail(string error) => new(null, [error], false);

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static string Camel<T>(T value) where T : struct, Enum
    {
        var name = value.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static bool TryParse<T>(string? text, out T value) where T : struct, Enum =>
        Enum.TryParse(text, ignoreCase: true, out value) && Enum.IsDefined(value);
}
