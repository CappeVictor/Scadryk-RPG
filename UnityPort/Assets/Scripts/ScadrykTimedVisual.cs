using UnityEngine;

public sealed class ScadrykTimedVisual : MonoBehaviour
{
    private float lifetime, age;
    private Color initialColor;
    private bool expanding;
    private float spinDegreesPerSecond;
    public void Initialize(float duration, Color color, bool expand)
    {
        lifetime = duration;
        initialColor = color;
        expanding = expand;
    }
    public void SetSpin(float degreesPerSecond) => spinDegreesPerSecond = degreesPerSecond;
    private void Update()
    {
        age += Time.deltaTime;
        float progress = Mathf.Clamp01(age / lifetime);
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) { var c = initialColor; c.a *= 1f - progress; renderer.color = c; }
        if (expanding) transform.localScale *= 1f + Time.deltaTime * 4f;
        if (spinDegreesPerSecond != 0f) transform.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
        if (age >= lifetime) Destroy(gameObject);
    }
}
