using UnityEngine;

public enum ScadrykAmbientMotion
{
    None,
    Sway,
    Glow,
    Water,
    Flicker,
    Flow
}

/// <summary>Small looping motion for trees, crystals, pools and lanterns.</summary>
public sealed class ScadrykAmbientProp : MonoBehaviour
{
    private ScadrykAmbientMotion motion;
    private SpriteRenderer sprite;
    private Vector3 basePosition;
    private Vector3 baseScale;
    private Quaternion baseRotation;
    private Color baseColor;
    private Transform revealFocus;
    private float revealRadius;
    private float hiddenOpacity;
    private float phase, strength;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        basePosition = transform.localPosition;
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
        if (sprite != null) baseColor = sprite.color;
        phase = Random.value * Mathf.PI * 2f;
    }

    public void Configure(ScadrykAmbientMotion type, float motionStrength = 1f)
    {
        motion = type;
        strength = Mathf.Max(0f, motionStrength);
    }

    public void ConfigureFlow(float motionStrength, float normalizedPhase)
    {
        Configure(ScadrykAmbientMotion.Flow, motionStrength);
        phase = Mathf.Repeat(normalizedPhase, 1f) * Mathf.PI * 2f;
    }

    public void ConfigureRevealNear(Transform focus, float radius, float distantOpacity = 0.2f)
    {
        revealFocus = focus;
        revealRadius = Mathf.Max(0.01f, radius);
        hiddenOpacity = Mathf.Clamp01(distantOpacity);
    }

    internal bool RevealsFor(Transform focus) => focus != null && revealFocus == focus;

    private void Update()
    {
        float wave = Mathf.Sin(Time.time * 1.1f + phase);
        switch (motion)
        {
            case ScadrykAmbientMotion.Sway:
                transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, wave * 2.2f * strength);
                break;
            case ScadrykAmbientMotion.Glow:
                if (sprite != null)
                {
                    float visibility = revealFocus == null ? 1f : RevealAlpha(Vector2.Distance(transform.position, revealFocus.position), revealRadius, hiddenOpacity);
                    sprite.color = baseColor * visibility * (0.9f + (wave + 1f) * 0.08f * strength);
                }
                break;
            case ScadrykAmbientMotion.Water:
                float ripple = 1f + wave * 0.035f * strength;
                transform.localScale = new Vector3(baseScale.x * ripple, baseScale.y / ripple, baseScale.z);
                if (sprite != null) sprite.color = baseColor * (0.94f + (wave + 1f) * 0.035f * strength);
                break;
            case ScadrykAmbientMotion.Flicker:
                if (sprite != null)
                {
                    float flicker = 0.82f + 0.12f * Mathf.Abs(Mathf.Sin(Time.time * 5.3f + phase)) + 0.06f * Mathf.Abs(Mathf.Sin(Time.time * 9.7f + phase * 0.7f));
                    sprite.color = baseColor * Mathf.Lerp(1f, flicker, Mathf.Clamp01(strength));
                }
                break;
            case ScadrykAmbientMotion.Flow:
                float normalizedPhase = phase / (Mathf.PI * 2f);
                transform.localPosition = basePosition + Vector3.right * FlowMarkerOffset(Time.time, normalizedPhase, 0.55f, 0.34f * Mathf.Clamp01(strength));
                break;
        }
    }

    internal static float FlowMarkerOffset(float time, float normalizedPhase, float cyclesPerSecond, float travelDistance) =>
        (Mathf.Repeat(Mathf.Max(0f, time) * Mathf.Max(0f, cyclesPerSecond) + Mathf.Repeat(normalizedPhase, 1f), 1f) - 0.5f) * Mathf.Max(0f, travelDistance);

    internal static float RevealAlpha(float distance, float radius, float distantOpacity)
    {
        float proximity = 1f - Mathf.Clamp01(Mathf.Max(0f, distance) / Mathf.Max(0.01f, radius));
        proximity *= proximity * (3f - 2f * proximity);
        return Mathf.Lerp(Mathf.Clamp01(distantOpacity), 1f, proximity);
    }
}
