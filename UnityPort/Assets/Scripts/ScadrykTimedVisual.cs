using UnityEngine;

public sealed class ScadrykTimedVisual : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer warningOutlineRenderer;
    private LineRenderer lineRenderer;
    private float lifetime, age;
    private Color initialColor;
    private bool expanding;
    private bool dangerWarning;
    private bool widenDangerWarning;
    private float baseDangerWarningLineStartWidth, baseDangerWarningLineEndWidth;
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
    internal void MarkDangerWarning(Color baseColor, bool widenShape = false)
    {
        dangerWarning = true;
        widenDangerWarning = widenShape || lineRenderer != null;
        initialColor = baseColor;
        EnsureDangerWarningOutline();
        if (lineRenderer != null)
        {
            baseDangerWarningLineStartWidth = lineRenderer.startWidth;
            baseDangerWarningLineEndWidth = lineRenderer.endWidth;
        }
        Color warningColor = ScadrykEffects.DangerWarningColor(initialColor);
        if (spriteRenderer != null) spriteRenderer.color = warningColor;
        if (lineRenderer != null)
        {
            lineRenderer.startColor = warningColor;
            lineRenderer.endColor = warningColor;
        }
        RefreshDangerWarningGeometry();
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
        if (warningOutlineRenderer != null)
        {
            Color outline = new Color(0.012f, 0.018f, 0.024f, Mathf.Clamp01(warningColor.a * 0.86f));
            warningOutlineRenderer.color = outline;
        }
        if (lineRenderer != null)
        {
            lineRenderer.startColor = warningColor;
            lineRenderer.endColor = warningColor;
        }
        RefreshDangerWarningGeometry();
    }

    private void RefreshDangerWarningGeometry()
    {
        if (!widenDangerWarning) return;
        float widthScale = ScadrykSettings.HighContrastWarnings ? 1.4f : 1f;
        if (spriteRenderer != null)
            transform.localScale = new Vector3(initialScale.x, initialScale.y * widthScale, initialScale.z);
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = baseDangerWarningLineStartWidth * widthScale;
            lineRenderer.endWidth = baseDangerWarningLineEndWidth * widthScale;
        }
    }

    private void EnsureDangerWarningOutline()
    {
        if (spriteRenderer == null || warningOutlineRenderer != null) return;
        GameObject outline = new GameObject("Danger warning contrast edge");
        outline.transform.SetParent(transform, false);
        outline.transform.localPosition = Vector3.zero;
        outline.transform.localScale = widenDangerWarning
            ? new Vector3(1.015f, 1.18f, 1f)
            : new Vector3(1.12f, 1.12f, 1f);
        warningOutlineRenderer = outline.AddComponent<SpriteRenderer>();
        warningOutlineRenderer.sprite = spriteRenderer.sprite;
        warningOutlineRenderer.sharedMaterial = spriteRenderer.sharedMaterial;
        warningOutlineRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        warningOutlineRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        warningOutlineRenderer.maskInteraction = spriteRenderer.maskInteraction;
        warningOutlineRenderer.flipX = spriteRenderer.flipX;
        warningOutlineRenderer.flipY = spriteRenderer.flipY;
        warningOutlineRenderer.color = new Color(0.012f, 0.018f, 0.024f, Mathf.Clamp01(spriteRenderer.color.a * 0.86f));
    }

    internal static float DangerWarningAlpha(float progress) =>
        Mathf.Lerp(0.78f, 1f, Mathf.SmoothStep(0.08f, 0.72f, Mathf.Clamp01(progress)));

    internal static Vector3 DangerWarningVisualScale(Vector3 baseScale, float expansionScale, bool highContrast, bool widenShape)
    {
        float widthScale = highContrast && widenShape ? 1.4f : 1f;
        return new Vector3(baseScale.x * expansionScale, baseScale.y * expansionScale * widthScale,
            baseScale.z * expansionScale);
    }

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
        if (expanding)
        {
            float expansionScale = ScadrykEffects.FlashExpansionScale(progress, ScadrykSettings.ReduceFlash);
            transform.localScale = dangerWarning
                ? DangerWarningVisualScale(initialScale, expansionScale, ScadrykSettings.HighContrastWarnings, widenDangerWarning)
                : initialScale * expansionScale;
        }
        if (spinDegreesPerSecond != 0f) transform.Rotate(0f, 0f, spinDegreesPerSecond * Time.deltaTime);
        if (age >= lifetime) Destroy(gameObject);
    }
}
