namespace ClocView.Behaviors;

using System.Collections;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

using Microsoft.Xaml.Behaviors;

public sealed class DataGridDeleteKeyBehavior : Behavior<DataGrid>
{
    public static readonly DependencyProperty DeleteCommandProperty = DependencyProperty.Register(
        nameof(DeleteCommand),
        typeof(ICommand),
        typeof(DataGridDeleteKeyBehavior));

    public ICommand? DeleteCommand
    {
        get => (ICommand?)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewKeyDown += OnPreviewKeyDown;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.PreviewKeyDown -= OnPreviewKeyDown;
        base.OnDetaching();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete)
        {
            return;
        }

        var grid = AssociatedObject;
        if (grid.SelectedItems.Count == 0)
        {
            return;
        }

        e.Handled = true;

        var command = DeleteCommand;
        if (command is null || !command.CanExecute(grid.SelectedItems))
        {
            return;
        }

        var items = (grid.ItemsSource as IList) ?? Array.Empty<object>();
        var lastSelected = grid.SelectedItems[^1];
        var nextIndex = items.IndexOf(lastSelected);

        command.Execute(grid.SelectedItems);

        var count = items.Count;
        if (count == 0)
        {
            return;
        }

        var targetIndex = Math.Min(nextIndex, count - 1);

        grid.Dispatcher.InvokeAsync(() =>
        {
            grid.SelectedIndex = targetIndex;
            grid.ScrollIntoView(grid.SelectedItem);

            if (grid.ItemContainerGenerator.ContainerFromIndex(targetIndex) is DataGridRow row)
            {
                var cell = FindCell(row, 0);
                cell?.Focus();
            }
        }, DispatcherPriority.Input);
    }

    private static DataGridCell? FindCell(DataGridRow row, int columnIndex)
    {
        var presenter = FindVisualChild<DataGridCellsPresenter>(row);
        return presenter?.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
    }

    private static T? FindVisualChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                return match;
            }

            var result = FindVisualChild<T>(child);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}
