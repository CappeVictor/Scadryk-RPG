using UnityEngine;

/// <summary>A readable, non-damaging ground effect that changes Nara's movement.</summary>
[RequireComponent(typeof(Collider2D))]
public sealed class ScadrykTerrainZone : MonoBehaviour
{
    [SerializeField, Range(0.4f, 1f)] private float movementScale = 0.78f;

    public float MovementScale => Mathf.Clamp(movementScale, 0.4f, 1f);

    public void Configure(float scale)
    {
        movementScale = Mathf.Clamp(scale, 0.4f, 1f);
        Collider2D zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider != null) zoneCollider.isTrigger = true;
    }
}
