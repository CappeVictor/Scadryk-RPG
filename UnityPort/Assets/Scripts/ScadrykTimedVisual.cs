using UnityEngine;

public sealed class ScadrykTimedVisual : MonoBehaviour
{
    private float lifetime, age;
    private Color initialColor;
    private bool expanding;
    private bool dangerWarning;
    private float spinDegreesPerSecond;
    private Vector3 initialScale;
    internal bool IsDangerWarning => dangerWarning;
    public void Initialize(float duration, Color color, bool expand)
    {
        lifetime = duration;
        initialColor = color;
        expanding = expand;
        initialScale = transform.localScale;
    }
    public void SetSpin(float degreesPerSecond) => spinDegreesPerSecond = degreesPerSecond;
    internal void MarkDangerWarning() => dangerWarning = true;

    internal static float DangerWarningAlpha(float progress) =>
        Mathf.Lerp(0.78f, 1f, Mathf.SmoothStep(0.08f, 0.72f, Mathf.Clamp01(progress)));

    private void Update()
    {
        age += Time.deltaTime;
        float progress = Mathf.Clamp01(age / Mathf.Max(0.01f, lifetime));
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            var c = initialColor;
            c.a *= dangerWarning ? DangerWarningAlpha(progress) : 1f - Mathf.SmoothStep(0.45f, 1f, progress);
            renderer.color = c;
        }
        if (expanding) transform.localScale = initialScale * ScadrykEffects.FlashExpansionScale(progress, ScadrykSettings.ReduceFlash);
        if (spinDegreesPerSecond != 0f) transform.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
        if (age >= lifetime) Destroy(gameObject);
    }
}
