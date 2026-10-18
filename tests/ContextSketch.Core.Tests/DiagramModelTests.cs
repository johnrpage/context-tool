using ContextSketch.Core.Model;

namespace ContextSketch.Core.Tests;

public class DiagramModelTests
{
    [Fact]
    public void New_diagram_has_one_system_and_nothing_else()
    {
        var d = Diagram.CreateNew();
        Assert.Single(d.Elements);
        Assert.Equal(ElementType.System, d.Elements[0].Type);
        Assert.Empty(d.Relationships);
    }

    [Theory]
    [InlineData(ElementType.PrimaryActor, true)]
    [InlineData(ElementType.SecondaryActor, false)]
    [InlineData(ElementType.ExternalSystem, false)]
    [InlineData(ElementType.Storage, false)]
    public void Adding_an_element_creates_its_system_relationship(ElementType type, bool pointsAtSystem)
    {
        var d = Diagram.CreateNew();
        var (element, rel) = d.AddElement(type, "X");

        Assert.Single(d.Relationships);
        Assert.Equal(pointsAtSystem, rel.To == d.System.Id);
        Assert.Equal(pointsAtSystem ? element.Id : d.System.Id, rel.From);
    }

    [Fact]
    public void Cannot_add_a_second_system()
    {
        Assert.Throws<ArgumentException>(() => Diagram.CreateNew().AddElement(ElementType.System, "Another"));
    }

    [Fact]
    public void Removing_an_element_removes_its_relationship()
    {
        var d = Diagram.CreateNew();
        var (a, _) = d.AddElement(ElementType.Storage, "A");
        d.AddElement(ElementType.Storage, "B");

        Assert.True(d.RemoveElement(a.Id));
        Assert.Equal(2, d.Elements.Count);
        Assert.Single(d.Relationships);
    }

    [Fact]
    public void System_cannot_be_removed()
    {
        var d = Diagram.CreateNew();
        Assert.False(d.RemoveElement(d.System.Id));
        Assert.Single(d.Elements);
    }
}
