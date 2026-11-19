using UnityEngine;

/// <summary>Solid scenery provides cover without changing magical area attacks.</summary>
public static class ScadrykCombatRules
{
    public static bool WorldBlocks(Vector2 origin, Vector2 target)
    {
        Vector2 delta = target - origin;
        float distance = delta.magnitude;
        if (distance < 0.05f) return false;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, delta / distance, distance);
        foreach (RaycastHit2D hit in hits)
        {
            Collider2D collider = hit.collider;
            if (collider == null || collider.isTrigger || !collider.enabled) continue;
            if (collider.GetComponent<ScadrykPlayerController>() != null || collider.GetComponent<ScadrykEnemyHealth>() != null) continue;
            return true;
        }
        return false;
    }
}
