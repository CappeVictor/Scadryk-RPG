using UnityEngine;

/// <summary>Small readable squash, bob and lunge animation for top-down actors.</summary>
public sealed class ScadrykActorAnimator : MonoBehaviour
{
    internal const float DashVisualLungeDistance = 0.14f;
    private Rigidbody2D body;
    private Transform visual;
    private SpriteRenderer visualRenderer;
    private SpriteRenderer extrusionRenderer;
    private Vector3 baseScale = Vector3.one;
    private Vector3 basePosition;
    private Vector2 attackDirection = Vector2.up;
    private float attackStartedAt = -10f;
    private float attackDuration = 0.28f;
    private float attackStrength = 1f;
    private float abilityImpactStartedAt = -10f;
    private float abilityRecoveryDuration;
    private float windupStartedAt = -10f, windupDuration;
    private float portalScale = 1f;
    private float dashUntil, nextTrailAt, hitUntil, deathStartedAt = -1f;
    private float stridePhase;
    private Vector2 dashDirection;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        stridePhase = Random.value * Mathf.PI * 2f;
    }

    public void SetVisual(Transform target, SpriteRenderer extrusion = null)
    {
        visual = target;
        extrusionRenderer = extrusion;
        if (visual != null)
        {
            baseScale = visual.localScale;
            basePosition = visual.localPosition;
            visualRenderer = visual.GetComponent<SpriteRenderer>();
        }
    }

    public void PlayAttack(Vector2 direction, bool heavy)
    {
        if (direction.sqrMagnitude > 0.01f) attackDirection = direction.normalized;
        abilityImpactStartedAt = -10f;
        windupStartedAt = -10f;
        attackStartedAt = Time.time;
        attackDuration = heavy ? 0.38f : 0.26f;
        attackStrength = heavy ? 1.35f : 1f;
    }

    public void PlayAbilityImpact(Vector2 direction, float recoveryDuration)
    {
        PlayAttack(direction, true);
        attackStartedAt = -10f;
        abilityImpactStartedAt = Time.time;
        abilityRecoveryDuration = Mathf.Max(0.05f, recoveryDuration);
    }

    internal static float AbilityImpactStrength(float elapsed, float recoveryDuration)
    {
        float duration = Mathf.Max(0.05f, recoveryDuration);
        float progress = Mathf.Clamp01(0.5f + Mathf.Max(0f, elapsed) / (2f * duration));
        return Mathf.Sin(progress * Mathf.PI);
    }

    public void PlayWindup(Vector2 direction, float duration)
    {
        if (direction.sqrMagnitude > 0.01f) attackDirection = direction.normalized;
        abilityImpactStartedAt = -10f;
        windupStartedAt = Time.time;
        windupDuration = Mathf.Max(0.05f, duration);
        attackStartedAt = -10f;
    }

    public void CancelWindup() => windupStartedAt = -10f;

    internal static float WindupStrength(float now, float startedAt, float duration) =>
        Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((now - startedAt) / Mathf.Max(0.05f, duration)));

    internal static float StrideWave(float time, float frequency, float phase) =>
        Mathf.Sin(Mathf.Max(0f, time) * Mathf.Max(0f, frequency) + phase);

    internal static Vector2 DashVisualOffset(Vector2 direction, float strength) => direction.sqrMagnitude > 0.001f
        ? direction.normalized * (Mathf.Clamp01(strength) * DashVisualLungeDistance)
        : Vector2.zero;

    public void SetPortalScale(float scale) => portalScale = Mathf.Clamp01(scale);

    public void PlayDash(Vector2 direction)
    {
        dashDirection = direction.normalized;
        dashUntil = Time.time + 0.2f;
        nextTrailAt = 0f;
    }

    public void PlayHit(Vector2 direction)
    {
        attackDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;
        hitUntil = Time.time + 0.24f;
    }

    public void PlayDeath()
    {
        deathStartedAt = Time.time;
        dashUntil = 0f;
        CancelWindup();
    }

    public void ResetPose()
    {
        deathStartedAt = -1f;
        hitUntil = dashUntil = 0f;
        attackStartedAt = abilityImpactStartedAt = -10f;
        windupStartedAt = -10f;
        if (visualRenderer != null) visualRenderer.color = Color.white;
    }

    private void LateUpdate()
    {
        if (visual == null) return;
        if (extrusionRenderer != null && visualRenderer != null)
        {
            extrusionRenderer.sprite = visualRenderer.sprite;
            extrusionRenderer.flipX = visualRenderer.flipX;
            extrusionRenderer.flipY = visualRenderer.flipY;
        }
        float speed = body != null ? BodyVelocity.magnitude : 0f;
        float stride = speed > 0.15f ? StrideWave(Time.time, 9f + speed * 0.7f, stridePhase) : 0f;
        float attack = 0f;
        float abilityImpactElapsed = Time.time - abilityImpactStartedAt;
        if (abilityImpactStartedAt >= 0f && abilityImpactElapsed < abilityRecoveryDuration)
            attack = AbilityImpactStrength(abilityImpactElapsed, abilityRecoveryDuration) * attackStrength;
        else if (Time.time - attackStartedAt < attackDuration)
            attack = Mathf.Sin(Mathf.Clamp01((Time.time - attackStartedAt) / attackDuration) * Mathf.PI) * attackStrength;
        float windup = Time.time < windupStartedAt + windupDuration
            ? WindupStrength(Time.time, windupStartedAt, windupDuration) : 0f;

        float dash = Mathf.Clamp01((dashUntil - Time.time) / 0.2f);
        Vector2 dashOffset = DashVisualOffset(dashDirection, dash);
        float hit = Mathf.Clamp01((hitUntil - Time.time) / 0.24f);
        float death = deathStartedAt < 0f ? 0f : Mathf.Clamp01((Time.time - deathStartedAt) / 0.72f);
        if (dash > 0f && Time.time >= nextTrailAt && visualRenderer != null)
        {
            ScadrykEffects.Afterimage(visualRenderer, new Color(0.48f, 0.88f, 1f, 0.36f));
            nextTrailAt = Time.time + 0.09f;
        }
        visual.localPosition = basePosition + Vector3.up * (Mathf.Abs(stride) * 0.085f - death * 0.22f) +
            (Vector3)dashOffset +
            (Vector3)(attackDirection * (attack * 0.18f - windup * 0.12f - hit * 0.13f));
        visual.localRotation = Quaternion.Euler(0f, 0f, stride * 2.2f + attack * -8f + windup * 4f + dash * -13f * dashDirection.x + death * 82f);
        visual.localScale = new Vector3(
            baseScale.x * (1f + stride * 0.035f + attack * 0.17f + windup * 0.07f + dash * 0.22f - hit * 0.12f) * portalScale * (1f - death * 0.38f),
            baseScale.y * (1f - stride * 0.055f - attack * 0.08f - windup * 0.06f - dash * 0.18f + hit * 0.08f) * portalScale * (1f - death * 0.62f),
            baseScale.z);
        if (visualRenderer != null)
        {
            Color color = hit > 0f ? Color.Lerp(Color.white, new Color(1f, 0.48f, 0.42f), hit) : visualRenderer.color;
            color.a = 1f - death;
            visualRenderer.color = color;
        }
        if (extrusionRenderer != null)
        {
            Color color = extrusionRenderer.color;
            color.a = 0.72f * (1f - death);
            extrusionRenderer.color = color;
        }
    }

#if UNITY_6000_0_OR_NEWER
    private Vector2 BodyVelocity => body.linearVelocity;
#else
    private Vector2 BodyVelocity => body.velocity;
#endif
}
