using System.Drawing;
using System.Windows.Forms;

namespace KeyboardLayoutSwitcher;

internal sealed class MainForm : Form
{
    private readonly ListView _keyboardList = CreateListView();
    private readonly ListView _layoutList = CreateListView();
    private readonly Button _playButton = new() { Text = Localization.Get("play"), AutoSize = true };
    private readonly Button _pauseButton = new() { Text = Localization.Get("pause"), AutoSize = true };
    private readonly Button _saveMappingButton = new() { Text = Localization.Get("save"), AutoSize = true };
    private readonly Button _updateMappingButton = new() { Text = Localization.Get("update"), AutoSize = true };
    private readonly Button _deleteMappingButton = new() { Text = Localization.Get("delete"), AutoSize = true };
    private readonly CheckBox _startWithWindowsCheck = new() { Text = Localization.Get("startup"), AutoSize = true };
    private readonly CheckBox _startMinimizedCheck = new() { Text = Localization.Get("startMinimized"), AutoSize = true };
    private readonly Label _monitoringStatusLabel = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Label _statusLabel = new() { AutoSize = true, ForeColor = Color.DimGray };
    private readonly Label _detectedKeyboardLabel = new() { AutoSize = false, Dock = DockStyle.Fill, AutoEllipsis = true, ForeColor = Color.DimGray };
    private readonly TextBox _testTextBox = new()
    {
        Multiline = true,
        AcceptsReturn = true,
        ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill,
        PlaceholderText = Localization.Get("testPlaceholder")
    };
    private readonly TextBox _logTextBox = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _trayPlayItem = new(Localization.Get("trayActivate"));
    private readonly ToolStripMenuItem _trayPauseItem = new(Localization.Get("trayPause"));
    private readonly ToolStripMenuItem _trayStartupItem = new(Localization.Get("startup"));
    private readonly ToolStripMenuItem _trayStartMinimizedItem = new(Localization.Get("startMinimized"));
    private readonly Icon _applicationIcon;
    private readonly AppConfig _config;
    private IReadOnlyList<KeyboardDevice> _devices = Array.Empty<KeyboardDevice>();
    private IReadOnlyList<KeyboardLayoutInfo> _layouts = Array.Empty<KeyboardLayoutInfo>();
    private string? _lastDetectedDevicePath;
    private bool _loadingSelection;
    private bool _exitRequested;
    private bool _startupVisibilityApplied;

    public MainForm()
    {
        Text = "Keyboard Layout Switcher";
        Width = 1120;
        Height = 760;
        MinimumSize = new Size(860, 620);
        StartPosition = FormStartPosition.CenterScreen;

        _applicationIcon = LoadApplicationIcon();
        Icon = _applicationIcon;
        _config = ConfigStore.Load();
        _notifyIcon = CreateNotifyIcon();

        BuildUi();
        _keyboardList.SelectedIndexChanged += (_, _) => LoadSelectedKeyboardSettings();
        _layoutList.SelectedIndexChanged += (_, _) => UpdateLayoutPreview();
        _startWithWindowsCheck.Checked = StartupManager.IsEnabled(Application.ExecutablePath);
        _startWithWindowsCheck.CheckedChanged += StartWithWindowsCheckChanged;
        _startMinimizedCheck.Checked = _config.StartMinimized;
        _trayStartMinimizedItem.Checked = _config.StartMinimized;
        _startMinimizedCheck.CheckedChanged += (_, _) => SetStartMinimized(_startMinimizedCheck.Checked);
        RefreshMonitoringUi();
    }

    protected override void SetVisibleCore(bool value)
    {
        if (value && !_startupVisibilityApplied)
        {
            _startupVisibilityApplied = true;
            // Raw Input needs a window handle even when starting hidden in the tray.
            CreateHandle();
            InitializeInputMonitoring();
            value = !_config.StartMinimized;
        }

        base.SetVisibleCore(value);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WM_INPUT)
        {
            var keyboardEvent = NativeMethods.ReadKeyboardEvent(m.LParam);
            if (keyboardEvent is { IsKeyDown: true })
            {
                HandleKeyDown(keyboardEvent.Value.DeviceHandle);
            }
        }
        else if (m.Msg == NativeMethods.WM_INPUT_DEVICE_CHANGE)
        {
            BeginInvoke(RefreshDevices);
        }

