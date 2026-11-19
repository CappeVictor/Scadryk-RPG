using UnityEngine;

/// <summary>Small, persistent settings shared by the front end, input and audio.</summary>
public static class ScadrykSettings
{
    public static readonly string[] Actions = { "Atacar", "Esquivar", "Pulso", "Crescente", "Estouro", "Rompante", "Interagir", "Cima", "Baixo", "Esquerda", "Direita" };
    private static readonly KeyCode[] Defaults = { KeyCode.Space, KeyCode.LeftShift, KeyCode.Q, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.E, KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D };

    public static KeyCode Key(int action) => (KeyCode)PlayerPrefs.GetInt("Scadryk.Key." + action, (int)Defaults[action]);
    public static bool Down(int action) => Input.GetKeyDown(Key(action));
    public static bool Held(int action) => Input.GetKey(Key(action));
    public static void Bind(int action, KeyCode key)
    {
        if (action < 0 || action >= Defaults.Length || key == KeyCode.None || key == KeyCode.Escape) return;
        KeyCode oldKey = Key(action);
        for (int i = 0; i < Defaults.Length; i++)
            if (i != action && Key(i) == key) PlayerPrefs.SetInt("Scadryk.Key." + i, (int)oldKey);
        PlayerPrefs.SetInt("Scadryk.Key." + action, (int)key);
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
}
