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

    // ---- Selection -------------------------------------------------------------------------

    /// <summary>The selected element, if an element (not a line) is selected.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private ElementViewModel? _selectedElement;

    /// <summary>The relationship shown in the properties panel: the selected line, or the line of the selected element.</summary>
    [ObservableProperty] private RelationshipViewModel? _panelRelationship;

    public bool NothingSelected => SelectedElement is null && PanelRelationship is null;

    public string DiagramTitle
    {
        get => Diagram.Title;
        set
        {
            if (Diagram.Title == value) return;
            Diagram.Title = value;
            OnPropertyChanged();
            OnModelChanged();
        }
    }

    public void SelectElement(ElementViewModel element) =>
        SetSelection(element, Relationships.FirstOrDefault(r => r.From == element || r.To == element), element);

    public void SelectRelationship(RelationshipViewModel relationship) => SetSelection(null, relationship, relationship);

    public void ClearSelection() => SetSelection(null, null, null);

    private void SetSelection(ElementViewModel? element, RelationshipViewModel? panel, object? highlighted)
    {
        foreach (var e in Elements) e.IsSelected = e == highlighted;
        foreach (var r in Relationships) r.IsSelected = r == highlighted;
        SelectedElement = element;
        PanelRelationship = panel;
        OnPropertyChanged(nameof(NothingSelected));
    }

    // ---- Editing ---------------------------------------------------------------------------

    [RelayCommand]
    private void AddElement(ElementType type)
    {
        var count = Diagram.Elements.Count(e => e.Type == type);
        var (element, relationship) = Diagram.AddElement(type, DefaultName(type) + " " + (count + 1));

        var elementVm = new ElementViewModel(element, OnModelChanged);
        Elements.Add(elementVm);
        var from = Elements.First(e => e.Id == relationship.From);
        var to = Elements.First(e => e.Id == relationship.To);
        Relationships.Add(new RelationshipViewModel(relationship, from, to, OnModelChanged));

        RunLayout(clearPins: false);
        SelectElement(elementVm);
    }

    private bool CanDelete() => SelectedElement is { Type: not ElementType.System };

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        var element = SelectedElement;
        if (element is null || !Diagram.RemoveElement(element.Id)) return;

        foreach (var relationship in Relationships.Where(r => r.From == element || r.To == element).ToList())
            Relationships.Remove(relationship);
        Elements.Remove(element);
        ClearSelection();
        OnModelChanged();
    }

    [RelayCommand]
    private void AutoLayout() => RunLayout(clearPins: false);

    [RelayCommand]
    private void AutoLayoutAll() => RunLayout(clearPins: true);

    private void RunLayout(bool clearPins)
    {
        Core.Layout.AutoLayout.Apply(Diagram, clearPins);
        foreach (var e in Elements) e.RaisePosition();
        OnModelChanged();
    }

    private static string DefaultName(ElementType type) => type switch
    {
        ElementType.PrimaryActor => "Primary Actor",
        ElementType.SecondaryActor => "Secondary Actor",
        ElementType.ExternalSystem => "External System",
        ElementType.Storage => "Storage",
        _ => "Element",
    };

    // ---- Files -----------------------------------------------------------------------------

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

        if (result.NeedsLayout) Core.Layout.AutoLayout.Apply(result.Diagram);
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

        ClearSelection();
        OnPropertyChanged(nameof(DiagramTitle));
    }

    private void OnModelChanged()
    {
        // Dirty tracking arrives with file handling.
    }
}
