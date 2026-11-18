using UnityEngine;

/// <summary>Lightweight hit and dash visuals made from Unity's built-in white sprite.</summary>
public static class ScadrykEffects
{
    private static Sprite whiteSprite;
    private static Material ringMaterial;
    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null) whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return whiteSprite;
        }
    }

    public static void Burst(Vector2 position, Color color, float diameter) => Create(position, color, diameter, diameter, 0.26f);
    public static void Slash(Vector2 position, Vector2 direction, float length)
    {
        GameObject go = Create(position + direction * 0.85f, new Color(1f, 0.82f, 0.42f, 0.85f), length * 1.55f, 0.22f, 0.13f);
        go.transform.right = direction;
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
        GameObject go = Create(center, color, length, width, lifetime);
        go.transform.right = direction;
        go.name = "Root thorn attack";
    }

    public static void Ring(Vector2 center, float diameter, Color color, float lifetime)
    {
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
