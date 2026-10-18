using System.Windows;
using System.Windows.Media;
using ContextSketch.App.Services;
using ContextSketch.App.ViewModels;
using ContextSketch.App.Views;
using ContextSketch.Core;

namespace ContextSketch.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // One source of truth for colours: DiagramStyle feeds both this view and the SVG export.
        foreach (var (name, hex) in DiagramStyle.All)
            Resources[name] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));

        var viewModel = new MainViewModel(new WpfDialogService());
        if (e.Args.Length > 0) viewModel.LoadFile(e.Args[0]);

        new MainWindow { DataContext = viewModel }.Show();
    }
}
