namespace ClocView.Behaviors;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Xaml.Interactivity;

public sealed class WindowDragDropBehavior : Behavior<Window>
{
    public static readonly StyledProperty<ICommand?> DropCommandProperty =
        AvaloniaProperty.Register<WindowDragDropBehavior, ICommand?>(nameof(DropCommand));

    public ICommand? DropCommand
    {
        get => GetValue(DropCommandProperty);
        set => SetValue(DropCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        DragDrop.SetAllowDrop(AssociatedObject!, true);
        AssociatedObject!.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AssociatedObject!.AddHandler(DragDrop.DropEvent, OnDrop);
    }

    protected override void OnDetaching()
    {
        AssociatedObject!.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
        AssociatedObject!.RemoveHandler(DragDrop.DropEvent, OnDrop);
        DragDrop.SetAllowDrop(AssociatedObject!, false);

        base.OnDetaching();
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Formats.Contains(DataFormat.File)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var files = e.DataTransfer.TryGetFiles();
        if (files is null)
        {
            return;
        }

        var paths = files
            .Select(f => f.TryGetLocalPath())
            .OfType<string>()
            .ToArray();

        var command = DropCommand;
        if (command?.CanExecute(paths) == true)
        {
            command.Execute(paths);
        }

        e.Handled = true;
    }
}
