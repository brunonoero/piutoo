using piootooapp.clientform.Shell;

namespace piootooapp.clientform;

static class Program
{
    /// <summary>
    ///  Punto di ingresso. Avvia la console nuova (<see cref="MainShellForm"/>); la vecchia
    ///  <see cref="WorkspaceBacktestingForm"/> resta raggiungibile dal menu File.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainShellForm());
    }
}
