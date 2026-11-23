using UnityEngine;

public sealed class ScadrykTimedVisual : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private LineRenderer lineRenderer;
    private float lifetime, age;
    private Color initialColor;
    private bool expanding;
    private bool dangerWarning;
    private float spinDegreesPerSecond;
    private Vector3 initialScale;
    internal bool IsDangerWarning => dangerWarning;

    private void Awake() => CacheRenderers();

    private void CacheRenderers()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
    }

    public void Initialize(float duration, Color color, bool expand)
    {
        CacheRenderers();
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
        if (spriteRenderer != null)
        {
            var c = initialColor;
            c.a *= dangerWarning ? DangerWarningAlpha(progress) : 1f - Mathf.SmoothStep(0.45f, 1f, progress);
            spriteRenderer.color = c;
        }
        LineRenderer line = dangerWarning ? lineRenderer : null;
        if (line != null)
        {
            Color warningColor = initialColor;
            warningColor.a *= DangerWarningAlpha(progress);
            line.startColor = warningColor;
            line.endColor = warningColor;
        }
        if (expanding) transform.localScale = initialScale * ScadrykEffects.FlashExpansionScale(progress, ScadrykSettings.ReduceFlash);
        if (spinDegreesPerSecond != 0f) transform.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
        if (age >= lifetime) Destroy(gameObject);
    }
}
