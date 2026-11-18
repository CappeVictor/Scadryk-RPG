using UnityEngine;

/// <summary>Lightweight hit and dash visuals made from Unity's built-in white sprite.</summary>
public static class ScadrykEffects
{
    private static Sprite whiteSprite;
    private static Material ringMaterial;
    private static Sprite rootThornSprite;
    private static Sprite saltShardSprite;
    private static Sprite bellWaveSprite;
    private static Sprite playerSlashSprite;
    private static Sprite playerPulseSprite;
    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null) whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return whiteSprite;
        }
    }

    public static Sprite SaltShardSprite => LoadEffectSprite("AttackSaltShard", 600f, ref saltShardSprite);
    private static Sprite PlayerSlashSprite => LoadEffectSprite("AttackPlayerSlash", 600f, ref playerSlashSprite);
    private static Sprite PlayerPulseSprite => LoadEffectSprite("AttackPlayerPulse", 600f, ref playerPulseSprite);

    public static void Burst(Vector2 position, Color color, float diameter) => Create(position, color, diameter, diameter, 0.26f);
    public static void Slash(Vector2 position, Vector2 direction, float length)
    {
        Sprite slashSprite = PlayerSlashSprite;
        if (slashSprite != null)
        {
            GameObject spriteEffect = CreateSpriteEffect(slashSprite, position + direction * 0.82f, new Vector2(length * 1.65f, length * 0.88f), Color.white, 0.16f, "Player sword arc", true);
            spriteEffect.transform.right = direction;
            spriteEffect.GetComponent<ScadrykTimedVisual>().SetSpin(-230f);
            return;
        }
        GameObject go = Create(position + direction * 0.85f, new Color(1f, 0.82f, 0.42f, 0.85f), length * 1.55f, 0.22f, 0.13f);
        go.transform.right = direction;
    }

    public static void PlayerPulse(Vector2 position, float diameter)
    {
        Sprite pulseSprite = PlayerPulseSprite;
        if (pulseSprite == null)
        {
            Burst(position, new Color(0.3f, 0.85f, 0.8f, 0.8f), diameter);
            return;
        }
        CreateSpriteEffect(pulseSprite, position, Vector2.one * diameter * 0.72f, Color.white, 0.3f, "Player special pulse", true);
    }

    public static void TelegraphLine(Vector2 start, Vector2 end, Color color, float thickness, float lifetime)
    {
        Vector2 delta = end - start;
        if (delta.sqrMagnitude < 0.01f) return;
        GameObject go = Create((start + end) * 0.5f, color, delta.magnitude, thickness, lifetime);
        go.transform.right = delta.normalized;
        go.name = "Enemy attack warning";
    }

    public static void GroundLane(Vector2 center, Vector2 direction, float length, float width, Color color, float lifetime)
    {
        Sprite attackSprite = LoadEffectSprite("AttackRootThorns", 300f, ref rootThornSprite);
        if (attackSprite == null)
        {
            GameObject fallback = Create(center, color, length, width, lifetime);
            fallback.transform.right = direction;
            fallback.name = "Root thorn attack";
            return;
        }
        GameObject go = CreateSpriteEffect(attackSprite, center, new Vector2(length, width), Color.white, lifetime, "Root thorn attack", true);
        go.transform.right = direction;
    }

    public static void Ring(Vector2 center, float diameter, Color color, float lifetime)
    {
        Sprite waveSprite = LoadEffectSprite("AttackBellWave", 600f, ref bellWaveSprite);
        if (waveSprite != null)
        {
            Color tint = Color.white;
            tint.a = lifetime > 0.5f ? 0.46f : 0.98f;
            CreateSpriteEffect(waveSprite, center, Vector2.one * diameter, tint, lifetime, "Bell echo wave");
            return;
        }
        GameObject go = new GameObject("Bell pulse warning");
        go.transform.position = center;
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 40;
        line.startWidth = 0.075f;
        line.endWidth = 0.075f;
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
        go.AddComponent<ScadrykTimedVisual>().Initialize(lifetime, color, false);
    }

    private static Sprite LoadEffectSprite(string resourceName, float pixelsPerUnit, ref Sprite cache)
    {
        if (cache != null) return cache;
        Texture2D texture = Resources.Load<Texture2D>("ScadrykArt/" + resourceName);
        if (texture == null) return null;
        cache = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        return cache;
    }

    private static GameObject CreateSpriteEffect(Sprite sprite, Vector2 position, Vector2 size, Color color, float lifetime, string objectName, bool expand = false)
    {
        var go = new GameObject(objectName);
        go.transform.position = position;
        Vector2 spriteSize = sprite.bounds.size;
        go.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = 22;
        go.AddComponent<ScadrykTimedVisual>().Initialize(lifetime, color, expand);
        return go;
    }

    private static GameObject Create(Vector2 position, Color color, float width, float height, float life)
    {
        var go = new GameObject("Combat effect");
        go.transform.position = position;
        go.transform.localScale = new Vector3(width, height, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = WhiteSprite;
        renderer.color = color;
        renderer.sortingOrder = 20;
        go.AddComponent<ScadrykTimedVisual>().Initialize(life, color, false);
        return go;
    }
}
