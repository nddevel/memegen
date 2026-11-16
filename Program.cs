namespace MemeGen;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        // Follow the Windows light/dark app setting (dark controls + dark title bar).
#pragma warning disable WFO5001 // dark mode API is still marked experimental
        Application.SetColorMode(SystemColorMode.System);
#pragma warning restore WFO5001
        Application.Run(new MainForm(args.FirstOrDefault()));
    }
}
