using UnityEngine;

/// <summary>Lightweight hit and dash visuals made from Unity's built-in white sprite.</summary>
public static class ScadrykEffects
{
    public const int SecondaryVisualLimit = 24;
    private const int MaxSecondaryVisuals = SecondaryVisualLimit;
    private static readonly System.Collections.Generic.List<ScadrykTimedVisual> SecondaryVisuals = new System.Collections.Generic.List<ScadrykTimedVisual>(MaxSecondaryVisuals);
    public static int SecondaryVisualCount
    {
        get
        {
            for (int i = SecondaryVisuals.Count - 1; i >= 0; i--)
                if (SecondaryVisuals[i] == null) SecondaryVisuals.RemoveAt(i);
            return SecondaryVisuals.Count;
        }
    }
    private static Sprite whiteSprite;
    private static Material ringMaterial;
    private static Sprite rootThornSprite;
    private static Sprite saltShardSprite;
    private static Sprite bellWaveSprite;
    private static Sprite playerSlashSprite;
    private static Sprite playerPulseSprite;
    private static Sprite softShadowSprite;
    private static Sprite verdantCrescentSprite, saltburstSprite, tidebreakerSprite, voidburstSprite;
    internal const float PlayerSlashForwardOffset = 0.82f;
    internal static Vector2 PlayerSlashFootprint(float length) => new Vector2(length * 1.65f, length * 0.88f);
    internal static float FlashAlphaScale(bool reduceFlash) => reduceFlash ? 0.48f : 1f;
    internal static float FlashDiameterScale(bool reduceFlash) => reduceFlash ? 0.8f : 1f;
    internal static bool ShouldReduceEffectFlash(bool reduceFlash, bool dangerWarning) => reduceFlash && !dangerWarning;
    internal static float DashAfterimageInterval(bool reduceFlash) => reduceFlash ? 0.18f : 0.09f;
    internal static float AfterimageLifetime(bool reduceFlash) => reduceFlash ? 0.13f : 0.23f;
    internal static Color ApplyReducedFlash(Color color, bool reduceFlash) { if (reduceFlash) color.a *= FlashAlphaScale(true); return color; }
    internal static float FlashExpansionScale(float progress, bool reduceFlash) =>
        Mathf.Lerp(reduceFlash ? 0.94f : 0.72f, reduceFlash ? 1.06f : 1.32f, Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI * 0.5f));
    internal static int DeathBurstMoteCount(bool boss, bool reduceFlash) => reduceFlash ? (boss ? 3 : 2) : (boss ? 6 : 3);
    internal static float DangerWarningWidth(float width) => width * (ScadrykSettings.HighContrastWarnings ? 1.4f : 1f);
    internal static Color DangerWarningColor(Color color)
    {
        if (!ScadrykSettings.HighContrastWarnings) return color;
        Color highContrast = Color.Lerp(color, Color.white, 0.55f);
        highContrast.a = Mathf.Max(color.a, 0.98f);
        return highContrast;
    }
    internal static Color DangerWarningTint(Color color, bool dangerWarning) => dangerWarning ? DangerWarningColor(color) : color;
    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null) whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return whiteSprite;
        }
    }

    public static Sprite SaltShardSprite => LoadEffectSprite("AttackSaltShard", 600f, ref saltShardSprite);
    public static Sprite RootThrustSprite => LoadEffectSprite("AttackRootThorns", 300f, ref rootThornSprite);
    public static Sprite BellPulseSprite => LoadEffectSprite("AttackBellWave", 600f, ref bellWaveSprite);
    public static Sprite VerdantCrescentSprite => LoadEffectSprite("AbilityVerdantCrescent", 600f, ref verdantCrescentSprite);
    public static Sprite SaltburstSprite => LoadEffectSprite("AbilitySaltburst", 600f, ref saltburstSprite);
    public static Sprite TidebreakerSprite => LoadEffectSprite("AbilityTidebreaker", 600f, ref tidebreakerSprite);
    public static Sprite VoidburstSprite => LoadEffectSprite("FinalBossVoidburst", 600f, ref voidburstSprite);
    public static Sprite SoftShadowSprite
    {
        get
        {
            if (softShadowSprite != null) return softShadowSprite;
            const int width = 128, height = 64;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float dx = ((x + 0.5f) - width * 0.5f) / (width * 0.5f);
                float dy = ((y + 0.5f) - height * 0.5f) / (height * 0.5f);
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = (1f - Mathf.SmoothStep(0.28f, 1f, radius)) * 0.48f;
                texture.SetPixel(x, y, new Color(0.008f, 0.014f, 0.02f, alpha));
            }
            texture.Apply();
            softShadowSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f);
            softShadowSprite.name = "Generated soft contact shadow";
            return softShadowSprite;
        }
    }
    private static Sprite PlayerSlashSprite => LoadEffectSprite("AttackPlayerSlash", 600f, ref playerSlashSprite);
    private static Sprite PlayerPulseSprite => LoadEffectSprite("AttackPlayerPulse", 600f, ref playerPulseSprite);

    internal static bool RequiredCombatArtIsLoaded => RootThrustSprite != null && SaltShardSprite != null &&
        BellPulseSprite != null && PlayerSlashSprite != null && PlayerPulseSprite != null &&
        VerdantCrescentSprite != null && SaltburstSprite != null && TidebreakerSprite != null && VoidburstSprite != null;

    private static void RegisterSecondary(GameObject effect)
    {
        ScadrykTimedVisual visual = effect.GetComponent<ScadrykTimedVisual>();
        for (int i = SecondaryVisuals.Count - 1; i >= 0; i--)
            if (SecondaryVisuals[i] == null) SecondaryVisuals.RemoveAt(i);

        if (SecondaryVisuals.Count >= MaxSecondaryVisuals)
        {
            ScadrykTimedVisual oldest = SecondaryVisuals[0];
            SecondaryVisuals.RemoveAt(0);
            if (oldest != null)
            {
                oldest.gameObject.SetActive(false);
                Object.Destroy(oldest.gameObject);
            }
        }

        if (visual != null) SecondaryVisuals.Add(visual);
    }

    public static void Burst(Vector2 position, Color color, float diameter)
    {
        bool reduceFlash = ScadrykSettings.ReduceFlash;
        color.a *= FlashAlphaScale(reduceFlash);
        float size = diameter * FlashDiameterScale(reduceFlash);
        Create(position, color, size, size, reduceFlash ? 0.34f : 0.26f);
    }
    public static void Afterimage(SpriteRenderer source, Color color)
    {
        if (source == null || source.sprite == null) return;
        bool reduceFlash = ScadrykSettings.ReduceFlash;
        color = ApplyReducedFlash(color, reduceFlash);
        var ghost = new GameObject("Motion afterimage");
        ghost.transform.position = source.transform.position;
        ghost.transform.rotation = source.transform.rotation;
        ghost.transform.localScale = source.transform.lossyScale;
        var renderer = ghost.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.flipX = source.flipX;
        renderer.sortingOrder = source.sortingOrder - 1;
        renderer.color = color;
        ghost.AddComponent<ScadrykTimedVisual>().Initialize(AfterimageLifetime(reduceFlash), color, false);
        RegisterSecondary(ghost);
    }

    public static void DeathBurst(Vector2 position, Color color, bool boss)
    {
        bool reduceFlash = ScadrykSettings.ReduceFlash;
        int count = DeathBurstMoteCount(boss, reduceFlash);
        Color moteColor = color;
        moteColor.a *= FlashAlphaScale(reduceFlash);
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float moteSize = (boss ? 0.25f : 0.15f) * FlashDiameterScale(reduceFlash);
            GameObject mote = Create(position + direction * 0.25f, moteColor, moteSize, moteSize, 0.5f);
            mote.AddComponent<ScadrykEffectDrift>().Initialize(direction * (boss ? 3f : 1.8f) * (reduceFlash ? 0.65f : 1f));
        }
        Burst(position, color, boss ? 2.1f : 0.95f);
    }

    public static GameObject ReleaseEcho(Sprite echoSprite, Vector2 position, Color color)
    {
        GameObject echo = CreateSpriteEffect(echoSprite ?? WhiteSprite, position, new Vector2(0.78f, 0.92f), color,
            1.2f, "Liberated echo memory");
        echo.AddComponent<ScadrykEffectDrift>().Initialize(Vector2.up * 0.85f);
        return echo;
    }

    public static void Slash(Vector2 position, Vector2 direction, float length, bool finisher = false)
    {
        Vector2 footprint = PlayerSlashFootprint(length);
        Color slashTint = finisher ? new Color(1f, 0.78f, 0.32f, 0.95f) : Color.white;
        float lifetime = finisher ? 0.22f : 0.16f;
        Sprite slashSprite = PlayerSlashSprite;
        if (slashSprite != null)
        {
            GameObject spriteEffect = CreateSpriteEffect(slashSprite, position + direction * PlayerSlashForwardOffset, footprint, slashTint, lifetime, finisher ? "Player sword finisher arc" : "Player sword arc", true);
            spriteEffect.transform.right = direction;
            spriteEffect.GetComponent<ScadrykTimedVisual>().SetSpin(finisher ? -310f : -230f);
            return;
        }
        if (!finisher) slashTint = new Color(1f, 0.82f, 0.42f, 0.85f);
        GameObject go = Create(position + direction * PlayerSlashForwardOffset, slashTint, footprint.x, footprint.y, lifetime);
        go.transform.right = direction;
    }

    internal static float PlayerPulseDiameter(float hitRadius) => hitRadius * 2f;
    internal static float AbilitySaltburstVisualDiameter(float hitRadius) => hitRadius * 2f;

    public static void PlayerPulse(Vector2 position, float hitRadius)
    {
        float diameter = PlayerPulseDiameter(hitRadius);
        Sprite pulseSprite = PlayerPulseSprite;
        if (pulseSprite == null)
        {
            Burst(position, new Color(0.3f, 0.85f, 0.8f, 0.8f), diameter);
            return;
        }
        CreateSpriteEffect(pulseSprite, position, Vector2.one * diameter, Color.white, 0.3f, "Player special pulse", true);
    }

    public static void AbilityCrescent(Vector2 position, Vector2 direction)
    {
        Sprite art = VerdantCrescentSprite ?? PlayerSlashSprite;
        if (art == null) { Slash(position, direction, 2.7f); return; }
        GameObject effect = CreateSpriteEffect(art, position + direction * 1.05f, new Vector2(4.1f, 2.8f), Color.white, 0.34f, "Verdant Crescent ability", true);
        effect.transform.right = direction;
        effect.AddComponent<ScadrykEffectDrift>().Initialize(direction * 3f);
    }

    public static void AbilitySaltburst(Vector2 position, float diameter)
    {
        Sprite art = SaltburstSprite ?? PlayerPulseSprite;
        if (art == null) { Burst(position, new Color(1f, 0.78f, 0.38f, 0.9f), diameter); return; }
        CreateSpriteEffect(art, position, Vector2.one * diameter, Color.white, 0.48f, "Saltburst ability", true);
    }

    public static void AbilityTidebreaker(Vector2 position, Vector2 direction)
    {
        Sprite art = TidebreakerSprite ?? PlayerPulseSprite;
        if (art == null) { GroundLane(position + direction * 2f, direction, 7f, 2.6f, new Color(0.3f, 0.85f, 0.95f, 0.9f), 0.4f); return; }
        GameObject effect = CreateSpriteEffect(art, position + direction * 2.7f, new Vector2(7.4f, 4.4f), Color.white, 0.52f, "Tidebreaker ability", true);
        effect.transform.right = direction;
        effect.AddComponent<ScadrykEffectDrift>().Initialize(direction * 4f);
    }

    public static GameObject Voidburst(Vector2 position, float diameter, float lifetime, bool attackWarning = false)
    {
        Sprite art = VoidburstSprite;
        if (art == null) return Ring(position, diameter, new Color(0.96f, 0.18f, 0.54f, 0.95f), lifetime, attackWarning);
        GameObject effect = CreateSpriteEffect(art, position, Vector2.one * diameter, Color.white, lifetime, "Final boss voidburst", true, true, attackWarning);
        if (attackWarning) effect.name = "Enemy attack warning";
        return effect;
    }

    public static GameObject TelegraphLine(Vector2 start, Vector2 end, Color color, float thickness, float lifetime)
    {
        Vector2 delta = end - start;
        if (delta.sqrMagnitude < 0.01f) return null;
        GameObject go = Create((start + end) * 0.5f, DangerWarningColor(color), delta.magnitude, DangerWarningWidth(thickness), lifetime, true);
        go.transform.right = delta.normalized;
        go.name = "Enemy attack warning";
        return go;
    }

    public static void GroundLane(Vector2 center, Vector2 direction, float length, float width, Color color, float lifetime, Sprite regionalSprite = null)
    {
        Sprite attackSprite = regionalSprite != null ? regionalSprite : LoadEffectSprite("AttackRootThorns", 300f, ref rootThornSprite);
        if (attackSprite == null)
        {
            CreateGroundLaneFallback(center, direction, length, width, color, lifetime);
            return;
        }
        GameObject go = CreateSpriteEffect(attackSprite, center, new Vector2(length, width), color, lifetime, "Regional enemy thrust", true, true, true);
        go.transform.right = direction;
    }

    internal static GameObject CreateGroundLaneFallback(Vector2 center, Vector2 direction, float length, float width, Color color, float lifetime)
    {
        GameObject fallback = Create(center, DangerWarningColor(color), length, DangerWarningWidth(width), lifetime, true);
        fallback.transform.right = direction;
        fallback.name = "Enemy attack warning";
        ScadrykTimedVisual visual = fallback.GetComponent<ScadrykTimedVisual>();
        if (visual != null) visual.MarkDangerWarning();
        return fallback;
    }

    public static GameObject Ring(Vector2 center, float diameter, Color color, float lifetime, bool attackWarning = false)
    {
        Sprite waveSprite = LoadEffectSprite("AttackBellWave", 600f, ref bellWaveSprite);
        if (waveSprite != null)
        {
            Color tint = DangerWarningTint(color, attackWarning);
            tint.a *= lifetime > 0.5f ? 0.46f : 0.98f;
            GameObject wave = CreateSpriteEffect(waveSprite, center, Vector2.one * diameter, tint, lifetime,
                attackWarning ? "Enemy attack warning" : "Bell echo wave", false, true, attackWarning);
            return wave;
        }
        return CreateRingFallback(center, diameter, color, lifetime, attackWarning);
    }

    internal static GameObject[] SupportPulseSignal(Vector2 center, Color color, float lifetime, bool attackWarning)
    {
        return new[]
        {
            Ring(center, ScadrykEnemyHealth.SupportPulseDiameter, color, lifetime, attackWarning),
            Ring(center, ScadrykEnemyHealth.SupportPulseInnerDiameter, Color.Lerp(color, Color.white, 0.28f), lifetime, attackWarning)
        };
    }

    internal static float RingVisualDiameter(GameObject ring)
    {
        if (ring == null) return -1f;
        SpriteRenderer sprite = ring.GetComponent<SpriteRenderer>();
        if (sprite != null) return sprite.bounds.size.x;
        LineRenderer line = ring.GetComponent<LineRenderer>();
        if (line == null || line.positionCount < 21) return -1f;
        return Vector3.Distance(line.GetPosition(0), line.GetPosition(line.positionCount / 2));
    }

    internal static GameObject CreateRingFallback(Vector2 center, float diameter, Color color, float lifetime, bool attackWarning)
    {
        color = ApplyReducedFlash(color, ShouldReduceEffectFlash(ScadrykSettings.ReduceFlash, attackWarning));
        color = DangerWarningTint(color, attackWarning);
        GameObject go = new GameObject(attackWarning ? "Enemy attack warning" : "Bell echo wave");
        go.transform.position = center;
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 40;
        line.startWidth = DangerWarningWidth(0.075f);
        line.endWidth = DangerWarningWidth(0.075f);
        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = 22;
        if (ringMaterial == null) ringMaterial = new Material(Shader.Find("Sprites/Default"));
        line.sharedMaterial = ringMaterial;
        float radius = diameter * 0.5f;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
        ScadrykTimedVisual timedVisual = go.AddComponent<ScadrykTimedVisual>();
        timedVisual.Initialize(lifetime, color, false);
        if (attackWarning) timedVisual.MarkDangerWarning();
        return go;
    }

    private static Sprite LoadEffectSprite(string resourceName, float pixelsPerUnit, ref Sprite cache)
    {
        if (cache != null) return cache;
        Texture2D texture = Resources.Load<Texture2D>("ScadrykArt/" + resourceName);
        if (texture == null) return null;
        cache = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        return cache;
    }

    private static GameObject CreateSpriteEffect(Sprite sprite, Vector2 position, Vector2 size, Color color, float lifetime,
        string objectName, bool expand = false, bool protectFromBudget = false, bool dangerWarning = false)
    {
        bool reduceFlash = ShouldReduceEffectFlash(ScadrykSettings.ReduceFlash, dangerWarning);
        color = ApplyReducedFlash(color, reduceFlash);
        var go = new GameObject(objectName);
        go.transform.position = position;
        Vector2 spriteSize = sprite.bounds.size;
        go.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 22;
        go.AddComponent<ScadrykTimedVisual>().Initialize(lifetime, color, expand);
        if (!protectFromBudget) RegisterSecondary(go);
        return go;
    }

    private static GameObject Create(Vector2 position, Color color, float width, float height, float life, bool protectFromBudget = false)
    {
        var go = new GameObject("Combat effect");
        go.transform.position = position;
        go.transform.localScale = new Vector3(width, height, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = WhiteSprite;
        renderer.color = color;
        renderer.sortingOrder = 20;
        go.AddComponent<ScadrykTimedVisual>().Initialize(life, color, false);
        if (!protectFromBudget) RegisterSecondary(go);
        return go;
    }
}
