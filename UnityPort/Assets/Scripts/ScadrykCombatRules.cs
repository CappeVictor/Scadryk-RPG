using UnityEngine;

/// <summary>Solid scenery provides cover without changing magical area attacks.</summary>
public static class ScadrykCombatRules
{
    private const int RaycastHitCapacity = 64;
    private static readonly RaycastHit2D[] RaycastHits = new RaycastHit2D[RaycastHitCapacity];
    private static readonly ContactFilter2D WorldQueryFilter = CreateWorldQueryFilter();

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
        int hitCount = Physics2D.Raycast(origin, delta / distance, WorldQueryFilter, RaycastHits, distance);
        if (hitCount >= RaycastHits.Length) return true;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D collider = RaycastHits[i].collider;
            if (collider == null || collider.isTrigger || !collider.enabled) continue;
            if (collider.GetComponent<ScadrykPlayerController>() != null || collider.GetComponent<ScadrykEnemyHealth>() != null) continue;
            return true;
        }
        return false;
    }
}
