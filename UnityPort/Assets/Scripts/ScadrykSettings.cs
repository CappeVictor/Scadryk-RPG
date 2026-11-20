using UnityEngine;

/// <summary>Small, persistent settings shared by the front end, input and audio.</summary>
public static class ScadrykSettings
{
    public static readonly string[] Actions = { "Atacar", "Esquivar", "Pulso", "Crescente", "Estouro", "Rompante", "Interagir", "Cima", "Baixo", "Esquerda", "Direita" };
    private static readonly KeyCode[] Defaults = { KeyCode.Space, KeyCode.LeftShift, KeyCode.Q, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.E, KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D };
    private static readonly int[] PadDefaults = { 0, 1, 2, 4, 5, 6, 3 };
    private static readonly KeyCode[] PadButtons =
    {
        KeyCode.JoystickButton0, KeyCode.JoystickButton1, KeyCode.JoystickButton2, KeyCode.JoystickButton3,
        KeyCode.JoystickButton4, KeyCode.JoystickButton5, KeyCode.JoystickButton6, KeyCode.JoystickButton7,
        KeyCode.JoystickButton8, KeyCode.JoystickButton9, KeyCode.JoystickButton10, KeyCode.JoystickButton11,
        KeyCode.JoystickButton12, KeyCode.JoystickButton13, KeyCode.JoystickButton14, KeyCode.JoystickButton15,
        KeyCode.JoystickButton16, KeyCode.JoystickButton17, KeyCode.JoystickButton18, KeyCode.JoystickButton19
    };

    public static KeyCode Key(int action) => (KeyCode)PlayerPrefs.GetInt("Scadryk.Key." + action, (int)Defaults[action]);
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
    public static int PadButton(int action) => PlayerPrefs.GetInt("Scadryk.Pad." + action, PadDefaults[action]);
    public static bool PadDown(int action) => action >= 0 && action < PadDefaults.Length && PadButtonDown(PadButton(action));
    public static bool PadButtonDown(int button) => button >= 0 && button < PadButtons.Length && Input.GetKeyDown(PadButtons[button]);
    public static string PadButtonName(int button) => button switch
    {
        0 => "A", 1 => "B", 2 => "X", 3 => "Y", 4 => "LB", 5 => "RB", 6 => "Back", 7 => "Start",
        _ => "Botão " + button
    };

    public static void Bind(int action, KeyCode key)
    {
        if (action < 0 || action >= Defaults.Length || key == KeyCode.None || key == KeyCode.Escape) return;
        KeyCode oldKey = Key(action);
        for (int i = 0; i < Defaults.Length; i++)
            if (i != action && Key(i) == key) PlayerPrefs.SetInt("Scadryk.Key." + i, (int)oldKey);
        PlayerPrefs.SetInt("Scadryk.Key." + action, (int)key);
        PlayerPrefs.Save();
    }

    public static void BindPad(int action, int button)
    {
        if (action < 0 || action >= PadDefaults.Length || button < 0 || button >= PadButtons.Length) return;
        int oldButton = PadButton(action);
        for (int i = 0; i < PadDefaults.Length; i++)
            if (i != action && PadButton(i) == button) PlayerPrefs.SetInt("Scadryk.Pad." + i, oldButton);
        PlayerPrefs.SetInt("Scadryk.Pad." + action, button);
        PlayerPrefs.Save();
    }

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat("Scadryk.Music", 0.42f);
        set { if (Mathf.Abs(MusicVolume - value) < 0.001f) return; PlayerPrefs.SetFloat("Scadryk.Music", Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }
    public static float EffectsVolume
    {
        get => PlayerPrefs.GetFloat("Scadryk.Effects", 0.65f);
        set { if (Mathf.Abs(EffectsVolume - value) < 0.001f) return; PlayerPrefs.SetFloat("Scadryk.Effects", Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }
    public static float TextScale
    {
        get => PlayerPrefs.GetFloat("Scadryk.TextScale", 1f);
        set { if (Mathf.Abs(TextScale - value) < 0.001f) return; PlayerPrefs.SetFloat("Scadryk.TextScale", Mathf.Clamp(value, 1f, 1.5f)); PlayerPrefs.Save(); }
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
}
