namespace ClocView.Behaviors;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

using ClocView.Services;

public sealed class DataGridDeleteKeyBehavior : Behavior<DataGrid>
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<DataGridDeleteKeyBehavior, ICommand?>(nameof(Command));

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject!.KeyDown += OnKeyDown;
    }

    protected override void OnDetaching()
    {
        AssociatedObject!.KeyDown -= OnKeyDown;

        base.OnDetaching();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete)
        {
            return;
        }

        var grid = AssociatedObject!;
        if (grid.SelectedItems.Count == 0)
        {
            return;
        }

        var command = Command;
        if (command is null)
        {
            return;
        }

        e.Handled = true;

        var nextIndex = grid.SelectedItems[^1] is ClocRecord lastSelected
            ? (grid.ItemsSource as IList)?.IndexOf(lastSelected) ?? -1
            : -1;

        var targets = grid.SelectedItems.Cast<object>().ToList();
        if (command.CanExecute(targets))
        {
            command.Execute(targets);
        }

        if (grid.ItemsSource is not IList items || items.Count == 0)
        {
            return;
        }

        var targetIndex = Math.Min(nextIndex, items.Count - 1);
        if (targetIndex >= 0)
        {
            grid.SelectedIndex = targetIndex;
            grid.ScrollIntoView(grid.SelectedItem, null);
        }
    }
}
