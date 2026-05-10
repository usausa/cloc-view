namespace ClocView.Behaviors;

using Microsoft.Xaml.Behaviors;

public sealed class FileDragDropBehavior : Behavior<UIElement>
{
    public static readonly DependencyProperty DropCommandProperty = DependencyProperty.Register(
        nameof(DropCommand),
        typeof(ICommand),
        typeof(FileDragDropBehavior));

    public ICommand? DropCommand
    {
        get => (ICommand?)GetValue(DropCommandProperty);
        set => SetValue(DropCommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.AllowDrop = true;
        AssociatedObject.DragOver += OnDragOver;
        AssociatedObject.Drop += OnDrop;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.DragOver -= OnDragOver;
        AssociatedObject.Drop -= OnDrop;
        base.OnDetaching();
    }

    private static void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        var command = DropCommand;
        if (command is null || !command.CanExecute(e.Data))
        {
            return;
        }

        command.Execute(e.Data);
        e.Handled = true;
    }
}
