using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ContextSketch.App.ViewModels;

namespace ContextSketch.App.Views;

/// <summary>Pan, zoom, selection and drag plumbing only; everything drawn comes from bindings.</summary>
public partial class DiagramCanvas : UserControl
{
    private const double MinZoom = 0.2;
    private const double MaxZoom = 3.0;
    private const double DragThreshold = 3;

    private enum Mode { None, Pan, DragElement }

    private Mode _mode;
    private bool _centred;
    private Point _mouseStart;
    private Point _panOrigin;
    private ElementViewModel? _dragged;
    private Point _dragOrigin;
    private bool _dragStarted;

    public event EventHandler? ElementDoubleClicked;

    public DiagramCanvas() => InitializeComponent();

    private MainViewModel? Main => DataContext as MainViewModel;
    private double Zoom => ScaleTransform.ScaleX;

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_centred || e.NewSize.Width < 1) return;

        // Put the diagram origin (the system) in the middle of the viewport.
        PanTransform.X = e.NewSize.Width / 2;
        PanTransform.Y = e.NewSize.Height / 2;
        _centred = true;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var mouse = e.GetPosition(Viewport);
        var oldScale = Zoom;
        var newScale = Math.Clamp(oldScale * (e.Delta > 0 ? 1.1 : 1 / 1.1), MinZoom, MaxZoom);

        // Keep the point under the cursor fixed while zooming.
        var worldX = (mouse.X - PanTransform.X) / oldScale;
        var worldY = (mouse.Y - PanTransform.Y) / oldScale;
        ScaleTransform.ScaleX = ScaleTransform.ScaleY = newScale;
        PanTransform.X = mouse.X - worldX * newScale;
        PanTransform.Y = mouse.Y - worldY * newScale;
        e.Handled = true;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        Viewport.Focus();
        Keyboard.Focus(Viewport);
        _mouseStart = e.GetPosition(Viewport);

        switch (FindViewModel(e.OriginalSource as DependencyObject))
        {
            case ElementViewModel element:
                Main?.SelectElement(element);
                if (e.ClickCount == 2) ElementDoubleClicked?.Invoke(this, EventArgs.Empty);
                _mode = Mode.DragElement;
                _dragged = element;
                _dragOrigin = new Point(element.X, element.Y);
                _dragStarted = false;
                break;
            case RelationshipViewModel relationship:
                Main?.SelectRelationship(relationship);
                _mode = Mode.None;
                break;
            default:
                Main?.ClearSelection();
                _mode = Mode.Pan;
                _panOrigin = new Point(PanTransform.X, PanTransform.Y);
                break;
        }

        if (_mode != Mode.None) Viewport.CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var now = e.GetPosition(Viewport);
        var delta = now - _mouseStart;

        if (_mode == Mode.Pan)
        {
            PanTransform.X = _panOrigin.X + delta.X;
            PanTransform.Y = _panOrigin.Y + delta.Y;
        }
        else if (_mode == Mode.DragElement && _dragged is not null)
        {
            // A plain click must not pin the element; only a real drag does.
            if (!_dragStarted && delta.Length < DragThreshold) return;
            _dragStarted = true;
            _dragged.DragTo(_dragOrigin.X + delta.X / Zoom, _dragOrigin.Y + delta.Y / Zoom);
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _mode = Mode.None;
        _dragged = null;
        Viewport.ReleaseMouseCapture();
    }

    /// <summary>Walks up from the clicked visual to the element or relationship it belongs to.</summary>
    private object? FindViewModel(DependencyObject? source)
    {
        while (source is not null && source != Viewport)
        {
            if (source is FrameworkElement { DataContext: ElementViewModel or RelationshipViewModel } fe)
                return fe.DataContext;
            source = source is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }
        return null;
    }
}
