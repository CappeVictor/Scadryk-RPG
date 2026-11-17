using UnityEngine;

/// <summary>Lightweight hit and dash visuals made from Unity's built-in white sprite.</summary>
public static class ScadrykEffects
{
    private static Sprite whiteSprite;
    private static Sprite WhiteSprite
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
