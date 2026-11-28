using UnityEngine;

/// <summary>Opens, idles and collapses a phase portal sprite.</summary>
public sealed class ScadrykPortalVisual : MonoBehaviour
{
    private const float OpenDuration = 0.78f;
    private const float CloseDuration = 0.48f;
    private Transform glow;
    private Vector3 fullScale;
    private float startedAt;
    private float closeStartScale;
    private float scaleFactor;
    private bool closing;

    public bool IsOpen { get; private set; }
    public bool IsClosed => closing && Time.time - startedAt >= CloseDuration;
    public float Diameter { get; private set; }

    public static GameObject Spawn(Transform parent, Vector2 position, Sprite sprite, Color accent, float diameter)
    {
        var portal = new GameObject("Ancient phase portal");
        portal.transform.SetParent(parent, true);
        portal.transform.position = position;
        var visual = portal.AddComponent<ScadrykPortalVisual>();
        visual.Initialize(sprite, accent, diameter);
        return portal;
    }

    private void Initialize(Sprite sprite, Color accent, float diameter)
    {
        Diameter = Mathf.Max(0f, diameter);
        if (sprite == null) return;
        var frame = gameObject.AddComponent<SpriteRenderer>();
        frame.sprite = sprite;
        frame.sortingOrder = 14;
        frame.color = Color.Lerp(Color.white, accent, accent.r > 0.9f && accent.g < 0.75f ? 0.62f : 0.22f);
        float baseDiameter = sprite.bounds.size.x;
        float scale = Diameter / Mathf.Max(0.01f, baseDiameter);
        fullScale = Vector3.one * scale;
        transform.localScale = fullScale * 0.04f;

        var glowObject = new GameObject("Portal aura");
        glowObject.transform.SetParent(transform, false);
        var glowRenderer = glowObject.AddComponent<SpriteRenderer>();
        glowRenderer.sprite = sprite;
        glowRenderer.color = new Color(accent.r, accent.g, accent.b, 0.2f);
        glowRenderer.sortingOrder = 13;
        glowObject.transform.localScale = Vector3.one * 1.12f;
        glow = glowObject.transform;
        startedAt = Time.time;
        scaleFactor = 0.04f;
    }

    private void Update()
    {
        if (closing)
        {
            float t = Mathf.Clamp01((Time.time - startedAt) / CloseDuration);
            scaleFactor = Mathf.Lerp(closeStartScale, 0f, Mathf.SmoothStep(0f, 1f, t));
            if (t >= 1f) { Destroy(gameObject); return; }
        }
        else if (!IsOpen)
        {
            float t = Mathf.Clamp01((Time.time - startedAt) / OpenDuration);
            scaleFactor = Mathf.Lerp(0.04f, 1f, Mathf.SmoothStep(0f, 1f, t));
            if (t >= 1f) IsOpen = true;
        }
        else
            scaleFactor = 0.97f + Mathf.Sin(Time.time * 3.4f) * 0.045f;

        transform.localScale = fullScale * scaleFactor;
        transform.Rotate(0f, 0f, Time.deltaTime * (closing ? 120f : -24f));
        if (glow != null) glow.localRotation = Quaternion.Euler(0f, 0f, -transform.eulerAngles.z * 1.3f);
    }

    public void BeginClose()
    {
        if (closing) return;
        closing = true;
        closeStartScale = scaleFactor;
        startedAt = Time.time;
    }
}
