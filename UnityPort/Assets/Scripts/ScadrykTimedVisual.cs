using UnityEngine;

public sealed class ScadrykTimedVisual : MonoBehaviour
{
    private float lifetime, age;
    private Color initialColor;
    private bool expanding;
    public void Initialize(float duration, Color color, bool expand)
    {
        lifetime = duration;
        initialColor = color;
        expanding = expand;
    }
    private void Update()
    {
        age += Time.deltaTime;
        float progress = Mathf.Clamp01(age / lifetime);
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) { var c = initialColor; c.a *= 1f - progress; renderer.color = c; }
        if (expanding) transform.localScale *= 1f + Time.deltaTime * 4f;
        if (age >= lifetime) Destroy(gameObject);
    }
}
