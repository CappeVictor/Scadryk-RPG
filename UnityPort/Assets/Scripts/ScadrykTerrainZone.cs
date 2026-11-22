using UnityEngine;

/// <summary>A readable, non-damaging ground effect that changes Nara's movement.</summary>
[RequireComponent(typeof(Collider2D))]
public sealed class ScadrykTerrainZone : MonoBehaviour
{
    [SerializeField, Range(0.4f, 1f)] private float movementScale = 0.78f;
    [SerializeField] private Vector2 flowVelocity;
    [SerializeField, TextArea] private string entryHint;
    [SerializeField] private string hintKey;

    public float MovementScale => Mathf.Clamp(movementScale, 0.4f, 1f);
    public Vector2 FlowVelocity => Vector2.ClampMagnitude(flowVelocity, 2.4f);
    public string EntryHint => entryHint;
    public string HintKey => hintKey;

    public void Configure(float scale, Vector2 current = default, string hint = null, string key = "terrain")
    {
        movementScale = Mathf.Clamp(scale, 0.4f, 1f);
        flowVelocity = Vector2.ClampMagnitude(current, 2.4f);
        entryHint = hint;
        hintKey = key;
        Collider2D zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider != null) zoneCollider.isTrigger = true;
    }
}
