namespace ClocView.Services;

using CsvHelper.Configuration.Attributes;

public sealed class ClocRecord
{
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

    [Ignore]
    public string RelativePath { get; set; } = string.Empty;
}
