using Microsoft.Win32;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace KeyboardLayoutSwitcher;

internal static class NativeMethods
{
    public const int WM_INPUT = 0x00FF;
    public const int WM_INPUT_DEVICE_CHANGE = 0x00FE;
    private const int WM_INPUTLANGCHANGEREQUEST = 0x0050;

    private const uint RID_INPUT = 0x10000003;
    private const uint RIDI_DEVICENAME = 0x20000007;
    private const uint RIDEV_INPUTSINK = 0x00000100;
    private const uint RIDEV_DEVNOTIFY = 0x00002000;
    private const uint RIM_TYPEKEYBOARD = 1;
    private const ushort HID_USAGE_PAGE_GENERIC = 0x01;
    private const ushort HID_USAGE_GENERIC_KEYBOARD = 0x06;
    private const ushort RI_KEY_BREAK = 0x01;
    private const uint KLF_ACTIVATE = 0x00000001;
    private const uint KLF_SUBSTITUTE_OK = 0x00000002;

    private const int CR_SUCCESS = 0;
    private const uint CM_DRP_DEVICEDESC = 0x00000001;
    private const uint CM_DRP_FRIENDLYNAME = 0x0000000D;

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICE
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public IntPtr Target;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTDEVICELIST
    {
        public IntPtr Device;
        public uint Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWINPUTHEADER
    {
        public uint Type;
        public uint Size;
        public IntPtr Device;
        public IntPtr WParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    internal readonly record struct RawKeyboardEvent(IntPtr DeviceHandle, bool IsKeyDown);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] devices, uint deviceCount, uint deviceSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputDeviceList([Out] RAWINPUTDEVICELIST[]? devices, ref uint deviceCount, uint deviceSize);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetRawInputDeviceInfo(IntPtr device, uint command, StringBuilder? data, ref uint dataSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetRawInputData(IntPtr rawInput, uint command, [Out] byte[]? data, ref uint dataSize, uint headerSize);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadKeyboardLayout(string keyboardLayoutId, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint threadId);

    [DllImport("user32.dll")]
    private static extern IntPtr PostMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr ActivateKeyboardLayout(IntPtr keyboardLayout, uint flags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Locate_DevNodeW(out uint deviceInstance, string deviceId, uint flags);

    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Get_Parent(out uint parentDeviceInstance, uint deviceInstance, uint flags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Get_DevNode_Registry_PropertyW(
        uint deviceInstance,
        uint property,
        out uint registryDataType,
        StringBuilder? buffer,
        ref uint bufferSize,
        uint flags);

    public static void RegisterKeyboardInput(IntPtr target)
    {
        var devices = new[]
        {
            new RAWINPUTDEVICE
            {
                UsagePage = HID_USAGE_PAGE_GENERIC,
                Usage = HID_USAGE_GENERIC_KEYBOARD,
                Flags = RIDEV_INPUTSINK | RIDEV_DEVNOTIFY,
                Target = target
            }
        };

        if (!RegisterRawInputDevices(devices, (uint)devices.Length, (uint)Marshal.SizeOf<RAWINPUTDEVICE>()))
        {
            throw new InvalidOperationException($"RegisterRawInputDevices failed: {Marshal.GetLastWin32Error()}");
        }
    }

    public static IReadOnlyList<KeyboardDevice> EnumerateKeyboards()
    {
        uint count = 0;
        _ = GetRawInputDeviceList(null, ref count, (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>());
        if (count == 0)
        {
            return Array.Empty<KeyboardDevice>();
        }

        var devices = new RAWINPUTDEVICELIST[count];
        var result = GetRawInputDeviceList(devices, ref count, (uint)Marshal.SizeOf<RAWINPUTDEVICELIST>());
        if (result == uint.MaxValue)
        {
            return Array.Empty<KeyboardDevice>();
        }

        return devices
            .Take((int)result)
            .Where(device => device.Type == RIM_TYPEKEYBOARD)
            .Select(device => CreateKeyboardDevice(device.Device))
            .GroupBy(device => device.DevicePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(device => device.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<KeyboardLayoutInfo> EnumerateInstalledLayouts()
    {
        var substitutes = ReadKeyboardLayoutSubstitutes();
        var layouts = new List<KeyboardLayoutInfo>();

        foreach (var configuredId in ReadPreloadedLayoutIds())
        {
            var effectiveId = substitutes.TryGetValue(configuredId, out var substituteId) ? substituteId : configuredId;
            var handle = LoadKeyboardLayout(configuredId, KLF_SUBSTITUTE_OK);
            var layoutName = ReadLayoutDisplayName(effectiveId) ?? ReadLayoutDisplayName(configuredId);
            var languageName = ReadLanguageDisplayName(configuredId);

            layouts.Add(new KeyboardLayoutInfo
            {
                Id = configuredId,
                ActiveHklId = ToHexId(handle),
                DisplayName = CreateLayoutDisplayName(languageName, layoutName, configuredId),
                Handle = handle
            });
        }

        return layouts
            .GroupBy(layout => layout.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(layout => layout.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static RawKeyboardEvent? ReadKeyboardEvent(IntPtr rawInput)
    {
        uint size = 0;
        var headerSize = (uint)Marshal.SizeOf<RAWINPUTHEADER>();
        if (GetRawInputData(rawInput, RID_INPUT, null, ref size, headerSize) == uint.MaxValue || size < headerSize)
        {
            return null;
        }

        var data = new byte[size];
        if (GetRawInputData(rawInput, RID_INPUT, data, ref size, headerSize) == uint.MaxValue)
        {
            return null;
        }

        var pinned = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            var pointer = pinned.AddrOfPinnedObject();
            var header = Marshal.PtrToStructure<RAWINPUTHEADER>(pointer);
            if (header.Type != RIM_TYPEKEYBOARD)
            {
                return null;
            }

            var keyboard = Marshal.PtrToStructure<RAWKEYBOARD>(IntPtr.Add(pointer, (int)headerSize));
            return new RawKeyboardEvent(header.Device, (keyboard.Flags & RI_KEY_BREAK) == 0);
        }
        finally
        {
            pinned.Free();
        }
    }

    public static bool IsForegroundKeyboardLayout(string layoutId)
    {
        var targetLayout = LoadKeyboardLayout(layoutId, KLF_SUBSTITUTE_OK);
        if (targetLayout == IntPtr.Zero)
        {
            return false;
        }

        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return false;
        }

        var threadId = GetWindowThreadProcessId(foregroundWindow, out _);
        return ToHexId(GetKeyboardLayout(threadId)).Equals(ToHexId(targetLayout), StringComparison.OrdinalIgnoreCase);
    }

    public static bool SetForegroundKeyboardLayout(string layoutId)
    {
        var handle = LoadKeyboardLayout(layoutId, KLF_ACTIVATE | KLF_SUBSTITUTE_OK);
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            return ActivateKeyboardLayout(handle, 0) != IntPtr.Zero;
        }

        return PostMessage(foregroundWindow, WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, handle) != IntPtr.Zero;
    }

    private static KeyboardDevice CreateKeyboardDevice(IntPtr handle)
    {
        var path = GetDeviceName(handle);
        var pnpInstanceId = TryCreatePnpInstanceId(path);
        var pnpDisplayName = pnpInstanceId is null ? null : ReadBestPnpDisplayName(pnpInstanceId);
        var isInternal = path.Contains("ACPI", StringComparison.OrdinalIgnoreCase) ||
                         path.Contains("PNP030", StringComparison.OrdinalIgnoreCase);
        var hardwareIdentifier = ExtractHardwareIdentifier(path);

        return new KeyboardDevice
        {
            Handle = handle,
            DevicePath = path,
            DisplayName = CreateKeyboardDisplayName(pnpDisplayName, isInternal, hardwareIdentifier),
            Detail = pnpInstanceId is null ? path : $"{pnpInstanceId} · {hardwareIdentifier ?? "identificador PnP"}"
        };
    }

    private static string GetDeviceName(IntPtr device)
    {
        uint size = 0;
        _ = GetRawInputDeviceInfo(device, RIDI_DEVICENAME, null, ref size);
        if (size == 0)
        {
            return device.ToString();
        }

        var builder = new StringBuilder((int)size + 1);
        _ = GetRawInputDeviceInfo(device, RIDI_DEVICENAME, builder, ref size);
        return builder.ToString();
    }

    private static IReadOnlyList<string> ReadPreloadedLayoutIds()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Keyboard Layout\Preload");
        if (key is null)
        {
            return Array.Empty<string>();
        }

        return key.GetValueNames()
            .OrderBy(name => int.TryParse(name, out var order) ? order : int.MaxValue)
            .Select(name => key.GetValue(name) as string)
            .Where(value => TryNormalizeLayoutId(value, out _))
            .Select(value => NormalizeLayoutId(value!))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, string> ReadKeyboardLayoutSubstitutes()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Keyboard Layout\Substitutes");
        if (key is null)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return key.GetValueNames()
            .Select(name => new { Name = name, Value = key.GetValue(name) as string })
            .Where(entry => TryNormalizeLayoutId(entry.Name, out _) && TryNormalizeLayoutId(entry.Value, out _))
            .ToDictionary(
                entry => NormalizeLayoutId(entry.Name),
                entry => NormalizeLayoutId(entry.Value!),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string? ReadLayoutDisplayName(string layoutId)
    {
        using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts\{layoutId}");
        return key?.GetValue("Layout Text") as string;
    }

    private static string ReadLanguageDisplayName(string layoutId)
    {
        if (!uint.TryParse(layoutId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var numericId))
        {
            return layoutId;
        }

        try
        {
            return CultureInfo.GetCultureInfo((int)(numericId & 0xFFFF)).DisplayName;
        }
        catch (CultureNotFoundException)
        {
            return $"Idioma 0x{numericId & 0xFFFF:X4}";
        }
    }

    private static string CreateLayoutDisplayName(string languageName, string? layoutName, string layoutId)
    {
        return string.IsNullOrWhiteSpace(layoutName) || layoutName.Equals(languageName, StringComparison.CurrentCultureIgnoreCase)
            ? $"{languageName} [{layoutId}]"
            : $"{languageName} — {layoutName} [{layoutId}]";
    }

    private static bool TryNormalizeLayoutId(string? value, out uint numericId)
    {
        return uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out numericId);
    }

    private static string NormalizeLayoutId(string value)
    {
        _ = TryNormalizeLayoutId(value, out var numericId);
        return numericId.ToString("X8");
    }

    private static string ToHexId(IntPtr handle)
    {
        return unchecked((uint)handle.ToInt64()).ToString("X8");
    }

    private static string? TryCreatePnpInstanceId(string rawInputPath)
    {
        var path = rawInputPath.TrimStart('\\', '?');
        var segments = path.Split('#');
        return segments.Length >= 3 ? string.Join("\\", segments.Take(3)) : null;
    }

    private static string? ReadBestPnpDisplayName(string pnpInstanceId)
    {
        if (CM_Locate_DevNodeW(out var deviceInstance, pnpInstanceId, 0) != CR_SUCCESS)
        {
            return null;
        }

        string? fallback = null;
        for (var level = 0; level < 5; level++)
        {
            var friendlyName = ReadDeviceNodeProperty(deviceInstance, CM_DRP_FRIENDLYNAME);
            var deviceDescription = ReadDeviceNodeProperty(deviceInstance, CM_DRP_DEVICEDESC);
            var candidate = friendlyName ?? deviceDescription;
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                fallback ??= candidate;
                if (!IsGenericKeyboardName(candidate))
                {
                    return candidate;
                }
            }

            if (CM_Get_Parent(out deviceInstance, deviceInstance, 0) != CR_SUCCESS)
            {
                break;
            }
        }

        return fallback;
    }

    private static string? ReadDeviceNodeProperty(uint deviceInstance, uint property)
    {
        uint size = 0;
        if (CM_Get_DevNode_Registry_PropertyW(deviceInstance, property, out _, null, ref size, 0) != CR_SUCCESS || size == 0)
        {
            return null;
        }

        var buffer = new StringBuilder((int)(size / sizeof(char)) + 1);
        if (CM_Get_DevNode_Registry_PropertyW(deviceInstance, property, out _, buffer, ref size, 0) != CR_SUCCESS)
        {
            return null;
        }

        var value = buffer.ToString().Trim();
        var separator = value.LastIndexOf(';');
        return separator >= 0 ? value[(separator + 1)..].Trim() : value;
    }

    private static bool IsGenericKeyboardName(string value)
    {
        return value.Contains("HID Keyboard Device", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("HID-compliant keyboard", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("USB Input Device", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("USB Composite Device", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("Keyboard Device", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateKeyboardDisplayName(string? pnpDisplayName, bool isInternal, string? hardwareIdentifier)
    {
        var kind = isInternal ? "Teclado interno" : "Teclado externo";
        if (string.IsNullOrWhiteSpace(pnpDisplayName) || IsGenericKeyboardName(pnpDisplayName))
        {
            return hardwareIdentifier is null ? kind : $"{kind} — {hardwareIdentifier}";
        }

        return isInternal ? $"{kind} — {pnpDisplayName}" : pnpDisplayName;
    }

    private static string? ExtractHardwareIdentifier(string path)
    {
        var upper = path.ToUpperInvariant();
        var vidIndex = upper.IndexOf("VID_", StringComparison.Ordinal);
        var pidIndex = upper.IndexOf("PID_", StringComparison.Ordinal);
        if (vidIndex < 0 || pidIndex < 0)
        {
            return null;
        }

        var vid = path.Substring(vidIndex, Math.Min(8, path.Length - vidIndex));
        var pid = path.Substring(pidIndex, Math.Min(8, path.Length - pidIndex));
        return $"{vid}, {pid}";
    }
}
