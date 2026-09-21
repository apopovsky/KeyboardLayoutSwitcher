using Microsoft.Win32;

namespace KeyboardLayoutSwitcher;

internal static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "KeyboardLayoutSwitcher";

    public static bool IsEnabled(string executablePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        var configuredCommand = key?.GetValue(RunValueName) as string;
        return !string.IsNullOrWhiteSpace(configuredCommand) &&
               configuredCommand.Contains(executablePath, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled, string executablePath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true)
            ?? throw new InvalidOperationException("No se pudo abrir la configuración de inicio de Windows.");

        if (enabled)
        {
            key.SetValue(RunValueName, $"\"{executablePath}\"");
        }
        else
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }
}
