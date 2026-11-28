using UnityEngine;

/// <summary>Small, persistent settings shared by the front end, input and audio.</summary>
public static class ScadrykSettings
{
    internal static int BindingRevision { get; private set; }
    internal const float DeferredSaveDelay = 0.45f;
    private static bool deferredSavePending;
    private static float deferredSaveAt;
    internal const float DefaultGamepadDeadzone = 0.19f;
    internal const float MinimumGamepadDeadzone = 0.08f;
    internal const float MaximumGamepadDeadzone = 0.4f;
    internal const float DefaultMusicVolume = 0.42f;
    internal const float DefaultEffectsVolume = 0.65f;
    internal const int FrameRateMonitor = 0;
    internal const int FrameRate30 = 1;
    internal const int FrameRate60 = 2;
    internal const int UnclearAttackAction = 11;
    private const int DefaultUnclearAttackPadButton = 9;
    private const string UnclearAttackPadKey = "Scadryk.Pad.UnclearAttack";
    public static readonly string[] Actions = { "Atacar", "Esquivar", "Pulso", "Crescente", "Estouro", "Rompante", "Interagir", "Cima", "Baixo", "Esquerda", "Direita", "Ataque pouco claro" };
    private static readonly KeyCode[] Defaults = { KeyCode.Space, KeyCode.LeftShift, KeyCode.Q, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.E, KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.F8 };
    private static readonly int[] PadDefaults = { 0, 1, 2, 4, 5, 6, 3 };
    private static readonly int[] PadOptionActions = { 0, 1, 2, 3, 4, 5, 6, UnclearAttackAction };
    private static readonly KeyCode[] PadButtons =
    {
        KeyCode.JoystickButton0, KeyCode.JoystickButton1, KeyCode.JoystickButton2, KeyCode.JoystickButton3,
        KeyCode.JoystickButton4, KeyCode.JoystickButton5, KeyCode.JoystickButton6, KeyCode.JoystickButton7,
        KeyCode.JoystickButton8, KeyCode.JoystickButton9, KeyCode.JoystickButton10, KeyCode.JoystickButton11,
        KeyCode.JoystickButton12, KeyCode.JoystickButton13, KeyCode.JoystickButton14, KeyCode.JoystickButton15,
        KeyCode.JoystickButton16, KeyCode.JoystickButton17, KeyCode.JoystickButton18, KeyCode.JoystickButton19
    };

    internal static bool ShouldFlushDeferredSave(bool pending, float now, float dueAt) => pending && now >= dueAt;
    internal static float DeferredSaveDeadline(bool pending, float now, float currentDueAt) =>
        pending ? currentDueAt : now + DeferredSaveDelay;

    internal static float ClampFiniteSetting(float value, float minimum, float maximum, float fallback) =>
        float.IsNaN(value) || float.IsInfinity(value)
            ? Mathf.Clamp(fallback, minimum, maximum)
            : Mathf.Clamp(value, minimum, maximum);

    private static Vector2 FiniteStickOrZero(Vector2 stick) =>
        float.IsNaN(stick.x) || float.IsInfinity(stick.x) || float.IsNaN(stick.y) || float.IsInfinity(stick.y)
            ? Vector2.zero
            : stick;

    private static void SetFloatDeferred(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        deferredSaveAt = DeferredSaveDeadline(deferredSavePending, Time.unscaledTime, deferredSaveAt);
        deferredSavePending = true;
    }

    private static void SetIntDeferred(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        deferredSaveAt = DeferredSaveDeadline(deferredSavePending, Time.unscaledTime, deferredSaveAt);
        deferredSavePending = true;
    }

    internal static void TickDeferredSave()
    {
        if (ShouldFlushDeferredSave(deferredSavePending, Time.unscaledTime, deferredSaveAt)) FlushPendingSave();
    }

    internal static void FlushPendingSave()
    {
        if (!deferredSavePending) return;
        PlayerPrefs.Save();
        deferredSavePending = false;
    }

