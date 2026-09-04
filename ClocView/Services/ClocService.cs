namespace ClocView.Services;

using System.ComponentModel;
using System.Diagnostics;

using CsvHelper;
using CsvHelper.Configuration;

public sealed record ClocResult(IReadOnlyList<ClocRecord> Records, int SkippedRows);

public sealed class ClocService
{
    private readonly ClocSetting settings;

    public ClocService(ClocSetting settings)
    {
        this.settings = settings;
    }

    public async Task<ClocResult> ExecuteAsync(string targetDirectory, CancellationToken cancel = default)
    {
        var executable = String.IsNullOrWhiteSpace(settings.ExecutablePath) ? "cloc" : settings.ExecutablePath;

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = targetDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        BuildArguments(startInfo.ArgumentList, targetDirectory);

        using var process = new Process();
        process.StartInfo = startInfo;

        try
        {
            process.Start();
        }
        catch (Win32Exception e)
        {
            throw new InvalidOperationException($"cloc is not found. Set Cloc:ExecutablePath. executable=[{executable}]", e);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancel);
        var stderrTask = process.StandardError.ReadToEndAsync(cancel);
        var streams = await Task.WhenAll(stdoutTask, stderrTask).ConfigureAwait(false);
        await process.WaitForExitAsync(cancel).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"cloc failed. exitCode=[{process.ExitCode}], error=[{streams[1].Trim()}]");
        }

        var (records, skipped) = ParseCsv(streams[0]);
        return new ClocResult(ApplyExcludeSegmentPrefix(records), skipped);
    }

    private void BuildArguments(Collection<string> args, string targetDirectory)
    {
        args.Add("--csv");

        var opt = settings.Option;

        if (opt.ByFile)
        {
            args.Add("--by-file");
        }

        if (!String.IsNullOrWhiteSpace(opt.IncludeLang))
        {
            args.Add($"--include-lang={opt.IncludeLang}");
        }

        if (!String.IsNullOrWhiteSpace(opt.ExcludeDir))
        {
            args.Add($"--exclude-dir={opt.ExcludeDir}");
        }

        if (!String.IsNullOrWhiteSpace(opt.ExcludeExt))
        {
            args.Add($"--exclude-ext={opt.ExcludeExt}");
        }

        if (!String.IsNullOrWhiteSpace(opt.ExcludeContent))
        {
            args.Add($"--exclude-content={opt.ExcludeContent}");
        }

        args.Add(targetDirectory);
    }

    private List<ClocRecord> ApplyExcludeSegmentPrefix(List<ClocRecord> records)
    {
        var prefix = settings.Option.ExcludePrefix;
        if (String.IsNullOrWhiteSpace(prefix))
        {
            return records;
        }

        var prefixes = prefix.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (prefixes.Length == 0)
        {
            return records;
        }

#pragma warning disable IDE0028
        return records.Where(r => !PathHasSegmentWithPrefix(r.Filename ?? string.Empty, prefixes)).ToList();
#pragma warning restore IDE0028
    }

    private static bool PathHasSegmentWithPrefix(string path, string[] prefixes)
    {
        var segments = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(seg => prefixes.Any(p => seg.StartsWith(p, StringComparison.OrdinalIgnoreCase)));
    }

    private static (List<ClocRecord> Records, int Skipped) ParseCsv(string csv)
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
            return ([], 0);
        }

        var csvBody = String.Join('\n', lines.Skip(headerIndex));

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var reader = new StringReader(csvBody);
        using var csvReader = new CsvReader(reader, config);

        var records = new List<ClocRecord>();
        var skipped = 0;

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
                skipped++;
            }
#pragma warning restore CA1031
        }

        return (records, skipped);
    }
}
