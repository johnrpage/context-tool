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

    private string? _path;
    private bool _isDirty;

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (_isDirty == value) return;
            _isDirty = value;
            OnPropertyChanged(nameof(WindowTitle));
        }
    }

    private string DocumentName => _path is null ? "Untitled" : Path.GetFileName(_path);

    public string WindowTitle => (IsDirty ? "*" : "") + DocumentName + " - ContextSketch";

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
    private void New()
    {
        if (!ConfirmDiscard()) return;
        Show(Diagram.CreateNew());
        SetDocument(null);
    }

    [RelayCommand]
    private void Open()
    {
        var path = _dialogs.PickOpenFile();
        if (path is not null) LoadFile(path);
    }

    /// <summary>Loads a file. A bad file is reported and the current diagram stays exactly as it was.</summary>
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

        // Only ask about unsaved work once we know the new file is usable.
        if (!ConfirmDiscard()) return;

        if (result.NeedsLayout) Core.Layout.AutoLayout.Apply(result.Diagram);
        Show(result.Diagram);
        SetDocument(path);
    }

    [RelayCommand]
    private void Save() => TrySave();

    [RelayCommand]
    private void SaveAs() => TrySaveAs();

    [RelayCommand]
    private void ExportSvg()
    {
        var name = string.IsNullOrWhiteSpace(Diagram.Title) ? "diagram" : Diagram.Title.Trim();
        var path = _dialogs.PickExportFile(name + ".svg");
        if (path is null) return;

        try
        {
            File.WriteAllText(path, Core.Svg.SvgExporter.Export(Diagram));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError("Cannot export file", ex.Message);
        }
    }

    /// <summary>Called when the window is closing. Returns false if the user cancelled.</summary>
    public bool ConfirmClose() => ConfirmDiscard();

    private bool ConfirmDiscard()
    {
        if (!IsDirty) return true;
        return _dialogs.AskUnsavedChanges(DocumentName) switch
        {
            UnsavedChoice.Save => TrySave(),
            UnsavedChoice.Discard => true,
            _ => false,
        };
    }

    private bool TrySave() => _path is null ? TrySaveAs() : WriteTo(_path);

    private bool TrySaveAs()
    {
        var suggested = string.IsNullOrWhiteSpace(Diagram.Title) ? "diagram" : Diagram.Title.Trim();
        var path = _dialogs.PickSaveFile(_path is null ? suggested + Services.WpfDialogService.DefaultSuffix : Path.GetFileName(_path));
        return path is not null && WriteTo(path);
    }

    private bool WriteTo(string path)
    {
        try
        {
            File.WriteAllText(path, DiagramJson.Save(Diagram) + "\n");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogs.ShowError("Cannot save file", ex.Message);
            return false;
        }
        SetDocument(path);
        return true;
    }

    private void SetDocument(string? path)
    {
        _path = path;
        _isDirty = false;
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(WindowTitle));
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

    private void OnModelChanged() => IsDirty = true;
}
