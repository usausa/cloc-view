namespace ClocView.Behaviors;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

public sealed class LoadedCommandBehavior : Behavior<Control>
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<LoadedCommandBehavior, ICommand?>(nameof(Command));

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject!.Loaded += OnLoaded;
    }

    protected override void OnDetaching()
    {
        AssociatedObject!.Loaded -= OnLoaded;

        base.OnDetaching();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var command = Command;
        if (command?.CanExecute(null) == true)
        {
            command.Execute(null);
        }
    }
}
