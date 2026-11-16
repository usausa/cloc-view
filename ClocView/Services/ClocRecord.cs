namespace ClocView.Services;

using CsvHelper;

[CsvRecord]
public sealed partial class ClocRecord
{
    [CsvColumn("language")]
    public string Language { get; set; } = string.Empty;

    [CsvColumn("filename", Optional = true)]
    public string? Filename { get; set; }

    [CsvColumn("blank")]
    public int Blank { get; set; }

    [CsvColumn("comment")]
    public int Comment { get; set; }

    [CsvColumn("code")]
    public int Code { get; set; }

    [CsvIgnore]
    public string RelativePath { get; set; } = string.Empty;
}
