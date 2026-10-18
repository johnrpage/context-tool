using System.Windows;
using Microsoft.Win32;

namespace ContextSketch.App.Services;

public sealed class WpfDialogService : IDialogService
{
    public const string FileFilter = "Context diagrams (*.contextsketch.json;*.json)|*.contextsketch.json;*.json|All files (*.*)|*.*";

    public string? PickOpenFile()
    {
        var dialog = new OpenFileDialog { Filter = FileFilter, Title = "Open diagram" };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public void ShowError(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
}
