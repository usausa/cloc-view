namespace ClocView.Behaviors;

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Xaml.Interactivity;

public sealed class TextBoxEnterKeyBehavior : Behavior<TextBox>
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<TextBoxEnterKeyBehavior, ICommand?>(nameof(Command));

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
        if (e.Key != Key.Return)
        {
            return;
        }

        var command = Command;
        if (command?.CanExecute(null) == true)
        {
            command.Execute(null);
        }
    }
}
