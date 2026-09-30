using System.Text.Json;
using System.Text.Json.Serialization;

namespace KeyboardLayoutSwitcher;

internal sealed class AppConfig
{
    public bool IsMonitoringEnabled { get; set; } = true;
    public bool StartMinimized { get; set; }
    public Dictionary<string, DeviceMapping> Mappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class DeviceMapping
{
    public string LayoutId { get; set; } = "";
}

internal sealed class KeyboardDevice
{
    public IntPtr Handle { get; init; }
    public string DevicePath { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Detail { get; init; } = "";

    public override string ToString() => DisplayName;
}

internal sealed class KeyboardLayoutInfo
{
    // Keyboard layout identifier (KLID) used by LoadKeyboardLayout, e.g. 00000409.
    public string Id { get; init; } = "";
    // The HKL that Windows currently assigned while loading this KLID.
    public string ActiveHklId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public IntPtr Handle { get; init; }

    public override string ToString() => DisplayName;
}

internal static class ConfigStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KeyboardLayoutSwitcher",
        "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json, Options) ?? new AppConfig();
            }
        }
        catch
        {
            // A damaged or partially written configuration should not prevent startup.
        }

        return new AppConfig();
    }

    public static void Save(AppConfig config)
    {
        var directory = Path.GetDirectoryName(ConfigPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = ConfigPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(config, Options));
        File.Move(temporaryPath, ConfigPath, true);
    }
}
