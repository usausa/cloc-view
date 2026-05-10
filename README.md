# cloc-view

A Windows desktop application that displays [cloc](https://github.com/AlDanial/cloc) results in a sortable grid.

## Configuration (`appsettings.json`)

Customize behavior via `appsettings.json` located in the same folder as the executable.

```json
{
  "Cloc": {
    "ExecutablePath": "",
    "Options": {
      "ByFile": true,
      "IncludeLang": "C#,XAML,Razor,SQL,JavaScript,CSS",
      "ExcludeDir": "lib,Sandbox",
      "ExcludeExt": "min.js,min.css",
      "ExcludeContent": "auto-generated"
    }
  }
}
```

| Key | Default | Description |
|-----|---------|-------------|
| `Cloc.ExecutablePath` | `""` | Path to the cloc executable. Searches PATH if empty. |
| `Cloc.Options.ByFile` | `true` | Passes `--by-file` to cloc (per-file breakdown) |
| `Cloc.Options.IncludeLang` | — | Comma-separated language list passed to `--include-lang` |
| `Cloc.Options.ExcludeDir` | — | Comma-separated directory names passed to `--exclude-dir` |
| `Cloc.Options.ExcludeExt` | — | Comma-separated extensions passed to `--exclude-ext` |
| `Cloc.Options.ExcludeContent` | — | Regex passed to `--exclude-content` |
