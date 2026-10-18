using System.ComponentModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using ContextSketch.Core.Geometry;
using ContextSketch.Core.Model;
using CorePoint = ContextSketch.Core.Geometry.Point;

namespace ContextSketch.App.ViewModels;

public partial class RelationshipViewModel : ObservableObject
{
    private const double ArrowLength = 12;
    private const double ArrowHalfWidth = 5;
    private const double LabelWidth = 160;

    private readonly Action _changed;

    public RelationshipViewModel(Relationship model, ElementViewModel from, ElementViewModel to, Action changed)
    {
        Model = model;
        From = from;
        To = to;
        _changed = changed;
        from.PropertyChanged += OnEndpointChanged;
        to.PropertyChanged += OnEndpointChanged;
    }

    public Relationship Model { get; }
    public ElementViewModel From { get; }
    public ElementViewModel To { get; }

    [ObservableProperty] private bool _isSelected;

    public string Label
    {
        get => Model.Label;
        set
        {
            if (Model.Label == value) return;
            Model.Label = value;
            OnPropertyChanged();
            _changed();
        }
    }

    public string Technology
    {
        get => Model.Technology ?? "";
        set
        {
            if (Technology == value) return;
            Model.Technology = string.IsNullOrWhiteSpace(value) ? null : value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TechnologyTag));
            _changed();
        }
    }

    public string TechnologyTag => string.IsNullOrWhiteSpace(Model.Technology) ? "" : "[" + Model.Technology + "]";

    public RelationshipDirection Direction
    {
        get => Model.Direction;
        set
        {
            if (Model.Direction == value) return;
            Model.Direction = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StartArrowVisible));
            _changed();
        }
    }

    public bool StartArrowVisible => Model.Direction == RelationshipDirection.TwoWay;

    private (CorePoint Start, CorePoint End) Line => EdgeGeometry.LineBetween(From.Model, To.Model);

    public double X1 => Line.Start.X;
    public double Y1 => Line.Start.Y;
    public double X2 => Line.End.X;
    public double Y2 => Line.End.Y;

    public double LabelLeft => EdgeGeometry.Midpoint(Line.Start, Line.End).X - LabelWidth / 2;
    public double LabelTop => EdgeGeometry.Midpoint(Line.Start, Line.End).Y - 18;
    public double LabelBoxWidth => LabelWidth;

    public PointCollection EndArrow => Arrow(Line.End, Line.Start);
    public PointCollection StartArrow => Arrow(Line.Start, Line.End);

    private static PointCollection Arrow(CorePoint tip, CorePoint tail)
    {
        var dx = tip.X - tail.X;
        var dy = tip.Y - tail.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1e-6) return [];

        var ux = dx / length;
        var uy = dy / length;
        var baseX = tip.X - ux * ArrowLength;
        var baseY = tip.Y - uy * ArrowLength;
        return
        [
            new System.Windows.Point(tip.X, tip.Y),
            new System.Windows.Point(baseX - uy * ArrowHalfWidth, baseY + ux * ArrowHalfWidth),
            new System.Windows.Point(baseX + uy * ArrowHalfWidth, baseY - ux * ArrowHalfWidth),
        ];
    }

    private void OnEndpointChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(ElementViewModel.X) or nameof(ElementViewModel.Y))) return;
        OnPropertyChanged(nameof(X1));
        OnPropertyChanged(nameof(Y1));
        OnPropertyChanged(nameof(X2));
        OnPropertyChanged(nameof(Y2));
        OnPropertyChanged(nameof(LabelLeft));
        OnPropertyChanged(nameof(LabelTop));
        OnPropertyChanged(nameof(EndArrow));
        OnPropertyChanged(nameof(StartArrow));
    }
}
