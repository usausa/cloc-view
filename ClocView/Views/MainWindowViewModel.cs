namespace ClocView.Views;

// ReSharper disable once ClassNeverInstantiated.Global
[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public sealed class MainWindowViewModel : ExtendViewModelBase
{
    public ICommand ExecuteCommand { get; }

    public MainWindowViewModel()
    {
        ExecuteCommand = MakeAsyncCommand(Execute, () => !BusyState.IsBusy);
    }

    private static async Task Execute()
    {
        await Task.Delay(3000).ConfigureAwait(true);
    }
}
