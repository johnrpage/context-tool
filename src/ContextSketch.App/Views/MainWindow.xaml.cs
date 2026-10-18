using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ContextSketch.App.ViewModels;

namespace ContextSketch.App.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

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
