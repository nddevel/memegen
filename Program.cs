namespace MemeGen;

static class Program
{
    static readonly ApplicationContext Context = new();

    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        var theme = ThemeSettings.Load();
        ThemeSettings.Apply(theme);
        Context.MainForm = CreateForm(new MainForm(theme, args.FirstOrDefault(), null));
        Application.Run(Context);
    }

    static MainForm CreateForm(MainForm form)
    {
        form.ThemeChangeRequested += OnThemeChangeRequested;
        return form;
    }

    // Controls only pick up a new color mode when they're created, so rebuild the
    // window with the same image, text and position.
    static void OnThemeChangeRequested(MainForm old, ThemeChoice theme)
    {
        ThemeSettings.Save(theme);
        ThemeSettings.Apply(theme);
        var next = CreateForm(new MainForm(theme, null, old.DetachState()));
        Context.MainForm = next; // must come before closing the old window, or the app exits
        next.Show();
        old.Close();
    }
}