    public static KeyCode Key(int action) => Key(action, "Scadryk.Key.");
    internal static KeyCode Key(int action, string prefix) => (KeyCode)PlayerPrefs.GetInt(prefix + action, (int)Defaults[action]);
    public static string KeyName(KeyCode key) => key switch
    {
        KeyCode.Space => "Espaço",
        KeyCode.LeftShift or KeyCode.RightShift => "Shift",
        KeyCode.LeftControl or KeyCode.RightControl => "Ctrl",
        KeyCode.LeftAlt or KeyCode.RightAlt => "Alt",
        KeyCode.Return => "Enter",
        KeyCode.KeypadEnter => "Enter numérico",
        KeyCode.Escape => "Esc",
        KeyCode.Backspace => "Backspace",
        KeyCode.UpArrow => "↑",
        KeyCode.DownArrow => "↓",
        KeyCode.LeftArrow => "←",
        KeyCode.RightArrow => "→",
        KeyCode.Alpha0 => "0",
        KeyCode.Alpha1 => "1",
        KeyCode.Alpha2 => "2",
        KeyCode.Alpha3 => "3",
        KeyCode.Alpha4 => "4",
        KeyCode.Alpha5 => "5",
        KeyCode.Alpha6 => "6",
        KeyCode.Alpha7 => "7",
        KeyCode.Alpha8 => "8",
        KeyCode.Alpha9 => "9",
        KeyCode.Keypad0 => "Num 0",
        KeyCode.Keypad1 => "Num 1",
        KeyCode.Keypad2 => "Num 2",
        KeyCode.Keypad3 => "Num 3",
        KeyCode.Keypad4 => "Num 4",
        KeyCode.Keypad5 => "Num 5",
        KeyCode.Keypad6 => "Num 6",
        KeyCode.Keypad7 => "Num 7",
        KeyCode.Keypad8 => "Num 8",
        KeyCode.Keypad9 => "Num 9",
        _ => key.ToString()
    };
    public static bool Down(int action) => Input.GetKeyDown(Key(action));
    public static bool Held(int action) => Input.GetKey(Key(action));
    internal static bool CanBindKey(KeyCode key) =>
        System.Enum.IsDefined(typeof(KeyCode), key) && (int)key < (int)KeyCode.Mouse0 &&
        key != KeyCode.None && key != KeyCode.Escape && key != KeyCode.M;
    internal static bool CanBindPadButton(int button) => button >= 0 && button < PadButtons.Length && button != 7 && button != 8;
    public static int PadButton(int action) => PadButton(action, "Scadryk.Pad.");
    internal static int PadButton(int action, string prefix) => action == UnclearAttackAction
        ? Mathf.Clamp(PlayerPrefs.GetInt(prefix + "UnclearAttack", DefaultUnclearAttackPadButton), 0, PadButtons.Length - 1)
        : PlayerPrefs.GetInt(prefix + action, PadDefaults[action]);
    internal static Vector2 ReadGamepadStick(string horizontalAxis, string verticalAxis) =>
        ApplyRadialDeadzone(new Vector2(Input.GetAxisRaw(horizontalAxis), Input.GetAxisRaw(verticalAxis)), GamepadDeadzone);
    internal static Vector2 ApplyRadialDeadzone(Vector2 stick, float deadzone)
    {
        stick = FiniteStickOrZero(stick);
        float threshold = ClampFiniteSetting(deadzone, 0f, 0.95f, DefaultGamepadDeadzone);
        float magnitude = stick.magnitude;
        if (magnitude <= threshold) return Vector2.zero;

        float remappedMagnitude = Mathf.Clamp01((magnitude - threshold) / (1f - threshold));
        return stick * (remappedMagnitude / magnitude);
    }
    internal static string GamepadStickReadout(Vector2 raw, float deadzone)
    {
        raw = FiniteStickOrZero(raw);
        int xPercent = Mathf.RoundToInt(Mathf.Clamp(raw.x, -1f, 1f) * 100f);
        int yPercent = Mathf.RoundToInt(Mathf.Clamp(raw.y, -1f, 1f) * 100f);
        int rawPercent = Mathf.RoundToInt(Mathf.Clamp01(raw.magnitude) * 100f);
        int usefulPercent = Mathf.RoundToInt(ApplyRadialDeadzone(raw, deadzone).magnitude * 100f);
        return string.Format("X{0:+00;-00;00} Y{1:+00;-00;00} {2:00}→{3:00}%", xPercent, yPercent, rawPercent, usefulPercent);
    }
    public static bool PadDown(int action) => action >= 0 && action < PadDefaults.Length
        ? PadButtonDown(PadButton(action))
        : action == UnclearAttackAction && PadButtonDown(UnclearAttackPadButton);
    internal static int PadOptionAction(int row) => row >= 0 && row < PadOptionActions.Length ? PadOptionActions[row] : -1;
    public static int UnclearAttackPadButton => PadButton(UnclearAttackAction);
    public static bool PadButtonDown(int button) => button >= 0 && button < PadButtons.Length && Input.GetKeyDown(PadButtons[button]);
    public static string PadButtonName(int button) => button switch
    {
        0 => "A", 1 => "B", 2 => "X", 3 => "Y", 4 => "LB", 5 => "RB", 6 => "Back", 7 => "Start",
        8 => "L3", 9 => "R3",
        _ => "Botão " + button
    };

