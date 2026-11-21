using UnityEngine;

/// <summary>Compact world-space health bar drawn above a player or enemy.</summary>
public sealed class ScadrykHealthBar : MonoBehaviour
{
    private ScadrykPlayerController player;
    private ScadrykEnemyHealth enemy;
    private Transform fill;
    private GameObject barObject;
    private float barWidth = 0.82f;

    public void Configure(bool forPlayer, bool boss, float height)
    {
        player = forPlayer ? GetComponent<ScadrykPlayerController>() : null;
        enemy = forPlayer ? null : GetComponent<ScadrykEnemyHealth>();
        barWidth = boss ? 1.04f : 0.82f;

        var bar = new GameObject("Floating health bar");
        barObject = bar;
        bar.transform.SetParent(transform, false);
        float parentScale = Mathf.Max(0.01f, transform.lossyScale.y);
        bar.transform.localPosition = Vector3.up * (height / parentScale);
        bar.transform.localScale = Vector3.one / Mathf.Max(0.01f, transform.lossyScale.x);

        CreateBarPiece(bar.transform, "Health bar background", new Vector2(barWidth + 0.12f, 0.14f), new Color(0.025f, 0.035f, 0.04f, 0.94f), 150);
        var healthFill = CreateBarPiece(bar.transform, "Health bar fill", new Vector2(barWidth, 0.075f), forPlayer ? new Color(0.36f, 0.88f, 0.53f) : boss ? new Color(1f, 0.68f, 0.26f) : new Color(0.9f, 0.3f, 0.27f), 151);
        fill = healthFill.transform;
    }

    public void Hide() { if (barObject != null) barObject.SetActive(false); }

    private static SpriteRenderer CreateBarPiece(Transform parent, string objectName, Vector2 size, Color color, int order)
    {
        var piece = new GameObject(objectName);
        piece.transform.SetParent(parent, false);
        piece.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = ScadrykEffects.WhiteSprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }

    private void LateUpdate()
    {
        if (fill == null) return;
        float health = player != null ? player.Health01 : enemy != null ? enemy.Health01 : 0f;
        fill.localScale = new Vector3(barWidth * health, 0.075f, 1f);
        fill.localPosition = new Vector3(-barWidth * (1f - health) * 0.5f, 0f, -0.01f);
    }
}
