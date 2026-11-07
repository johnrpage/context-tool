using ContextSketch.Core.Model;
using ContextSketch.Core.Serialization;

namespace ContextSketch.Core.Tests;

public class DiagramJsonTests
{
    private static Diagram LoadOk(string json)
    {
        var result = DiagramJson.Load(json);
        Assert.True(result.Success, string.Join("; ", result.Errors));
        return result.Diagram!;
    }

    [Fact]
    public void Spec_example_loads()
    {
        var d = LoadOk(SampleDiagrams.Shop);
        Assert.Equal("Online Shop", d.Title);
        Assert.Equal(6, d.Elements.Count);
        Assert.Equal(5, d.Relationships.Count);
        Assert.Equal(RelationshipDirection.TwoWay, d.Relationships[2].Direction);
        Assert.Equal(-350, d.Elements[1].X);
    }

    [Fact]
    public void Save_load_save_is_stable()
    {
        var first = DiagramJson.Save(LoadOk(SampleDiagrams.Shop));
        var second = DiagramJson.Save(LoadOk(first));
        Assert.Equal(first, second);
    }

    [Fact]
    public void Round_trip_preserves_every_field()
    {
        var d = Diagram.CreateNew("Round trip");
        var (e, r) = d.AddElement(ElementType.Storage, "Orders DB");
        e.Description = "All orders";
        e.Technology = "PostgreSQL";
        e.X = 120; e.Y = -40; e.Pinned = true;
        r.Label = "Reads/writes"; r.Direction = RelationshipDirection.TwoWay;

        var loaded = LoadOk(DiagramJson.Save(d));

        Assert.Equal(d.Title, loaded.Title);
        Assert.Equal(d.Elements.Select(x => (x.Id, x.Type, x.Name, x.Description, x.Technology, x.X, x.Y, x.Pinned)),
                     loaded.Elements.Select(x => (x.Id, x.Type, x.Name, x.Description, x.Technology, x.X, x.Y, x.Pinned)));
        Assert.Equal(d.Relationships.Select(x => (x.Id, x.From, x.To, x.Label, x.Direction)),
                     loaded.Relationships.Select(x => (x.Id, x.From, x.To, x.Label, x.Direction)));
    }

    [Fact]
    public void Output_is_camel_case_indented_and_omits_empty_optionals()
    {
        var json = DiagramJson.Save(Diagram.CreateNew("T"));
        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.Contains("\"type\": \"system\"", json);
        Assert.Contains("\n  ", json);
        Assert.DoesNotContain("description", json);
        Assert.DoesNotContain("technology", json);
    }

    [Fact]
    public void Technology_on_a_relationship_in_an_older_file_is_ignored_and_not_saved()
    {
        var json = """
        { "schemaVersion": 1, "title": "t", "elements": [
          { "id": "s", "type": "system", "name": "S", "x": 0, "y": 0 },
          { "id": "u", "type": "primaryActor", "name": "User", "technology": "WPF", "x": -350, "y": 0 } ],
          "relationships": [ { "id": "r", "from": "u", "to": "s", "label": "Uses", "technology": "HTTPS", "direction": "oneWay" } ] }
        """;

        var saved = DiagramJson.Save(LoadOk(json));

        Assert.Single(System.Text.RegularExpressions.Regex.Matches(saved, "\"technology\""));
        Assert.Contains("\"technology\": \"WPF\"", saved);
        Assert.DoesNotContain("HTTPS", saved);
    }

    [Fact]
    public void Missing_positions_request_layout()
    {
        var json = """
        { "schemaVersion": 1, "title": "t", "elements": [
          { "id": "s", "type": "system", "name": "S" } ], "relationships": [] }
        """;
        var result = DiagramJson.Load(json);
        Assert.True(result.Success);
        Assert.True(result.NeedsLayout);
    }

    [Fact]
    public void Complete_positions_do_not_request_layout()
    {
        Assert.False(DiagramJson.Load(SampleDiagrams.Shop).NeedsLayout);
    }

    [Fact]
    public void Pinned_without_position_is_ignored()
    {
        var json = """
        { "schemaVersion": 1, "title": "t", "elements": [
          { "id": "s", "type": "system", "name": "S", "pinned": true } ], "relationships": [] }
        """;
        var result = DiagramJson.Load(json);
        Assert.True(result.NeedsLayout);
        Assert.False(result.Diagram!.System.Pinned);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{ \"schemaVersion\": ")]
    [InlineData("null")]
    public void Malformed_input_returns_an_error_instead_of_throwing(string json)
    {
        var result = DiagramJson.Load(json);
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Unknown_element_type_is_reported_by_name()
    {
        var result = DiagramJson.Load(SampleDiagrams.Shop.Replace("\"storage\"", "\"cloud\""));
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("'cloud'") && e.Contains("'db'"));
    }

    [Fact]
    public void Unknown_direction_is_reported()
    {
        var result = DiagramJson.Load(SampleDiagrams.Shop.Replace("\"twoWay\"", "\"sideways\""));
        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("'sideways'"));
    }
}