    public static void Bind(int action, KeyCode key)
    {
        if (action < 0 || action >= Defaults.Length || !CanBindKey(key)) return;
        var bindings = new int[Defaults.Length];
        for (int i = 0; i < bindings.Length; i++) bindings[i] = (int)Key(i);
        SwapConflictingBinding(bindings, action, (int)key);
        for (int i = 0; i < bindings.Length; i++) PlayerPrefs.SetInt("Scadryk.Key." + i, bindings[i]);
        PlayerPrefs.Save();
        BindingRevision++;
    }

    public static void BindPad(int action, int button)
    {
        if (action == UnclearAttackAction)
        {
            BindUnclearAttackPad(button);
            return;
        }
        if (action < 0 || action >= PadDefaults.Length || !CanBindPadButton(button)) return;
        var bindings = new int[PadDefaults.Length];
        for (int i = 0; i < bindings.Length; i++) bindings[i] = PadButton(i);
        int unclearAttackButton = UnclearAttackPadButton;
        if (!SwapGamepadBinding(bindings, action, button, ref unclearAttackButton)) return;
        for (int i = 0; i < bindings.Length; i++) PlayerPrefs.SetInt("Scadryk.Pad." + i, bindings[i]);
        if (unclearAttackButton != UnclearAttackPadButton) PlayerPrefs.SetInt(UnclearAttackPadKey, unclearAttackButton);
        PlayerPrefs.Save();
        BindingRevision++;
    }

    private static void BindUnclearAttackPad(int button)
    {
        if (!CanBindPadButton(button)) return;
        int previous = UnclearAttackPadButton;
        var bindings = new int[PadDefaults.Length];
        for (int i = 0; i < PadDefaults.Length; i++) bindings[i] = PadButton(i);
        if (!SwapUnclearAttackPadBinding(bindings, button, ref previous)) return;
        for (int i = 0; i < PadDefaults.Length; i++) PlayerPrefs.SetInt("Scadryk.Pad." + i, bindings[i]);
        PlayerPrefs.SetInt(UnclearAttackPadKey, button);
        PlayerPrefs.Save();
        BindingRevision++;
    }

    internal static bool SwapConflictingBinding(int[] bindings, int action, int newBinding)
    {
        if (bindings == null || action < 0 || action >= bindings.Length) return false;
        int oldBinding = bindings[action];
        for (int i = 0; i < bindings.Length; i++)
            if (i != action && bindings[i] == newBinding) bindings[i] = oldBinding;
        bindings[action] = newBinding;
        return true;
    }

    internal static bool SwapGamepadBinding(int[] bindings, int action, int newBinding, ref int unclearAttackButton)
    {
        if (bindings == null || action < 0 || action >= bindings.Length || !CanBindPadButton(newBinding)) return false;
        int previousActionButton = bindings[action];
        if (!SwapConflictingBinding(bindings, action, newBinding)) return false;
        if (newBinding == unclearAttackButton) unclearAttackButton = previousActionButton;
        return true;
    }

    internal static bool SwapUnclearAttackPadBinding(int[] bindings, int newBinding, ref int unclearAttackButton)
    {
        if (bindings == null || !CanBindPadButton(newBinding)) return false;
        int previous = unclearAttackButton;
        for (int i = 0; i < bindings.Length; i++)
            if (bindings[i] == newBinding) bindings[i] = previous;
        unclearAttackButton = newBinding;
        return true;
    }

    internal static bool BindingValuesAreUnique(int[] bindings)
    {
        if (bindings == null) return false;
        for (int i = 0; i < bindings.Length; i++)
        for (int other = i + 1; other < bindings.Length; other++)
            if (bindings[i] == bindings[other]) return false;
        return true;
    }

    internal static void ClearReservedBindings(string keyPrefix, string padPrefix) =>
        ClearReservedBindings(keyPrefix, padPrefix, keyPrefix == "Scadryk.Key." ? UnclearAttackPadKey : keyPrefix + "UnclearAttackPad");

