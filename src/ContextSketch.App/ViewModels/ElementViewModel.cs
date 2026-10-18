using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using ContextSketch.Core.Geometry;
using ContextSketch.Core.Model;

namespace ContextSketch.App.ViewModels;

public partial class ElementViewModel : ObservableObject
{
    private readonly Action _changed;

    public ElementViewModel(Element model, Action changed)
    {
        Model = model;
        _changed = changed;
        var size = ElementShapes.SizeOf(model.Type);
        Width = size.Width;
        Height = size.Height;
    }

    public Element Model { get; }
    public string Id => Model.Id;
    public ElementType Type => Model.Type;

    public string TypeName => Type switch
    {
        ElementType.System => "System",
        ElementType.PrimaryActor => "Primary actor",
        ElementType.SecondaryActor => "Secondary actor",
        ElementType.ExternalSystem => "External system",
        ElementType.Storage => "Storage",
        _ => "Element",
    };

    public double Width { get; }
    public double Height { get; }

    [ObservableProperty] private bool _isSelected;

    public string Name
    {
        get => Model.Name;
        set => Set(Model.Name, value, v => Model.Name = v);
    }

    public string Description
    {
        get => Model.Description ?? "";
        set => Set(Model.Description ?? "", value, v => Model.Description = string.IsNullOrWhiteSpace(v) ? null : v);
    }

    public string Technology
    {
        get => Model.Technology ?? "";
        set
        {
            Set(Model.Technology ?? "", value, v => Model.Technology = string.IsNullOrWhiteSpace(v) ? null : v);
            OnPropertyChanged(nameof(TechnologyTag));
        }
    }

    public string TechnologyTag => string.IsNullOrWhiteSpace(Model.Technology) ? "" : "[" + Model.Technology + "]";

    public double X => Model.X;
    public double Y => Model.Y;
    public double Left => Model.X - Width / 2;
    public double Top => Model.Y - Height / 2;
    public bool Pinned => Model.Pinned;

    /// <summary>Moves the centre of the element. Used by layout and dragging.</summary>
    public void MoveTo(double x, double y)
    {
        Model.X = x;
        Model.Y = y;
        RaisePosition();
    }

    /// <summary>Moves the element by hand. This pins it so auto-layout leaves it alone.</summary>
    public void DragTo(double x, double y)
    {
        Model.Pinned = true;
        MoveTo(x, y);
        _changed();
    }

    /// <summary>Re-reads position and pin state from the model after the model was changed directly.</summary>
    public void RaisePosition()
    {
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(Left));
        OnPropertyChanged(nameof(Top));
        OnPropertyChanged(nameof(Pinned));
    }

    private void Set(string current, string value, Action<string> apply, [CallerMemberName] string? property = null)
    {
        if (current == value) return;
        apply(value);
        OnPropertyChanged(property);
        _changed();
    }
}
