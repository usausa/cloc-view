namespace ClocView.Views;

using System.Text.Json;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Win32;

using Smart.Windows.Input;

// ReSharper disable once ClassNeverInstantiated.Global
[ObservableGeneratorOption(Reactive = true, ViewModel = true)]
public sealed partial class MainWindowViewModel : ExtendViewModelBase
{
    private readonly ClocService clocService;

    [ObservableProperty]
    public partial string TargetDirectory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int TotalBlank { get; set; }

    [ObservableProperty]
    public partial int TotalComment { get; set; }

    [ObservableProperty]
    public partial int TotalCode { get; set; }

    public ObservableCollection<ClocRecord> Records { get; } = [];

    public ICommand ExecuteCommand { get; }

    public ICommand DeleteCommand { get; }

    public ICommand ExportCsvCommand { get; }

    public ICommand SelectDirectoryCommand { get; }

    public ICommand ExitCommand { get; }

    /// <summary>Window.Loaded にバインドし、起動引数があれば自動実行する。</summary>
    public ICommand LoadedCommand { get; }

    /// <summary>D&amp;D で渡された IDataObject を受け取り、フォルダがあれば処理する。</summary>
    public ICommand DropCommand { get; }

    public MainWindowViewModel()
    {
        var settings = LoadSettings();
        clocService = new ClocService(settings);

        // 起動引数: index 0 は実行ファイル自身なので 1 以降を参照
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1 && Directory.Exists(args[1]))
        {
            TargetDirectory = args[1];
        }

        ExecuteCommand = MakeAsyncCommand(ExecuteAsync, () => !BusyState.IsBusy);
        DeleteCommand = new DelegateCommand<IList>(DeleteSelected);
        ExportCsvCommand = new DelegateCommand(ExportCsv, () => Records.Count > 0);
        SelectDirectoryCommand = new DelegateCommand(SelectDirectory, () => !BusyState.IsBusy);
        ExitCommand = new DelegateCommand(Application.Current.Shutdown);
        LoadedCommand = MakeAsyncCommand(OnLoadedAsync);
        DropCommand = MakeAsyncCommand<IDataObject>(OnDropAsync);
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

    private async Task OnDropAsync(IDataObject data)
    {
        if (data.GetData(DataFormats.FileDrop) is not string[] paths)
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

        Records.Clear();
        UpdateTotals();
        StatusMessage = "Running cloc...";

#pragma warning disable CA1031
        try
        {
            var records = await clocService.ExecuteAsync(TargetDirectory).ConfigureAwait(true);

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

                Records.Add(record);
            }

            UpdateTotals();
            StatusMessage = $"Completed. {Records.Count} items.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
#pragma warning restore CA1031
    }

    private void ExportCsv()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            DefaultExt = "csv",
            FileName = "cloc_result",
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture);
        using var writer = new StreamWriter(dialog.FileName, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        using var csv = new CsvWriter(writer, config);
        csv.WriteRecords(Records);

        StatusMessage = $"Saved: {dialog.FileName}";
    }

    private void SelectDirectory()
    {
        var dialog = new OpenFolderDialog();

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        TargetDirectory = dialog.FolderName;
    }

    private void UpdateTotals()
    {
        TotalBlank = Records.Sum(r => r.Blank);
        TotalComment = Records.Sum(r => r.Comment);
        TotalCode = Records.Sum(r => r.Code);
    }

    //--------------------------------------------------------------------------------
    // Setting
    //--------------------------------------------------------------------------------

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