    internal static void ClearReservedBindings(string keyPrefix, string padPrefix, string unclearPadKey)
    {
        bool changed = false;
        for (int i = 0; i < Defaults.Length; i++)
        {
            KeyCode saved = (KeyCode)PlayerPrefs.GetInt(keyPrefix + i, (int)Defaults[i]);
            if (CanBindKey(saved)) continue;
            PlayerPrefs.DeleteKey(keyPrefix + i);
            changed = true;
        }
        for (int i = 0; i < PadDefaults.Length; i++)
        {
            int saved = PlayerPrefs.GetInt(padPrefix + i, PadDefaults[i]);
            if (CanBindPadButton(saved)) continue;
            PlayerPrefs.DeleteKey(padPrefix + i);
            changed = true;
        }
        int unclearPad = Mathf.Clamp(PlayerPrefs.GetInt(unclearPadKey, DefaultUnclearAttackPadButton), 0, PadButtons.Length - 1);
        bool padConflict = !CanBindPadButton(unclearPad);
        for (int i = 0; i < PadDefaults.Length; i++)
            padConflict |= PlayerPrefs.GetInt(padPrefix + i, PadDefaults[i]) == unclearPad;
        if (padConflict)
        {
            for (int candidate = 0; candidate < PadButtons.Length; candidate++)
            {
                if (!CanBindPadButton(candidate)) continue;
                bool occupied = false;
                for (int i = 0; i < PadDefaults.Length; i++)
                    occupied |= PlayerPrefs.GetInt(padPrefix + i, PadDefaults[i]) == candidate;
                if (!occupied) { unclearPad = candidate; break; }
            }
            PlayerPrefs.SetInt(unclearPadKey, unclearPad);
            changed = true;
        }
        if (changed) PlayerPrefs.Save();
    }

    public static void ClearReservedBindings() => ClearReservedBindings("Scadryk.Key.", "Scadryk.Pad.");

    public static void ResetKeyboardBindings()
    {
        ResetKeyboardBindings("Scadryk.Key.");
        BindingRevision++;
    }

    internal static void ResetKeyboardBindings(string prefix)
    {
        for (int i = 0; i < Defaults.Length; i++) PlayerPrefs.DeleteKey(prefix + i);
        PlayerPrefs.Save();
    }

    public static void ResetGamepadBindings()
    {
        ResetGamepadBindings("Scadryk.Pad.");
        BindingRevision++;
    }

    internal static void ResetGamepadBindings(string prefix)
    {
        for (int i = 0; i < PadDefaults.Length; i++) PlayerPrefs.DeleteKey(prefix + i);
        PlayerPrefs.DeleteKey(prefix + "UnclearAttack");
        PlayerPrefs.Save();
    }

