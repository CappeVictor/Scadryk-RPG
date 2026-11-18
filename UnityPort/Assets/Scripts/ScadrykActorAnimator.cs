using UnityEngine;

/// <summary>Small readable squash, bob and lunge animation for top-down actors.</summary>
public sealed class ScadrykActorAnimator : MonoBehaviour
{
    private Rigidbody2D body;
    private Transform visual;
    private Vector3 baseScale = Vector3.one;
    private Vector3 basePosition;
    private Vector2 attackDirection = Vector2.up;
    private float attackStartedAt = -10f;
    private float attackDuration = 0.28f;
    private float attackStrength = 1f;

    private void Awake() => body = GetComponent<Rigidbody2D>();

    public void SetVisual(Transform target)
    {
        visual = target;
        if (visual != null) { baseScale = visual.localScale; basePosition = visual.localPosition; }
    }

    public void PlayAttack(Vector2 direction, bool heavy)
    {
        if (direction.sqrMagnitude > 0.01f) attackDirection = direction.normalized;
        attackStartedAt = Time.time;
        attackDuration = heavy ? 0.38f : 0.26f;
        attackStrength = heavy ? 1.35f : 1f;
    }

    private void LateUpdate()
    {
        if (visual == null) return;
        float speed = body != null ? BodyVelocity.magnitude : 0f;
        float stride = speed > 0.15f ? Mathf.Abs(Mathf.Sin(Time.time * 14f)) : 0f;
        float attack = 0f;
        if (Time.time - attackStartedAt < attackDuration)
            attack = Mathf.Sin(Mathf.Clamp01((Time.time - attackStartedAt) / attackDuration) * Mathf.PI) * attackStrength;

        visual.localPosition = basePosition + Vector3.up * (stride * 0.055f) + (Vector3)(attackDirection * (attack * 0.14f));
        visual.localScale = new Vector3(
            baseScale.x * (1f + stride * 0.045f + attack * 0.13f),
            baseScale.y * (1f - stride * 0.055f - attack * 0.06f),
            baseScale.z);
    }

#if UNITY_6000_0_OR_NEWER
    private Vector2 BodyVelocity => body.linearVelocity;
#else
    private Vector2 BodyVelocity => body.velocity;
#endif
}
