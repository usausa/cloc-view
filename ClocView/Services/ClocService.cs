namespace ClocView.Services;

using System.Diagnostics;

using CsvHelper;
using CsvHelper.Configuration;

public sealed class ClocService
{
    private readonly ClocSetting settings;

    public ClocService(ClocSetting settings)
    {
        this.settings = settings;
    }

    public async Task<List<ClocRecord>> ExecuteAsync(string targetDirectory, CancellationToken cancel = default)
    {
        var executable = string.IsNullOrWhiteSpace(settings.ExecutablePath) ? "cloc" : settings.ExecutablePath;

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        BuildArguments(startInfo.ArgumentList, targetDirectory);

        using var process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancel).ConfigureAwait(false);
        await process.WaitForExitAsync(cancel).ConfigureAwait(false);

        return ParseCsv(output);
    }

    private void BuildArguments(Collection<string> args, string targetDirectory)
    {
        args.Add("--csv");

        var opt = settings.Option;

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
        var lines = csv.Split('\n');
        var headerIndex = Array.FindIndex(
            lines,
            l =>
            {
                var trimmed = l.TrimStart();
                return trimmed.StartsWith("language,", StringComparison.OrdinalIgnoreCase) ||
                       trimmed.StartsWith("files,", StringComparison.OrdinalIgnoreCase);
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
            BadDataFound = null
        };

        using var reader = new StringReader(csvBody);
        using var csvReader = new CsvReader(reader, config);

        var records = new List<ClocRecord>();

        csvReader.Read();
        csvReader.ReadHeader();

        while (csvReader.Read())
        {
            var language = csvReader.GetField("language") ?? string.Empty;
            if (String.Equals(language, "SUM", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

#pragma warning disable CA1031
            try
            {
                records.Add(csvReader.GetRecord<ClocRecord>());
            }
            catch
            {
                // Ignore
            }
#pragma warning restore CA1031
        }

        return records;
    }
}