    public static float MusicVolume
    {
        get => ClampFiniteSetting(PlayerPrefs.GetFloat("Scadryk.Music", DefaultMusicVolume), 0f, 1f, DefaultMusicVolume);
        set { float clamped = ClampFiniteSetting(value, 0f, 1f, DefaultMusicVolume); if (Mathf.Abs(MusicVolume - clamped) < 0.001f) return; SetFloatDeferred("Scadryk.Music", clamped); }
    }
    public static float EffectsVolume
    {
        get => ClampFiniteSetting(PlayerPrefs.GetFloat("Scadryk.Effects", DefaultEffectsVolume), 0f, 1f, DefaultEffectsVolume);
        set { float clamped = ClampFiniteSetting(value, 0f, 1f, DefaultEffectsVolume); if (Mathf.Abs(EffectsVolume - clamped) < 0.001f) return; SetFloatDeferred("Scadryk.Effects", clamped); }
    }
    public static float GamepadDeadzone
    {
        get => ClampFiniteSetting(PlayerPrefs.GetFloat("Scadryk.GamepadDeadzone", DefaultGamepadDeadzone), MinimumGamepadDeadzone, MaximumGamepadDeadzone, DefaultGamepadDeadzone);
        set
        {
            float clamped = ClampFiniteSetting(value, MinimumGamepadDeadzone, MaximumGamepadDeadzone, DefaultGamepadDeadzone);
            if (Mathf.Abs(GamepadDeadzone - clamped) < 0.001f) return;
            SetFloatDeferred("Scadryk.GamepadDeadzone", clamped);
        }
    }
    public static float TextScale
    {
        get => ClampFiniteSetting(PlayerPrefs.GetFloat("Scadryk.TextScale", 1f), 1f, 1.5f, 1f);
        set { float clamped = ClampFiniteSetting(value, 1f, 1.5f, 1f); if (Mathf.Abs(TextScale - clamped) < 0.001f) return; SetFloatDeferred("Scadryk.TextScale", clamped); }
    }
    public static bool ReduceFlash
    {
        get => PlayerPrefs.GetInt("Scadryk.ReduceFlash", 0) != 0;
        set { if (ReduceFlash == value) return; PlayerPrefs.SetInt("Scadryk.ReduceFlash", value ? 1 : 0); PlayerPrefs.Save(); }
    }
    public static bool ReduceShake
    {
        get => PlayerPrefs.GetInt("Scadryk.ReduceShake", 0) != 0;
        set { if (ReduceShake == value) return; PlayerPrefs.SetInt("Scadryk.ReduceShake", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static bool HighContrastWarnings
    {
        get => PlayerPrefs.GetInt("Scadryk.HighContrastWarnings", 0) != 0;
        set { if (HighContrastWarnings == value) return; PlayerPrefs.SetInt("Scadryk.HighContrastWarnings", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static bool ExtendedAttackWarnings
    {
        get => PlayerPrefs.GetInt("Scadryk.ExtendedAttackWarnings", 0) != 0;
        set { if (ExtendedAttackWarnings == value) return; PlayerPrefs.SetInt("Scadryk.ExtendedAttackWarnings", value ? 1 : 0); PlayerPrefs.Save(); }
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt("Scadryk.Fullscreen", 1) != 0;
        set
        {
            if (Fullscreen == value) return;
            PlayerPrefs.SetInt("Scadryk.Fullscreen", value ? 1 : 0);
            PlayerPrefs.Save();
            ApplyFullscreen(value);
        }
    }

    public static int FrameRateMode
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("Scadryk.FrameRateMode", FrameRateMonitor), FrameRateMonitor, FrameRate60);
        set
        {
            int clamped = Mathf.Clamp(value, FrameRateMonitor, FrameRate60);
            if (FrameRateMode == clamped) return;
            SetIntDeferred("Scadryk.FrameRateMode", clamped);
            ApplyFrameRateMode(clamped);
        }
    }

    internal static int FrameRateTargetForMode(int mode) => Mathf.Clamp(mode, FrameRateMonitor, FrameRate60) switch
    {
        FrameRate30 => 30,
        FrameRate60 => 60,
        _ => -1
    };

    internal static int VSyncForFrameRateMode(int mode) => Mathf.Clamp(mode, FrameRateMonitor, FrameRate60) == FrameRateMonitor ? 1 : 0;

    internal static int NextFrameRateMode(int mode, int direction)
    {
        int normalizedDirection = direction < 0 ? -1 : 1;
        return (Mathf.Clamp(mode, FrameRateMonitor, FrameRate60) + normalizedDirection + 3) % 3;
    }

    internal static string FrameRateModeLabel(int mode) => Mathf.Clamp(mode, FrameRateMonitor, FrameRate60) switch
    {
        FrameRate30 => "30 FPS",
        FrameRate60 => "60 FPS",
        _ => "Auto FPS"
    };

    internal static void ApplyFrameRateMode(int mode)
    {
        int clamped = Mathf.Clamp(mode, FrameRateMonitor, FrameRate60);
        QualitySettings.vSyncCount = VSyncForFrameRateMode(clamped);
        Application.targetFrameRate = FrameRateTargetForMode(clamped);
    }

    internal static void ApplyFrameRatePreference() => ApplyFrameRateMode(FrameRateMode);

    internal static Vector2Int WindowedResolutionForDisplay(int displayWidth, int displayHeight)
    {
        if (displayWidth <= 0 || displayHeight <= 0) return new Vector2Int(1280, 720);
        float scale = Mathf.Min(0.9f, 1280f / displayWidth, 720f / displayHeight);
        return new Vector2Int(Mathf.Max(1, Mathf.FloorToInt(displayWidth * scale)),
            Mathf.Max(1, Mathf.FloorToInt(displayHeight * scale)));
    }

    internal static void ApplyFullscreenPreference()
    {
        bool fullscreen = Fullscreen;
        if (Screen.fullScreen != fullscreen) ApplyFullscreen(fullscreen);
    }

    private static void ApplyFullscreen(bool fullscreen)
    {
        Resolution display = Screen.currentResolution;
        if (fullscreen) Screen.SetResolution(display.width, display.height, FullScreenMode.FullScreenWindow);
        else
        {
            Vector2Int windowed = WindowedResolutionForDisplay(display.width, display.height);
            Screen.SetResolution(windowed.x, windowed.y, FullScreenMode.Windowed);
        }
    }
}
