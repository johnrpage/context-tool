using System.Windows;
using Microsoft.Win32;

namespace ContextSketch.App.Services;

public sealed class WpfDialogService : IDialogService
{
    public const string DefaultSuffix = ".contextsketch.json";

    private const string OpenFilter = "Context diagrams (*.contextsketch.json;*.json)|*.contextsketch.json;*.json|All files (*.*)|*.*";
    private const string SaveFilter = "Context diagram (*.contextsketch.json)|*.contextsketch.json|JSON file (*.json)|*.json";

    public string? PickOpenFile()
    {
        var dialog = new OpenFileDialog { Filter = OpenFilter, Title = "Open diagram" };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickSaveFile(string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = SaveFilter,
            Title = "Save diagram",
            FileName = suggestedName,
            AddExtension = true,
            DefaultExt = DefaultSuffix,
            OverwritePrompt = true,
        };
        if (dialog.ShowDialog() != true) return null;

        var path = dialog.FileName;
        return path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? path : path + DefaultSuffix;
    }

    public UnsavedChoice AskUnsavedChanges(string documentName)
    {
        var answer = MessageBox.Show(
            "Save changes to " + documentName + "?",
            "ContextSketch",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);
        return answer switch
        {
            MessageBoxResult.Yes => UnsavedChoice.Save,
            MessageBoxResult.No => UnsavedChoice.Discard,
            _ => UnsavedChoice.Cancel,
        };
    }

    public void ShowError(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
}
