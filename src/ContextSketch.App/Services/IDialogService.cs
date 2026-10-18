namespace ContextSketch.App.Services;

public interface IDialogService
{
    string? PickOpenFile();
    void ShowError(string title, string message);
}