        base.WndProc(ref m);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_exitRequested)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _applicationIcon.Dispose();
        base.OnFormClosing(e);
    }

    private void BuildUi()
    {
        var instructions = new Label
        {
            Text = Localization.Get("instructions"),
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(14, 12, 14, 4)
        };

        ConfigureKeyboardList();
        ConfigureLayoutList();
        var keyboardGroup = new GroupBox { Text = Localization.Get("keyboardsGroup"), Dock = DockStyle.Fill, Padding = new Padding(10) };
        var layoutGroup = new GroupBox { Text = Localization.Get("layoutsGroup"), Dock = DockStyle.Fill, Padding = new Padding(10) };
        keyboardGroup.Controls.Add(_keyboardList);
        layoutGroup.Controls.Add(_layoutList);

        var selector = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 4, 14, 4),
            ColumnCount = 2,
            RowCount = 1
        };
        selector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        selector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        selector.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        selector.Controls.Add(keyboardGroup, 0, 0);
        selector.Controls.Add(layoutGroup, 1, 0);

        var refreshButton = new Button { Text = Localization.Get("refreshDevices"), AutoSize = true };
        refreshButton.Click += (_, _) => RefreshDevices();
        _playButton.Click += (_, _) => SetMonitoringEnabled(true);
        _pauseButton.Click += (_, _) => SetMonitoringEnabled(false);
        _saveMappingButton.Click += (_, _) => SaveNewMapping();
        _updateMappingButton.Click += (_, _) => UpdateMapping();
        _deleteMappingButton.Click += (_, _) => DeleteMapping();

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 72,
            Padding = new Padding(14, 6, 14, 0),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        actionPanel.Controls.Add(new Label { Text = Localization.Get("automaticSwitching"), AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 7, 3, 0) });
        actionPanel.Controls.Add(_playButton);
        actionPanel.Controls.Add(_pauseButton);
        actionPanel.Controls.Add(_monitoringStatusLabel);
        actionPanel.Controls.Add(new Label { Text = Localization.Get("mapping"), AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(14, 7, 3, 0) });
        actionPanel.Controls.Add(_saveMappingButton);
        actionPanel.Controls.Add(_updateMappingButton);
        actionPanel.Controls.Add(_deleteMappingButton);
        actionPanel.Controls.Add(refreshButton);
        actionPanel.Controls.Add(_startWithWindowsCheck);
        actionPanel.Controls.Add(_startMinimizedCheck);
        actionPanel.Controls.Add(_statusLabel);

        var testGroup = new GroupBox { Text = Localization.Get("testGroup"), Dock = DockStyle.Fill, Padding = new Padding(10) };
        var testLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        testLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        testLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _detectedKeyboardLabel.Text = Localization.Get("detectedHint");
        testLayout.Controls.Add(_detectedKeyboardLabel, 0, 0);
        testLayout.Controls.Add(_testTextBox, 0, 1);
        testGroup.Controls.Add(testLayout);

        var activityGroup = new GroupBox { Text = Localization.Get("activity"), Dock = DockStyle.Fill, Padding = new Padding(10) };
        activityGroup.Controls.Add(_logTextBox);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 0, 0, 10),
            ColumnCount = 1,
            RowCount = 3
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        content.Controls.Add(selector, 0, 0);
        content.Controls.Add(testGroup, 0, 1);
        content.Controls.Add(activityGroup, 0, 2);

        Controls.Add(content);
        Controls.Add(actionPanel);
        Controls.Add(instructions);
    }

    private static ListView CreateListView()
    {
        return new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            MultiSelect = false,
            GridLines = true
        };
    }

    private void ConfigureKeyboardList()
    {
        _keyboardList.Columns.Add(Localization.Get("keyboardColumn"), 270);
        _keyboardList.Columns.Add(Localization.Get("deviceIdColumn"), 370);
        _keyboardList.Columns.Add(Localization.Get("assignmentColumn"), 135);
        _keyboardList.Columns.Add(Localization.Get("detectionColumn"), 105);
    }

    private void ConfigureLayoutList()
    {
        _layoutList.Columns.Add(Localization.Get("layoutColumn"), 300);
        _layoutList.Columns.Add(Localization.Get("idColumn"), 95);
        _layoutList.Columns.Add(Localization.Get("mappedColumn"), 80);
    }

    private void InitializeInputMonitoring()
    {
        try
        {
            NativeMethods.RegisterKeyboardInput(Handle);
            _layouts = NativeMethods.EnumerateInstalledLayouts();
            RefreshDevices();
            Log(Localization.Format("monitoringStarted", _config.IsMonitoringEnabled ? Localization.Get("active").TrimStart('●', ' ') : Localization.Get("paused").TrimStart('●', ' ')));
            Log(Localization.Format("installedLayouts", _layouts.Count, ConfigStore.ConfigPath));
        }
        catch (Exception exception)
        {
            Log(Localization.Format("monitoringError", exception.Message));
            MessageBox.Show(this, exception.Message, Localization.Get("startupErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshDevices()
    {
        try
        {
            var previouslySelectedPath = SelectedKeyboard?.DevicePath;
            _devices = NativeMethods.EnumerateKeyboards();
            RebuildKeyboardList(previouslySelectedPath);
            Log(Localization.Format("devicesDetected", _devices.Count));
        }
        catch (Exception exception)
        {
            Log(Localization.Format("deviceError", exception.Message));
        }
    }

    private void RebuildKeyboardList(string? preferredDevicePath)
    {
        _loadingSelection = true;
        _keyboardList.BeginUpdate();
        _keyboardList.Items.Clear();
        foreach (var device in _devices)
        {
            var item = new ListViewItem(device.DisplayName) { Tag = device };
            item.SubItems.Add(device.Detail);
            item.SubItems.Add(GetAssignmentSummary(device));
            item.SubItems.Add(device.DevicePath.Equals(_lastDetectedDevicePath, StringComparison.OrdinalIgnoreCase) ? Localization.Get("typingNow") : "");
            _keyboardList.Items.Add(item);
        }
        _keyboardList.EndUpdate();
        _loadingSelection = false;

        var itemToSelect = _keyboardList.Items
            .Cast<ListViewItem>()
            .FirstOrDefault(item => (item.Tag as KeyboardDevice)?.DevicePath.Equals(preferredDevicePath, StringComparison.OrdinalIgnoreCase) == true)
            ?? _keyboardList.Items.Cast<ListViewItem>().FirstOrDefault();
        if (itemToSelect is not null)
        {
            itemToSelect.Selected = true;
            itemToSelect.Focused = true;
            itemToSelect.EnsureVisible();
        }
        else
        {
            RebuildLayoutList(null);
        }
    }

    private void LoadSelectedKeyboardSettings()
    {
        if (_loadingSelection)
        {
            return;
        }

        var keyboard = SelectedKeyboard;
        _loadingSelection = true;
        RebuildLayoutList(keyboard);
        _loadingSelection = false;

        _statusLabel.Text = keyboard is null
            ? Localization.Get("selectKeyboardLayout")
            : GetAssignmentStatus(keyboard);
        UpdateMappingActionAvailability();
    }

    private void RebuildLayoutList(KeyboardDevice? keyboard)
    {
        _layoutList.BeginUpdate();
        _layoutList.Items.Clear();
        var mappedLayout = keyboard is null ? null : FindMappedLayout(keyboard);

        foreach (var layout in _layouts)
        {
            var isMapped = layout.Id.Equals(mappedLayout?.Id, StringComparison.OrdinalIgnoreCase);
            var item = new ListViewItem(layout.DisplayName) { Tag = layout };
            item.SubItems.Add(layout.Id);
            item.SubItems.Add(isMapped ? "✓" : "");
            _layoutList.Items.Add(item);
            if (isMapped)
            {
                item.Selected = true;
                item.Focused = true;
            }
        }

        _layoutList.EndUpdate();
    }

    private void SaveNewMapping()
    {
        if (!TryGetSelectedMappingValues(out var keyboard, out var layout))
        {
            return;
        }

        if (_config.Mappings.ContainsKey(keyboard.DevicePath))
        {
            MessageBox.Show(this, Localization.Get("mappingExists"), Localization.Get("mappingExistsTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        PersistMapping(keyboard, layout, Localization.Get("savedAction"));
    }

    private void UpdateMapping()
    {
        if (!TryGetSelectedMappingValues(out var keyboard, out var layout))
        {
            return;
        }

        if (!_config.Mappings.ContainsKey(keyboard.DevicePath))
        {
            MessageBox.Show(this, Localization.Get("mappingMissing"), Localization.Get("mappingMissingTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        PersistMapping(keyboard, layout, Localization.Get("updatedAction"));
    }

    private void DeleteMapping()
    {
        if (SelectedKeyboard is not { } keyboard || !_config.Mappings.ContainsKey(keyboard.DevicePath))
        {
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            Localization.Format("deleteConfirmation", keyboard.DisplayName),
            Localization.Get("deleteMappingTitle"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        _config.Mappings.Remove(keyboard.DevicePath);
        ConfigStore.Save(_config);
        RebuildKeyboardList(keyboard.DevicePath);
        _statusLabel.Text = Localization.Get("mappingDeleted");
        Log(Localization.Format("mappingDeletedLog", keyboard.DisplayName));
    }

    private bool TryGetSelectedMappingValues(out KeyboardDevice keyboard, out KeyboardLayoutInfo layout)
    {
        keyboard = SelectedKeyboard!;
        layout = SelectedLayout!;
        if (keyboard is not null && layout is not null)
        {
            return true;
        }

        MessageBox.Show(this, Localization.Get("selectKeyboardLayout"), Localization.Get("incompleteMappingTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    private void PersistMapping(KeyboardDevice keyboard, KeyboardLayoutInfo layout, string action)
    {
        _config.Mappings[keyboard.DevicePath] = new DeviceMapping
        {
            LayoutId = layout.Id
        };
        ConfigStore.Save(_config);
        RebuildKeyboardList(keyboard.DevicePath);
        _statusLabel.Text = GetAssignmentStatus(keyboard);
        Log(Localization.Format("mappingSaved", action, keyboard.DisplayName, layout.DisplayName));
    }

    private void HandleKeyDown(IntPtr deviceHandle)
    {
        var device = _devices.FirstOrDefault(item => item.Handle == deviceHandle);
        if (device is null)
        {
            return;
        }

        MarkDetectedKeyboard(device);
        if (!_config.IsMonitoringEnabled || !_config.Mappings.TryGetValue(device.DevicePath, out var mapping))
        {
            return;
        }

        var layout = FindLayout(mapping.LayoutId);
        if (layout is null)
        {
            Log(Localization.Format("layoutMissing", device.DisplayName, mapping.LayoutId));
            return;
        }

        if (NativeMethods.IsForegroundKeyboardLayout(layout.Id))
        {
            return;
        }

        if (NativeMethods.SetForegroundKeyboardLayout(layout.Id))
        {
            Log(Localization.Format("layoutRequested", device.DisplayName, layout.DisplayName));
        }
    }

    private void MarkDetectedKeyboard(KeyboardDevice device)
    {
        var changedDevice = !device.DevicePath.Equals(_lastDetectedDevicePath, StringComparison.OrdinalIgnoreCase);
        _lastDetectedDevicePath = device.DevicePath;
        var testBoxHadFocus = _testTextBox.Focused;

        foreach (ListViewItem item in _keyboardList.Items)
        {
            var itemDevice = item.Tag as KeyboardDevice;
            var isDetected = itemDevice?.DevicePath.Equals(device.DevicePath, StringComparison.OrdinalIgnoreCase) == true;
            item.SubItems[3].Text = isDetected ? Localization.Get("typingNow") : "";
            if (isDetected)
            {
                item.Selected = true;
                item.Focused = true;
                item.EnsureVisible();
            }
        }

        _detectedKeyboardLabel.Text = Localization.Format("detected", device.DisplayName, device.Detail);
        if (changedDevice)
        {
            Log(Localization.Format("keyboardDetected", device.DisplayName, device.Detail));
        }

        if (testBoxHadFocus && !_testTextBox.Focused)
        {
            BeginInvoke(() => _testTextBox.Focus());
        }
    }

    private KeyboardLayoutInfo? FindMappedLayout(KeyboardDevice keyboard)
    {
        return _config.Mappings.TryGetValue(keyboard.DevicePath, out var mapping)
            ? FindLayout(mapping.LayoutId)
            : null;
    }

    private KeyboardLayoutInfo? FindLayout(string id)
    {
        return _layouts.FirstOrDefault(layout => layout.Id.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                                                  layout.ActiveHklId.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    private string GetAssignmentSummary(KeyboardDevice device)
    {
        var layout = FindMappedLayout(device);
        if (layout is null)
        {
            return Localization.Get("unassigned");
        }

        return layout.DisplayName;
    }

    private string GetAssignmentStatus(KeyboardDevice device)
    {
        var layout = FindMappedLayout(device);
        if (layout is null)
        {
            return Localization.Get("noAssignment");
        }

        return Localization.Format("mappingStatus", layout.DisplayName);
    }

    private void UpdateLayoutPreview()
    {
        if (!_loadingSelection && SelectedLayout is { } layout)
        {
            _statusLabel.Text = Localization.Format("layoutSelected", layout.DisplayName);
        }

        UpdateMappingActionAvailability();
    }

    private void UpdateMappingActionAvailability()
    {
        var keyboard = SelectedKeyboard;
        var hasMapping = keyboard is not null && _config.Mappings.ContainsKey(keyboard.DevicePath);
        var hasCompleteSelection = keyboard is not null && SelectedLayout is not null;
        _saveMappingButton.Enabled = hasCompleteSelection && !hasMapping;
        _updateMappingButton.Enabled = hasCompleteSelection && hasMapping;
        _deleteMappingButton.Enabled = hasMapping;
    }

    private KeyboardDevice? SelectedKeyboard => _keyboardList.SelectedItems.Count == 1
        ? _keyboardList.SelectedItems[0].Tag as KeyboardDevice
        : null;

    private KeyboardLayoutInfo? SelectedLayout => _layoutList.SelectedItems.Count == 1
        ? _layoutList.SelectedItems[0].Tag as KeyboardLayoutInfo
        : null;

    private NotifyIcon CreateNotifyIcon()
    {
        var menu = new ContextMenuStrip();
        _trayPlayItem.Click += (_, _) => SetMonitoringEnabled(true);
        _trayPauseItem.Click += (_, _) => SetMonitoringEnabled(false);
        _trayStartupItem.Click += (_, _) => SetStartWithWindows(!_trayStartupItem.Checked);
        _trayStartMinimizedItem.Click += (_, _) => SetStartMinimized(!_trayStartMinimizedItem.Checked);
        menu.Items.Add(_trayPlayItem);
        menu.Items.Add(_trayPauseItem);
        menu.Items.Add(_trayStartupItem);
        menu.Items.Add(_trayStartMinimizedItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Localization.Get("show"), null, (_, _) => { Show(); WindowState = FormWindowState.Normal; Activate(); });
        menu.Items.Add(Localization.Get("exit"), null, (_, _) => { _exitRequested = true; Application.Exit(); });

        var icon = new NotifyIcon
        {
            Icon = _applicationIcon,
            Text = "Keyboard Layout Switcher",
            Visible = true,
            ContextMenuStrip = menu
        };
        icon.DoubleClick += (_, _) => { Show(); WindowState = FormWindowState.Normal; Activate(); };
        return icon;
    }

    private void SetStartMinimized(bool enabled)
    {
        if (_config.StartMinimized == enabled)
        {
            return;
        }

        _config.StartMinimized = enabled;
        ConfigStore.Save(_config);
        _startMinimizedCheck.Checked = enabled;
        _trayStartMinimizedItem.Checked = enabled;
    }

    private void SetStartWithWindows(bool enabled)
    {
        try
        {
            StartupManager.SetEnabled(enabled, Application.ExecutablePath);
            _startWithWindowsCheck.Checked = enabled;
            _trayStartupItem.Checked = enabled;
            Log(Localization.Format("startupChanged", enabled ? Localization.Get("enabled") : Localization.Get("disabled")));
        }
        catch (Exception exception)
        {
            _startWithWindowsCheck.CheckedChanged -= StartWithWindowsCheckChanged;
            _startWithWindowsCheck.Checked = !enabled;
            _startWithWindowsCheck.CheckedChanged += StartWithWindowsCheckChanged;
            Log(Localization.Format("startupChangeError", exception.Message));
            MessageBox.Show(this, exception.Message, Localization.Get("startupChangeErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StartWithWindowsCheckChanged(object? sender, EventArgs e)
    {
        SetStartWithWindows(_startWithWindowsCheck.Checked);
    }

    private void SetMonitoringEnabled(bool enabled)
    {
        if (_config.IsMonitoringEnabled == enabled)
        {
            return;
        }

        _config.IsMonitoringEnabled = enabled;
        ConfigStore.Save(_config);
        RefreshMonitoringUi();
        Log(Localization.Format("globalSwitchingChanged", enabled ? Localization.Get("enabled") : Localization.Get("disabled")));
    }

    private void RefreshMonitoringUi()
    {
        var enabled = _config.IsMonitoringEnabled;
        _monitoringStatusLabel.Text = enabled ? Localization.Get("active") : Localization.Get("paused");
        _monitoringStatusLabel.ForeColor = enabled ? Color.ForestGreen : Color.DarkOrange;
        _playButton.Enabled = !enabled;
        _pauseButton.Enabled = enabled;
        _trayPlayItem.Enabled = !enabled;
        _trayPauseItem.Enabled = enabled;
        _trayPlayItem.Checked = enabled;
        _trayPauseItem.Checked = !enabled;
        _trayStartupItem.Checked = StartupManager.IsEnabled(Application.ExecutablePath);
        _notifyIcon.Text = enabled ? Localization.Get("trayActive") : Localization.Get("trayPaused");
    }

    private static Icon LoadApplicationIcon()
    {
        // Icon(string) expects an .ico file, while the application icon is embedded in the executable.
        return Icon.ExtractAssociatedIcon(Application.ExecutablePath) is { } icon
            ? (Icon)icon.Clone()
            : SystemIcons.Application;
    }

    private void Log(string message)
    {
        if (!IsDisposed)
        {
            _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
    }
}
