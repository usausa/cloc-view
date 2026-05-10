namespace ClocView.Services;

public sealed class ClocOption
{
    public bool ByFile { get; set; } = true;

    public string? IncludeLang { get; set; }

    public string? ExcludeDir { get; set; }

    public string? ExcludeExt { get; set; }

    public string? ExcludeContent { get; set; }
}

public sealed class ClocSetting
{
    public string ExecutablePath { get; set; } = string.Empty;

    public ClocOption Option { get; set; } = new();
}
