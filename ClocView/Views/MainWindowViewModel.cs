namespace ClocView.Views;

using System.Text.Json;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

// ReSharper disable once ClassNeverInstantiated.Global
[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public sealed partial class MainWindowViewModel : ExtendViewModelBase
{
    private readonly ClocService clocService;

    private CancellationTokenSource? cts;

    [ObservableProperty]
    public partial string TargetDirectory { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial int TotalBlank { get; set; }

    [ObservableProperty]
    public partial int TotalComment { get; set; }

    [ObservableProperty]
    public partial int TotalCode { get; set; }

    public ObservableCollection<ClocRecord> Records { get; } = [];

    public ICommand ExecuteCommand { get; }

    public ICommand CancelCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand ExportCsvCommand { get; }

    public ICommand SelectDirectoryCommand { get; }

    public ICommand ExitCommand { get; }

    /// <summary>Window.Loaded にバインドし、起動引数があれば自動実行する。</summary>
    public ICommand LoadedCommand { get; }

    /// <summary>D&amp;D で渡されたファイルパスを受け取り、フォルダがあれば処理する。</summary>
    public ICommand DropCommand { get; }

    public MainWindowViewModel()
    {
        TargetDirectory = string.Empty;
        StatusMessage = string.Empty;

        var settings = LoadSettings();
        clocService = new ClocService(settings);

        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1 && Directory.Exists(args[1]))
        {
            TargetDirectory = args[1];
        }

        ExecuteCommand = MakeAsyncCommand(ExecuteAsync, () => !BusyState.IsBusy);
        CancelCommand = new DelegateCommand(Cancel, () => BusyState.IsBusy);
        DeleteCommand = new DelegateCommand<IList>(DeleteSelected);
        ExportCsvCommand = new DelegateCommand(ExportCsvAsync, () => Records.Count > 0);
        SelectDirectoryCommand = new DelegateCommand(SelectDirectoryAsync, () => !BusyState.IsBusy);
        ExitCommand = new DelegateCommand(Exit);
        LoadedCommand = MakeAsyncCommand(OnLoadedAsync);
        DropCommand = MakeAsyncCommand<string[]>(OnDropAsync);
    }

    //--------------------------------------------------------------------------------
    // Dispose
    //--------------------------------------------------------------------------------

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Loaded / Drop
    //--------------------------------------------------------------------------------

    private async Task OnLoadedAsync()
    {
        if (!string.IsNullOrWhiteSpace(TargetDirectory))
        {
            await ExecuteAsync().ConfigureAwait(true);
        }
    }

    private async Task OnDropAsync(string[]? paths)
    {
        if (paths is null)
        {
            return;
        }

        var directory = paths.FirstOrDefault(Directory.Exists);
        if (directory is null)
        {
            return;
        }

        TargetDirectory = directory;
        await ExecuteAsync().ConfigureAwait(true);
    }

    //--------------------------------------------------------------------------------
    // Commands
    //--------------------------------------------------------------------------------

    private void Cancel()
    {
        cts?.Cancel();
    }

    private void DeleteSelected(IList? selectedItems)
    {
        if (selectedItems is null || selectedItems.Count == 0)
        {
            return;
        }

        foreach (var item in selectedItems.Cast<ClocRecord>().ToList())
        {
            Records.Remove(item);
        }

        UpdateTotals();
    }

    private async Task ExecuteAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetDirectory))
        {
            return;
        }

        if (cts is not null)
        {
            await cts.CancelAsync();
            cts.Dispose();
        }
        cts = new CancellationTokenSource();
        var ct = cts.Token;

        Records.Clear();
        UpdateTotals();
        StatusMessage = "Running cloc...";

#pragma warning disable CA1031
        try
        {
            var records = await clocService.ExecuteAsync(TargetDirectory, ct).ConfigureAwait(true);

            var baseDir = TargetDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                          + Path.DirectorySeparatorChar;

            foreach (var record in records)
            {
                if (!string.IsNullOrEmpty(record.Filename))
                {
                    record.RelativePath = record.Filename.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase)
                        ? record.Filename[baseDir.Length..]
                        : record.Filename;
                }
            }

            foreach (var record in records.OrderBy(r => Path.GetDirectoryName(r.RelativePath)).ThenBy(r => r.Language))
            {
                Records.Add(record);
            }

            UpdateTotals();
            StatusMessage = $"Completed. {Records.Count} items.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
#pragma warning restore CA1031
    }

    // ReSharper disable once AsyncVoidMethod
    private async void ExportCsvAsync()
    {
        var window = GetTopLevel();
        if (window is null)
        {
            return;
        }

        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save as CSV",
            SuggestedFileName = "cloc_result",
            DefaultExtension = "csv",
            FileTypeChoices = [new FilePickerFileType("CSV files") { Patterns = ["*.csv"] }]
        }).ConfigureAwait(true);

        if (file is null)
        {
            return;
        }

        await using var stream = await file.OpenWriteAsync().ConfigureAwait(true);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        await using var csv = new CsvHelper.CsvWriter(writer, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture));
        await csv.WriteRecordsAsync(Records);

        StatusMessage = $"Saved: {file.Name}";
    }

    // ReSharper disable once AsyncVoidMethod
    private async void SelectDirectoryAsync()
    {
        var window = GetTopLevel();
        if (window is null)
        {
            return;
        }

        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Directory",
            AllowMultiple = false
        }).ConfigureAwait(true);

        if (folders.Count == 0)
        {
            return;
        }

        var path = folders[0].TryGetLocalPath();
        if (path is not null)
        {
            TargetDirectory = path;
        }
    }

    private void Exit()
    {
        Cancel();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }
    }

    private static Avalonia.Controls.Window? GetTopLevel()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return lifetime.MainWindow;
        }

        return null;
    }

    private void UpdateTotals()
    {
        TotalBlank = Records.Sum(r => r.Blank);
        TotalComment = Records.Sum(r => r.Comment);
        TotalCode = Records.Sum(r => r.Code);
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static ClocSetting LoadSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return new ClocSetting();
        }

#pragma warning disable CA1031
        try
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("Cloc", out var clocElement))
            {
                return new ClocSetting();
            }

            return JsonSerializer.Deserialize<ClocSetting>(clocElement.GetRawText(), JsonOptions) ?? new ClocSetting();
        }
        catch
        {
            return new ClocSetting();
        }
#pragma warning restore CA1031
    }
}
