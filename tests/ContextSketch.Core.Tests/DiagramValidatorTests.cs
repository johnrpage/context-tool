using ContextSketch.Core.Model;
using ContextSketch.Core.Serialization;
using ContextSketch.Core.Validation;

namespace ContextSketch.Core.Tests;

public class DiagramValidatorTests
{
    private static Diagram Valid() => DiagramJson.Load(SampleDiagrams.Shop).Diagram!;

    [Fact]
    public void Valid_diagram_has_no_errors() => Assert.Empty(DiagramValidator.Validate(Valid()));

    [Fact]
    public void Unknown_schema_version()
    {
        var d = Valid(); d.SchemaVersion = 7;
        Assert.Contains(DiagramValidator.Validate(d), e => e.Contains("schema version 7"));
    }

    [Fact]
    public void Missing_system()
    {
        var d = Valid(); d.Elements.RemoveAll(e => e.Type == ElementType.System);
        Assert.Contains(DiagramValidator.Validate(d), e => e.Contains("no system"));
    }

    [Fact]
    public void Multiple_systems()
    {
        var d = Valid(); d.Elements.Add(new Element { Id = "sys2", Type = ElementType.System, Name = "Two" });
        Assert.Contains(DiagramValidator.Validate(d), e => e.Contains("2 system"));
    }

    [Fact]
    public void Duplicate_element_ids()
    {
        var d = Valid(); d.Elements[1].Id = "sys";
        Assert.Contains(DiagramValidator.Validate(d), e => e.Contains("Duplicate id 'sys'"));
    }

    [Fact]
    public void Element_and_relationship_ids_share_one_namespace()
    {
        var d = Valid(); d.Relationships[0].Id = "db";
        Assert.Contains(DiagramValidator.Validate(d), e => e.Contains("Duplicate id 'db'"));
    }

    [Fact]
    public void Dangling_reference()
    {
        var d = Valid(); d.Relationships[0].From = "ghost";
        Assert.Contains(DiagramValidator.Validate(d), e => e.Contains("unknown element 'ghost'"));
    }

    [Fact]
    public void Self_relationship()
    {
        var d = Valid(); d.Relationships[0].From = d.Relationships[0].To;
        Assert.Contains(DiagramValidator.Validate(d), e => e.Contains("to itself"));
    }

    [Fact]
    public void Relationship_between_two_non_system_elements()
    {
        var d = Valid(); d.Relationships[0].To = "admin"; // cust -> admin
        Assert.Contains(DiagramValidator.Validate(d), e => e.Contains("system as one end"));
    }

    [Fact]
    public void Element_without_relationship()
    {
        var d = Valid(); d.Relationships.RemoveAll(r => r.Id == "r5");
        Assert.Contains(DiagramValidator.Validate(d), e => e.Contains("'db'") && e.Contains("has 0"));
    }

    [Fact]
    public void Element_with_two_relationships()
    {
        var d = Valid();
        d.Relationships.Add(new Relationship { Id = "extra", From = "sys", To = "db" });
        Assert.Contains(DiagramValidator.Validate(d), e => e.Contains("'db'") && e.Contains("has 2"));
    }

    [Fact]
    public void Empty_name()
    {
        var d = Valid(); d.Elements[1].Name = "  ";
        Assert.Contains(DiagramValidator.Validate(d), e => e.Contains("has no name"));
    }

    [Fact]
    public void Reports_all_problems_not_just_the_first()
    {
        var d = Valid(); d.SchemaVersion = 2; d.Elements[1].Name = ""; d.Relationships[0].From = "ghost";
        Assert.True(DiagramValidator.Validate(d).Count >= 3);
    }
}
