using System.Globalization;

namespace KeyboardLayoutSwitcher;

internal static class Localization
{
    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        ["play"] = "▶ Play",
        ["pause"] = "⏸ Pause",
        ["save"] = "Save",
        ["update"] = "Update",
        ["delete"] = "Delete",
        ["startup"] = "Start with Windows",
        ["testPlaceholder"] = "Type here with the keyboard you want to identify and test...",
        ["trayActivate"] = "▶ Enable automatic switching",
        ["trayPause"] = "⏸ Pause automatic switching",
        ["show"] = "Show",
        ["exit"] = "Exit",
        ["instructions"] = "Choose a physical keyboard or type in the test area to detect it. Installed layouts are shown on the right.",
        ["keyboardsGroup"] = "Detected physical keyboards",
        ["layoutsGroup"] = "Windows layouts",
        ["refreshDevices"] = "Refresh devices",
        ["automaticSwitching"] = "Automatic switching:",
        ["mapping"] = "Mapping:",
        ["testGroup"] = "Test and identify keyboard",
        ["detectedHint"] = "Type in the box: the correct keyboard interface will be selected automatically.",
        ["activity"] = "Activity",
        ["keyboardColumn"] = "Keyboard",
        ["deviceIdColumn"] = "Device identifier",
        ["assignmentColumn"] = "Assignment",
        ["detectionColumn"] = "Detection",
        ["layoutColumn"] = "Layout",
        ["idColumn"] = "ID",
        ["mappedColumn"] = "Mapped",
        ["active"] = "● Active",
        ["paused"] = "● Paused",
        ["typingNow"] = "Typing now",
        ["unassigned"] = "Unassigned",
        ["noAssignment"] = "This keyboard does not have an assignment yet.",
        ["mappingStatus"] = "Mapping: {0}",
        ["layoutSelected"] = "Layout selected: {0}",
        ["detected"] = "Detected: {0} — {1}",
        ["monitoringStarted"] = "Monitoring started: {0}.",
        ["installedLayouts"] = "Installed layouts: {0}. Configuration: {1}",
        ["devicesDetected"] = "Devices detected: {0}.",
        ["monitoringError"] = "ERROR starting monitoring: {0}",
        ["deviceError"] = "ERROR enumerating keyboards: {0}",
        ["startupErrorTitle"] = "Could not start",
        ["mappingExists"] = "This keyboard already has a mapping. Use Update to replace it.",
        ["mappingExistsTitle"] = "Existing mapping",
        ["mappingMissing"] = "This keyboard does not have a mapping yet. Use Save to create one.",
        ["mappingMissingTitle"] = "Missing mapping",
        ["deleteConfirmation"] = "Delete the mapping for {0}?",
        ["deleteMappingTitle"] = "Delete mapping",
        ["mappingDeleted"] = "Mapping deleted.",
        ["mappingDeletedLog"] = "Mapping deleted: {0}.",
        ["savedAction"] = "saved",
        ["updatedAction"] = "updated",
        ["selectKeyboardLayout"] = "Select a keyboard and a layout.",
        ["incompleteMappingTitle"] = "Incomplete mapping",
        ["mappingSaved"] = "Mapping {0}: {1} → {2}.",
        ["layoutMissing"] = "The mapping for {0} uses a layout that is no longer installed: {1}.",
        ["layoutRequested"] = "Layout requested: {0} → {1}.",
        ["keyboardDetected"] = "Keyboard detected while typing: {0} ({1}).",
        ["startupChanged"] = "Start with Windows: {0}.",
        ["enabled"] = "enabled",
        ["disabled"] = "disabled",
        ["startupChangeError"] = "ERROR configuring startup with Windows: {0}",
        ["startupChangeErrorTitle"] = "Could not change automatic startup",
        ["globalSwitchingChanged"] = "Global automatic switching: {0}.",
        ["trayActive"] = "Keyboard Layout Switcher — Active",
        ["trayPaused"] = "Keyboard Layout Switcher — Paused"
    };

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Translations =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["es"] = Create(
                ("save", "Guardar"), ("update", "Actualizar"), ("delete", "Borrar"), ("startup", "Iniciar con Windows"),
                ("testPlaceholder", "Escribí aquí con el teclado que querés identificar y probar..."),
                ("trayActivate", "▶ Activar cambio automático"), ("trayPause", "⏸ Pausar cambio automático"),
                ("show", "Mostrar"), ("exit", "Salir"),
                ("instructions", "Elegí un teclado físico o escribí en el área de prueba para detectarlo. A la derecha se muestran los layouts instalados."),
                ("keyboardsGroup", "Teclados físicos detectados"), ("layoutsGroup", "Layouts de Windows"),
                ("refreshDevices", "Actualizar dispositivos"), ("automaticSwitching", "Cambio automático:"), ("mapping", "Mapping:"),
                ("testGroup", "Probar e identificar teclado"),
                ("detectedHint", "Escribí en el cuadro: se seleccionará automáticamente la interfaz correcta del teclado."),
                ("activity", "Actividad"), ("keyboardColumn", "Teclado"), ("deviceIdColumn", "Identificador de dispositivo"),
                ("assignmentColumn", "Asignación"), ("detectionColumn", "Detección"), ("layoutColumn", "Layout"),
                ("mappedColumn", "Mapeado"), ("active", "● Activo"), ("paused", "● Pausado"),
                ("typingNow", "Escribiendo ahora"), ("unassigned", "Sin asignación"),
                ("noAssignment", "Este teclado todavía no tiene una asignación."), ("mappingStatus", "Mapping: {0}"),
                ("layoutSelected", "Layout seleccionado: {0}"), ("detected", "Detectado: {0} — {1}"),
                ("monitoringStarted", "Monitoreo iniciado: {0}."), ("installedLayouts", "Layouts instalados: {0}. Configuración: {1}"),
                ("devicesDetected", "Dispositivos detectados: {0}."), ("monitoringError", "ERROR al iniciar el monitoreo: {0}"),
                ("deviceError", "ERROR al enumerar teclados: {0}"), ("startupErrorTitle", "No se pudo iniciar"),
                ("mappingExists", "Este teclado ya tiene un mapping. Usá Actualizar para reemplazarlo."), ("mappingExistsTitle", "Mapping existente"),
                ("mappingMissing", "Este teclado todavía no tiene un mapping. Usá Guardar para crearlo."), ("mappingMissingTitle", "Mapping inexistente"),
                ("deleteConfirmation", "¿Borrar el mapping de {0}?"), ("deleteMappingTitle", "Borrar mapping"),
                ("mappingDeleted", "Mapping borrado."), ("mappingDeletedLog", "Mapping borrado: {0}."),
                ("savedAction", "guardado"), ("updatedAction", "actualizado"),
                ("selectKeyboardLayout", "Seleccioná un teclado y un layout."), ("incompleteMappingTitle", "Mapping incompleto"),
                ("mappingSaved", "Mapping {0}: {1} → {2}."), ("layoutMissing", "La asignación de {0} usa un layout que ya no está instalado: {1}."),
                ("layoutRequested", "Layout solicitado: {0} → {1}."), ("keyboardDetected", "Teclado detectado al escribir: {0} ({1})."),
                ("startupChanged", "Inicio con Windows: {0}."), ("enabled", "activado"), ("disabled", "desactivado"),
                ("startupChangeError", "ERROR al configurar el inicio con Windows: {0}"),
                ("startupChangeErrorTitle", "No se pudo cambiar el inicio automático"),
                ("globalSwitchingChanged", "Cambio automático global: {0}."), ("trayActive", "Keyboard Layout Switcher — Activo"),
                ("trayPaused", "Keyboard Layout Switcher — Pausado")),
            ["fr"] = Create(("save", "Enregistrer"), ("update", "Mettre à jour"), ("delete", "Supprimer"), ("startup", "Démarrer avec Windows"), ("show", "Afficher"), ("exit", "Quitter"), ("refreshDevices", "Actualiser les périphériques"), ("automaticSwitching", "Changement automatique :"), ("mapping", "Association :"), ("keyboardsGroup", "Claviers physiques détectés"), ("layoutsGroup", "Dispositions Windows"), ("testGroup", "Tester et identifier le clavier"), ("activity", "Activité"), ("active", "● Actif"), ("paused", "● En pause"), ("unassigned", "Non associé"), ("typingNow", "Saisie en cours"), ("mappedColumn", "Associé")),
            ["de"] = Create(("save", "Speichern"), ("update", "Aktualisieren"), ("delete", "Löschen"), ("startup", "Mit Windows starten"), ("show", "Anzeigen"), ("exit", "Beenden"), ("refreshDevices", "Geräte aktualisieren"), ("automaticSwitching", "Automatischer Wechsel:"), ("mapping", "Zuordnung:"), ("keyboardsGroup", "Erkannte physische Tastaturen"), ("layoutsGroup", "Windows-Tastaturlayouts"), ("testGroup", "Tastatur testen und identifizieren"), ("activity", "Aktivität"), ("active", "● Aktiv"), ("paused", "● Pausiert"), ("unassigned", "Nicht zugeordnet"), ("typingNow", "Wird verwendet"), ("mappedColumn", "Zugeordnet")),
            ["it"] = Create(("save", "Salva"), ("update", "Aggiorna"), ("delete", "Elimina"), ("startup", "Avvia con Windows"), ("show", "Mostra"), ("exit", "Esci"), ("refreshDevices", "Aggiorna dispositivi"), ("automaticSwitching", "Cambio automatico:"), ("mapping", "Associazione:"), ("keyboardsGroup", "Tastiere fisiche rilevate"), ("layoutsGroup", "Layout di Windows"), ("testGroup", "Test e identificazione tastiera"), ("activity", "Attività"), ("active", "● Attivo"), ("paused", "● In pausa"), ("unassigned", "Non assegnata"), ("typingNow", "In uso"), ("mappedColumn", "Associato")),
            ["pt"] = Create(("save", "Guardar"), ("update", "Atualizar"), ("delete", "Excluir"), ("startup", "Iniciar com o Windows"), ("show", "Mostrar"), ("exit", "Sair"), ("refreshDevices", "Atualizar dispositivos"), ("automaticSwitching", "Troca automática:"), ("mapping", "Mapeamento:"), ("keyboardsGroup", "Teclados físicos detectados"), ("layoutsGroup", "Layouts do Windows"), ("testGroup", "Testar e identificar teclado"), ("activity", "Atividade"), ("active", "● Ativo"), ("paused", "● Pausado"), ("unassigned", "Sem atribuição"), ("typingNow", "Digitando agora"), ("mappedColumn", "Mapeado")),
            ["ja"] = Create(("save", "保存"), ("update", "更新"), ("delete", "削除"), ("startup", "Windows と同時に起動"), ("show", "表示"), ("exit", "終了"), ("refreshDevices", "デバイスを更新"), ("automaticSwitching", "自動切り替え:"), ("mapping", "割り当て:"), ("keyboardsGroup", "検出された物理キーボード"), ("layoutsGroup", "Windows レイアウト"), ("testGroup", "キーボードのテストと識別"), ("activity", "アクティビティ"), ("active", "● 有効"), ("paused", "● 一時停止"), ("unassigned", "未割り当て"), ("typingNow", "入力中"), ("mappedColumn", "割り当て済み")),
            ["ko"] = Create(("save", "저장"), ("update", "업데이트"), ("delete", "삭제"), ("startup", "Windows와 함께 시작"), ("show", "표시"), ("exit", "종료"), ("refreshDevices", "장치 새로 고침"), ("automaticSwitching", "자동 전환:"), ("mapping", "매핑:"), ("keyboardsGroup", "감지된 물리 키보드"), ("layoutsGroup", "Windows 레이아웃"), ("testGroup", "키보드 테스트 및 식별"), ("activity", "활동"), ("active", "● 활성"), ("paused", "● 일시 중지"), ("unassigned", "할당되지 않음"), ("typingNow", "입력 중"), ("mappedColumn", "매핑됨")),
            ["ru"] = Create(("save", "Сохранить"), ("update", "Обновить"), ("delete", "Удалить"), ("startup", "Запускать с Windows"), ("show", "Показать"), ("exit", "Выход"), ("refreshDevices", "Обновить устройства"), ("automaticSwitching", "Автоматическое переключение:"), ("mapping", "Сопоставление:"), ("keyboardsGroup", "Обнаруженные физические клавиатуры"), ("layoutsGroup", "Раскладки Windows"), ("testGroup", "Проверка и определение клавиатуры"), ("activity", "Активность"), ("active", "● Активно"), ("paused", "● Приостановлено"), ("unassigned", "Не назначено"), ("typingNow", "Ввод сейчас"), ("mappedColumn", "Назначено")),
            ["zh-Hans"] = Create(("save", "保存"), ("update", "更新"), ("delete", "删除"), ("startup", "随 Windows 启动"), ("show", "显示"), ("exit", "退出"), ("refreshDevices", "刷新设备"), ("automaticSwitching", "自动切换:"), ("mapping", "映射:"), ("keyboardsGroup", "检测到的物理键盘"), ("layoutsGroup", "Windows 布局"), ("testGroup", "测试并识别键盘"), ("activity", "活动"), ("active", "● 已启用"), ("paused", "● 已暂停"), ("unassigned", "未分配"), ("typingNow", "正在输入"), ("mappedColumn", "已映射")),
            ["zh-Hant"] = Create(("save", "儲存"), ("update", "更新"), ("delete", "刪除"), ("startup", "隨 Windows 啟動"), ("show", "顯示"), ("exit", "結束"), ("refreshDevices", "重新整理裝置"), ("automaticSwitching", "自動切換:"), ("mapping", "對應:"), ("keyboardsGroup", "偵測到的實體鍵盤"), ("layoutsGroup", "Windows 配置"), ("testGroup", "測試並識別鍵盤"), ("activity", "活動"), ("active", "● 使用中"), ("paused", "● 已暫停"), ("unassigned", "未指定"), ("typingNow", "正在輸入"), ("mappedColumn", "已對應"))
        };

    public static string Get(string key)
    {
        var cultureName = CultureInfo.InstalledUICulture.Name;
        if (Translations.TryGetValue(cultureName, out var exact) && exact.TryGetValue(key, out var exactValue))
        {
            return exactValue;
        }

        var language = cultureName.Split('-', StringSplitOptions.RemoveEmptyEntries)[0];
        if (Translations.TryGetValue(language, out var languageTranslation) && languageTranslation.TryGetValue(key, out var value))
        {
            return value;
        }

        return English.TryGetValue(key, out var englishValue) ? englishValue : key;
    }

    public static string Format(string key, params object[] arguments) => string.Format(CultureInfo.CurrentCulture, Get(key), arguments);

    private static IReadOnlyDictionary<string, string> Create(params (string Key, string Value)[] entries) =>
        entries.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
}
