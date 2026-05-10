namespace ClocView.Views;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;

using ClocView.Services;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnWindowDragOver);
        AddHandler(DragDrop.DropEvent, OnWindowDrop);

        Loaded += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.LoadedCommand.Execute(null);
            }
        };

        var textBox = this.FindControl<TextBox>("DirectoryTextBox");
        if (textBox is not null)
        {
            textBox.KeyDown += OnDirectoryTextBoxKeyDown;
        }
    }

    private void OnDirectoryTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return && DataContext is MainWindowViewModel vm)
        {
            vm.ExecuteCommand.Execute(null);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key != Key.Delete)
        {
            return;
        }

        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var grid = this.FindControl<DataGrid>("MainGrid");
        if (grid is null || grid.SelectedItems.Count == 0)
        {
            return;
        }

        e.Handled = true;

        var nextIndex = grid.SelectedItems[^1] is ClocRecord lastSelected ? vm.Records.IndexOf(lastSelected) : -1;

        vm.DeleteCommand.Execute(grid.SelectedItems);

        if (vm.Records.Count == 0)
        {
            return;
        }

        var targetIndex = Math.Min(nextIndex, vm.Records.Count - 1);
        grid.SelectedIndex = targetIndex;
        grid.ScrollIntoView(grid.SelectedItem, null);
    }

    private static void OnWindowDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Formats.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnWindowDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        var files = e.DataTransfer.TryGetFiles();
        if (files is null)
        {
            return;
        }

        var paths = files
            .Select(f => f.TryGetLocalPath())
            .OfType<string>()
            .ToArray();

        vm.DropCommand.Execute(paths);
        e.Handled = true;
    }
}
