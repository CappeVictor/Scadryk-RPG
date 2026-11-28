using UnityEngine;

/// <summary>Solid scenery provides cover without changing magical area attacks.</summary>
public static class ScadrykCombatRules
{
    private const int RaycastHitCapacity = 64;
    private static readonly RaycastHit2D[] RaycastHits = new RaycastHit2D[RaycastHitCapacity];
    private static readonly ContactFilter2D WorldQueryFilter = CreateWorldQueryFilter();

    internal static bool IsValidDamageAmount(float amount) =>
        amount > 0f && !float.IsNaN(amount) && !float.IsInfinity(amount);

    internal static float ApplyHealing(float currentHealth, float maximumHealth, float amount)
    {
        if (!IsValidDamageAmount(maximumHealth)) return 0f;
        float boundedHealth = float.IsNaN(currentHealth) || float.IsInfinity(currentHealth)
            ? 0f
            : Mathf.Clamp(currentHealth, 0f, maximumHealth);
        if (!IsValidDamageAmount(amount)) return boundedHealth;
        return Mathf.Min(maximumHealth, boundedHealth + amount);
    }

    private static ContactFilter2D CreateWorldQueryFilter()
    {
        var filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.DefaultRaycastLayers);
        filter.useTriggers = false;
        return filter;
    }

    public static bool WorldBlocks(Vector2 origin, Vector2 target)
    {
        Vector2 delta = target - origin;
        float distance = delta.magnitude;
        if (distance < 0.05f) return false;
        return FirstWorldBlockDistance(origin, delta / distance, distance) < distance;
    }

    public static Vector2 ClipWarningLineEnd(Vector2 start, Vector2 end, float warningWidth)
    {
        Vector2 delta = end - start;
        float distance = delta.magnitude;
        if (distance < 0.05f) return end;
        float blockDistance = FirstWorldBlockDistance(start, delta / distance, distance);
        if (blockDistance >= distance) return end;
        float visibleDistance = Mathf.Max(0f, blockDistance - Mathf.Max(0f, warningWidth) * 0.5f);
        return start + delta / distance * visibleDistance;
    }

    private static float FirstWorldBlockDistance(Vector2 origin, Vector2 direction, float distance)
    {
        int hitCount = Physics2D.Raycast(origin, direction, WorldQueryFilter, RaycastHits, distance);
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D collider = RaycastHits[i].collider;
            if (collider == null || collider.isTrigger || !collider.enabled) continue;
            if (collider.TryGetComponent<ScadrykPlayerController>(out _) ||
                collider.TryGetComponent<ScadrykEnemyHealth>(out _)) continue;
            nearestDistance = Mathf.Min(nearestDistance, RaycastHits[i].distance);
        }
        return hitCount >= RaycastHits.Length ?
            Mathf.Min(nearestDistance, Mathf.Max(0f, distance - 0.01f)) : nearestDistance;
    }
}
