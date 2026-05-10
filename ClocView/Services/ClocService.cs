namespace ClocView.Services;

using System.Diagnostics;
using System.Text.Json;

using ClocView.Models;
using ClocView.Settings;

using CsvHelper;
using CsvHelper.Configuration;

public sealed class ClocService
{
    private readonly ClocSettings settings;

    public ClocService(ClocSettings settings)
    {
        this.settings = settings;
    }

    /// <summary>指定フォルダに対して cloc を実行し、結果レコードの一覧を返す。</summary>
    public async Task<List<ClocRecord>> ExecuteAsync(string targetDirectory, CancellationToken ct = default)
    {
        var executable = string.IsNullOrWhiteSpace(settings.ExecutablePath) ? "cloc" : settings.ExecutablePath;

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };

        // ArgumentList に1引数ずつ追加することで、OS側のエスケープ処理に任せる
        BuildArguments(startInfo.ArgumentList, targetDirectory);

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);

        return ParseCsv(output);
    }

    private void BuildArguments(Collection<string> args, string targetDirectory)
    {
        args.Add("--csv");

        var opt = settings.Options;

        if (opt.ByFile)
        {
            args.Add("--by-file");
        }

        if (!string.IsNullOrWhiteSpace(opt.IncludeLang))
        {
            args.Add($"--include-lang={opt.IncludeLang}");
        }

        if (!string.IsNullOrWhiteSpace(opt.ExcludeDir))
        {
            args.Add($"--exclude-dir={opt.ExcludeDir}");
        }

        if (!string.IsNullOrWhiteSpace(opt.ExcludeExt))
        {
            args.Add($"--exclude-ext={opt.ExcludeExt}");
        }

        if (!string.IsNullOrWhiteSpace(opt.ExcludeContent))
        {
            args.Add($"--exclude-content={opt.ExcludeContent}");
        }

        args.Add(targetDirectory);
    }

    private static List<ClocRecord> ParseCsv(string csv)
    {
        // cloc --csv の出力先頭には "github.com/AlDanial/cloc..." のような統計行が付く。
        // --by-file なし: "files,language,..."  --by-file あり: "language,filename,..."
        // どちらも "language" 列を含むのでその行をヘッダとして探す。
        var lines = csv.Split('\n');
        var headerIndex = Array.FindIndex(
            lines,
            l =>
            {
                var trimmed = l.TrimStart();
                return trimmed.StartsWith("language,", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("files,", StringComparison.OrdinalIgnoreCase);
            });

        if (headerIndex < 0)
        {
            return [];
        }

        var csvBody = string.Join('\n', lines.Skip(headerIndex));

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
        };

        using var reader = new StringReader(csvBody);
        using var csvReader = new CsvReader(reader, config);

        var records = new List<ClocRecord>();

        csvReader.Read();
        csvReader.ReadHeader();

        while (csvReader.Read())
        {
            // SUM 行はスキップ
            var language = csvReader.GetField("language") ?? string.Empty;
            if (string.Equals(language, "SUM", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                records.Add(csvReader.GetRecord<ClocRecord>());
            }
            catch
            {
                // パース失敗行は無視して継続
            }
        }

        return records;
    }
}
