using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ContextSketch.App.ViewModels;

namespace ContextSketch.App.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    /// <summary>Double-clicking an element jumps straight into editing its name.</summary>
    private void OnElementDoubleClicked(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(() =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        }, System.Windows.Threading.DispatcherPriority.Input);

    /// <summary>Once a label has been typed, the layout may need a wider gap for it. Done on leaving the box so nothing jumps while typing.</summary>
    private void OnLabelLostFocus(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.RelayoutAfterLabelEdit();
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm && !vm.ConfirmClose()) e.Cancel = true;
    }

    /// <summary>Delete removes the selected element, but must not fire while text is being edited.</summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete || Keyboard.FocusedElement is TextBoxBase or ComboBox) return;
        if (DataContext is MainViewModel { DeleteCommand: var delete } && delete.CanExecute(null))
        {
            delete.Execute(null);
            e.Handled = true;
        }
    }
}
