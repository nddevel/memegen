namespace MemeGen;

enum ThemeChoice { System, Light, Dark }

/// <summary>Remembers the theme choice in %APPDATA%\MemeGen\theme.txt.</summary>
static class ThemeSettings
{
    static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MemeGen", "theme.txt");

    public static ThemeChoice Load()
    {
        try
        {
            if (File.Exists(FilePath) && Enum.TryParse(File.ReadAllText(FilePath).Trim(), out ThemeChoice t))
                return t;
        }
        catch { /* unreadable settings: fall back to default */ }
        return ThemeChoice.System;
    }

    public static void Save(ThemeChoice theme)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, theme.ToString());
        }
        catch { /* not critical */ }
    }

#pragma warning disable WFO5001 // dark mode API is still marked experimental
    public static void Apply(ThemeChoice theme) => Application.SetColorMode(theme switch
    {
        ThemeChoice.Light => SystemColorMode.Classic,
        ThemeChoice.Dark => SystemColorMode.Dark,
        _ => SystemColorMode.System,
    });
#pragma warning restore WFO5001
}

/// <summary>What carries over when the window is rebuilt after a theme change.</summary>
sealed class SavedWindow
{
    public Image? Source;
    public string? SourcePath;
    public string TopText = "";
    public string BottomText = "";
    public Rectangle Bounds;
    public FormWindowState FormState;
}
