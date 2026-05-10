namespace ClocView.Models;

using CsvHelper.Configuration.Attributes;

public sealed class ClocRecord
{
    /// <summary>言語別集計時のファイル数（--by-file では null）。</summary>
    [Name("files")]
    [Optional]
    public int? Files { get; set; }

    [Name("language")]
    public string Language { get; set; } = string.Empty;

    [Name("filename")]
    [Optional]
    public string? Filename { get; set; }

    [Name("blank")]
    public int Blank { get; set; }

    [Name("comment")]
    public int Comment { get; set; }

    [Name("code")]
    public int Code { get; set; }

    /// <summary>対象フォルダからの相対パス（表示用）。</summary>
    [Ignore]
    public string RelativePath { get; set; } = string.Empty;
}
