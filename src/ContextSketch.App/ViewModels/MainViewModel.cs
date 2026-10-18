using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContextSketch.App.Services;
using ContextSketch.Core.Layout;
using ContextSketch.Core.Model;
using ContextSketch.Core.Serialization;

namespace ContextSketch.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDialogService _dialogs;

    public MainViewModel(IDialogService dialogs)
    {
        _dialogs = dialogs;
        Show(Diagram.CreateNew());
    }

    public Diagram Diagram { get; private set; } = null!;
    public ObservableCollection<ElementViewModel> Elements { get; } = [];
    public ObservableCollection<RelationshipViewModel> Relationships { get; } = [];

    [ObservableProperty] private string _windowTitle = "ContextSketch";

    [RelayCommand]
    private void Open()
    {
        var path = _dialogs.PickOpenFile();
        if (path is not null) LoadFile(path);
    }

    public void LoadFile(string path)
    {
        LoadResult result;
        try
        {
            result = DiagramJson.Load(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError("Cannot open file", ex.Message);
            return;
        }

        if (result.Diagram is null)
        {
            var problems = string.Join(Environment.NewLine + "- ", result.Errors);
            _dialogs.ShowError("Cannot open file", Path.GetFileName(path) + " is not a valid diagram:" + Environment.NewLine + Environment.NewLine + "- " + problems);
            return;
        }

        if (result.NeedsLayout) AutoLayout.Apply(result.Diagram);
        Show(result.Diagram);
        WindowTitle = Path.GetFileName(path) + " - ContextSketch";
    }

    /// <summary>Replaces the displayed diagram. The previous one stays untouched until this is called.</summary>
    private void Show(Diagram diagram)
    {
        Diagram = diagram;
        Elements.Clear();
        Relationships.Clear();

        var byId = new Dictionary<string, ElementViewModel>();
        foreach (var element in diagram.Elements)
        {
            var vm = new ElementViewModel(element, OnModelChanged);
            byId[element.Id] = vm;
            Elements.Add(vm);
        }
        foreach (var relationship in diagram.Relationships)
            Relationships.Add(new RelationshipViewModel(relationship, byId[relationship.From], byId[relationship.To], OnModelChanged));
    }

    private void OnModelChanged()
    {
        // Dirty tracking arrives with file handling.
    }
}
