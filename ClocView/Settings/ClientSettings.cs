namespace ClocView.Settings;

public sealed class ClocOptions
{
    /// <summary>ファイル単位で集計する（デフォルト有効）。</summary>
    public bool ByFile { get; set; } = true;

    public string? IncludeLang { get; set; }

    public string? ExcludeDir { get; set; }

    public string? ExcludeExt { get; set; }

    public string? ExcludeContent { get; set; }
}

public sealed class ClocSettings
{
    /// <summary>cloc 実行ファイルのパス。空の場合は PATH から検索する。</summary>
    public string ExecutablePath { get; set; } = string.Empty;

    public ClocOptions Options { get; set; } = new();
}

public sealed class ClientSettings
{
    public ClocSettings Cloc { get; set; } = new();
}
