namespace ContextSketch.App.Services;

public enum UnsavedChoice
{
    Save,
    Discard,
    Cancel,
}

public interface IDialogService
{
    string? PickOpenFile();

    /// <summary>Asks where to save. The result always ends in .json; a bare name gets .contextsketch.json.</summary>
    string? PickSaveFile(string suggestedName);

    UnsavedChoice AskUnsavedChanges(string documentName);

    void ShowError(string title, string message);
}
