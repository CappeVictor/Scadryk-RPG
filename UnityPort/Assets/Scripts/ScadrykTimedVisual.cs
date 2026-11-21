using UnityEngine;

public sealed class ScadrykTimedVisual : MonoBehaviour
{
    private float lifetime, age;
    private Color initialColor;
    private bool expanding;
    private float spinDegreesPerSecond;
    private Vector3 initialScale;
    public void Initialize(float duration, Color color, bool expand)
    {
        lifetime = duration;
        initialColor = color;
        expanding = expand;
        initialScale = transform.localScale;
    }
    public void SetSpin(float degreesPerSecond) => spinDegreesPerSecond = degreesPerSecond;
    private void Update()
    {
        age += Time.deltaTime;
        float progress = Mathf.Clamp01(age / Mathf.Max(0.01f, lifetime));
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) { var c = initialColor; c.a *= 1f - Mathf.SmoothStep(0.45f, 1f, progress); renderer.color = c; }
        if (expanding) transform.localScale = initialScale * ScadrykEffects.FlashExpansionScale(progress, ScadrykSettings.ReduceFlash);
        if (spinDegreesPerSecond != 0f) transform.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
        if (age >= lifetime) Destroy(gameObject);
    }
}
