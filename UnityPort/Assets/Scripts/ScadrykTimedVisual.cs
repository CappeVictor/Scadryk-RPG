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
    internal void MarkDangerWarning(Color baseColor)
    {
        dangerWarning = true;
        initialColor = baseColor;
        Color warningColor = ScadrykEffects.DangerWarningColor(initialColor);
        if (spriteRenderer != null) spriteRenderer.color = warningColor;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = warningColor;
            lineRenderer.endColor = warningColor;
        }
    }

    internal void MarkDangerWarning()
    {
        if (dangerWarning) return;
        MarkDangerWarning(initialColor);
    }

    internal void RefreshDangerWarningColor(float progress)
    {
        if (!dangerWarning) return;
        Color warningColor = ScadrykEffects.DangerWarningColor(initialColor);
        warningColor.a *= DangerWarningAlpha(progress);
        if (spriteRenderer != null) spriteRenderer.color = warningColor;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = warningColor;
            lineRenderer.endColor = warningColor;
        }
    }

    internal static float DangerWarningAlpha(float progress) =>
        Mathf.Lerp(0.78f, 1f, Mathf.SmoothStep(0.08f, 0.72f, Mathf.Clamp01(progress)));

    private void Update()
    {
        age += Time.deltaTime;
        float progress = Mathf.Clamp01(age / Mathf.Max(0.01f, lifetime));
        if (dangerWarning)
        {
            RefreshDangerWarningColor(progress);
        }
        else if (spriteRenderer != null)
        {
            var c = initialColor;
            c.a *= 1f - Mathf.SmoothStep(0.45f, 1f, progress);
            spriteRenderer.color = c;
        }
        if (expanding) transform.localScale = initialScale * ScadrykEffects.FlashExpansionScale(progress, ScadrykSettings.ReduceFlash);
        if (spinDegreesPerSecond != 0f) transform.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
        if (age >= lifetime) Destroy(gameObject);
    }
}
