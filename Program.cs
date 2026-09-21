using System.Windows.Forms;

namespace KeyboardLayoutSwitcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.ThreadException += (_, args) => ReportUnhandledException(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                ReportUnhandledException(exception);
            }
        };

        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception exception)
        {
            ReportUnhandledException(exception);
        }
    }

    private static void ReportUnhandledException(Exception exception)
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KeyboardLayoutSwitcher");
        Directory.CreateDirectory(directory);
        File.AppendAllText(
            Path.Combine(directory, "startup-errors.log"),
            $"[{DateTimeOffset.Now:O}]{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
    }
}
