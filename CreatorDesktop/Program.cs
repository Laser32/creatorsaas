using CreatorDesktop.Services;

namespace CreatorDesktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        RunLog.WriteSessionHeader(
            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?");

        // Catch any unhandled exception and show it instead of dying silently.
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogAndShow(e.ExceptionObject as Exception);
        Application.ThreadException += (_, e) => LogAndShow(e.Exception);

        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            LogAndShow(ex);
        }
    }

    private static void LogAndShow(Exception? ex)
    {
        if (ex == null) return;
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CreatorDesktop");
            Directory.CreateDirectory(dir);
            var logPath = Path.Combine(dir, "crash.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\r\n{ex}\r\n\r\n");
            RunLog.WriteBlock("ABSTURZ:", ex.ToString());
            MessageBox.Show(
                $"CreatorDesktop ist abgestürzt:\r\n\r\n{ex.GetType().Name}: {ex.Message}\r\n\r\n" +
                $"Volle Details: {logPath}",
                "Fehler beim Start", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch { }
    }
}
