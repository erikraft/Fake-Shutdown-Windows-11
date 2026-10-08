using System.Threading;

namespace FakeShutdown;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using Mutex singleInstance = new(true, "FakeShutdown.SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "A experiência já está em execução.",
                "Fake Shutdown",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}