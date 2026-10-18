using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ContextSketch.App.Views;

/// <summary>Pan and zoom plumbing only; everything drawn comes from bindings.</summary>
public partial class DiagramCanvas : UserControl
{
    private const double MinZoom = 0.2;
    private const double MaxZoom = 3.0;

    private bool _centred;
    private bool _panning;
    private Point _panStart;
    private Point _panOrigin;

    public DiagramCanvas() => InitializeComponent();

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
        var oldScale = ScaleTransform.ScaleX;
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
        _panning = true;
        _panStart = e.GetPosition(Viewport);
        _panOrigin = new Point(PanTransform.X, PanTransform.Y);
        Viewport.CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_panning) return;
        var now = e.GetPosition(Viewport);
        PanTransform.X = _panOrigin.X + now.X - _panStart.X;
        PanTransform.Y = _panOrigin.Y + now.Y - _panStart.Y;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _panning = false;
        Viewport.ReleaseMouseCapture();
    }
}
