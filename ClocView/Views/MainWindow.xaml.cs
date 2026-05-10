namespace ClocView.Views;

using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

public sealed partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnGridPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete)
        {
            return;
        }

        e.Handled = true;

        if (DataContext is not MainWindowViewModel vm || MainGrid.SelectedItems.Count == 0)
        {
            return;
        }

        // 削除前に次に選択すべきインデックスを確定しておく
        var lastSelected = MainGrid.SelectedItems[^1] as ClocRecord;
        var nextIndex = lastSelected is not null ? vm.Records.IndexOf(lastSelected) : -1;

        vm.DeleteCommand.Execute(MainGrid.SelectedItems);

        // 削除後の選択インデックス
        if (vm.Records.Count == 0)
        {
            return;
        }

        var targetIndex = Math.Min(nextIndex, vm.Records.Count - 1);

        // UI の仮想化更新を待ってからフォーカス復帰
        Dispatcher.InvokeAsync(() =>
        {
            MainGrid.SelectedIndex = targetIndex;
            MainGrid.ScrollIntoView(MainGrid.SelectedItem);

            // セルコンテナにフォーカスを当てる
            if (MainGrid.ItemContainerGenerator.ContainerFromIndex(targetIndex) is DataGridRow row)
            {
                var cell = GetCell(row, 0);
                cell?.Focus();
            }
        }, DispatcherPriority.Input);
    }

    private static DataGridCell? GetCell(DataGridRow row, int columnIndex)
    {
        var presenter = FindVisualChild<DataGridCellsPresenter>(row);
        return presenter?.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
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

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }
}
