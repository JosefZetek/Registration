using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace Registration.Views;

/// <summary>
/// One test case: a micro volume plus the ground-truth transformation it should register to.
/// </summary>
public sealed class ExperimentTestCase
{
    public string MicroMhdPath { get; }
    public string MicroRawPath { get; }
    public string GroundTruthPath { get; }

    public ExperimentTestCase(string microMhdPath, string microRawPath, string groundTruthPath)
    {
        MicroMhdPath = microMhdPath;
        MicroRawPath = microRawPath;
        GroundTruthPath = groundTruthPath;
    }

    public string Name => Path.GetFileNameWithoutExtension(MicroMhdPath);
}

/// <summary>
/// One macro volume with the test cases that should be registered onto it.
/// </summary>
public sealed class ExperimentGroup
{
    public string MacroMhdPath { get; }
    public string MacroRawPath { get; }
    public List<ExperimentTestCase> Tests { get; } = new();

    public ExperimentGroup(string macroMhdPath, string macroRawPath)
    {
        MacroMhdPath = macroMhdPath;
        MacroRawPath = macroRawPath;
    }

    /* Folder name is included so two macros with the same file name stay distinguishable. */
    public string Name =>
        $"{Path.GetFileName(Path.GetDirectoryName(MacroMhdPath))}/{Path.GetFileNameWithoutExtension(MacroMhdPath)}";
}

/// <summary>
/// Configuration produced by <see cref="ExperimentSetupDialog"/>.
/// </summary>
public sealed class ExperimentConfiguration
{
    public IReadOnlyList<ExperimentGroup> Groups { get; }

    public ExperimentConfiguration(IReadOnlyList<ExperimentGroup> groups)
    {
        Groups = groups;
    }

    public int TotalTests => Groups.Sum(group => group.Tests.Count);
}

/// <summary>
/// Modal shown before an experiment run. The user assembles the run manually: any number
/// of macro volumes (parent rows), each with any number of test cases (child rows), every
/// file picked explicitly - no naming convention required.
/// </summary>
public partial class ExperimentSetupDialog : Window
{
    private static readonly FilePickerFileType MhdType =
        new("MetaImage Files") { Patterns = new[] { "*.mhd" } };
    private static readonly FilePickerFileType RawType =
        new("Raw Image Files") { Patterns = new[] { "*.raw" } };
    private static readonly FilePickerFileType TxtType =
        new("Transformation Files") { Patterns = new[] { "*.txt" } };

    private readonly List<ExperimentGroup> _groups = new();

    public ExperimentSetupDialog()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        RebuildPreview();
    }

    private async Task<string?> PickFile(string title, FilePickerFileType type)
    {
        var picks = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[] { type },
        });

        return picks.Count == 0 ? null : picks[0].Path.AbsolutePath;
    }

    #region Button Event Handlers

    private async void OnAddMacroButton_Click(object? sender, RoutedEventArgs e)
    {
        string? mhdPath = await PickFile("Select macro meta data (.mhd)", MhdType);
        if (mhdPath == null)
            return;

        string? rawPath = await PickFile("Select macro raw data (.raw)", RawType);
        if (rawPath == null)
            return;

        _groups.Add(new ExperimentGroup(mhdPath, rawPath));
        RebuildPreview();
    }

    private async void AddTest(ExperimentGroup group)
    {
        string? mhdPath = await PickFile("Select micro meta data (.mhd)", MhdType);
        if (mhdPath == null)
            return;

        string? rawPath = await PickFile("Select micro raw data (.raw)", RawType);
        if (rawPath == null)
            return;

        string? txtPath = await PickFile("Select ground-truth transformation (.txt)", TxtType);
        if (txtPath == null)
            return;

        group.Tests.Add(new ExperimentTestCase(mhdPath, rawPath, txtPath));
        RebuildPreview();
    }

    private void OnCancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void OnRunButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(new ExperimentConfiguration(_groups));
    }

    #endregion

    #region File Preview

    private static Button SmallButton(string caption, System.Action onClick)
    {
        var button = new Button { Content = caption, FontSize = 12, Padding = new Avalonia.Thickness(6, 2) };
        button.Click += (_, _) => onClick();
        return button;
    }

    private static StackPanel Row(params Control[] children)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
        };

        foreach (var child in children)
        {
            child.VerticalAlignment = VerticalAlignment.Center;
            row.Children.Add(child);
        }

        return row;
    }

    private TreeViewItem BuildGroupItem(ExperimentGroup group)
    {
        var header = Row(
            new TextBlock
            {
                Text = $"{group.Name}  ({Path.GetFileName(group.MacroMhdPath)}, {Path.GetFileName(group.MacroRawPath)})",
                FontWeight = Avalonia.Media.FontWeight.SemiBold,
            },
            SmallButton("+ Add Test", () => AddTest(group)),
            SmallButton("Remove", () => { _groups.Remove(group); RebuildPreview(); }));

        var children = group.Tests.Select(test => new TreeViewItem
        {
            Header = Row(
                new TextBlock
                {
                    Text = $"{test.Name}  ({Path.GetFileName(test.MicroMhdPath)}, " +
                           $"{Path.GetFileName(test.MicroRawPath)}, {Path.GetFileName(test.GroundTruthPath)})",
                },
                SmallButton("Remove", () => { group.Tests.Remove(test); RebuildPreview(); })),
        }).ToList();

        return new TreeViewItem { Header = header, ItemsSource = children, IsExpanded = true };
    }

    private void RebuildPreview()
    {
        var tree = this.FindControl<TreeView>("FilePreviewTree");
        var runButton = this.FindControl<Button>("RunButton");
        var validationText = this.FindControl<TextBlock>("ValidationText");

        if (tree == null || runButton == null || validationText == null)
            return;

        if (_groups.Count == 0)
        {
            tree.ItemsSource = new[] { new TreeViewItem { Header = "No macro data added yet." } };
            runButton.IsEnabled = false;
            validationText.Text = string.Empty;
            return;
        }

        tree.ItemsSource = _groups.Select(BuildGroupItem).ToList();

        bool everyGroupHasTests = _groups.All(group => group.Tests.Count > 0);
        runButton.IsEnabled = everyGroupHasTests;
        validationText.Text = everyGroupHasTests ? string.Empty : "Every macro volume needs at least one test.";
    }

    #endregion
}
