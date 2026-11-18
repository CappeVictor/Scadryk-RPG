using UnityEngine;

/// <summary>Moves a short lived combat sprite along its cast direction.</summary>
public sealed class ScadrykEffectDrift : MonoBehaviour
{
    private Vector2 velocity;

    public void Initialize(Vector2 initialVelocity) => velocity = initialVelocity;

    private void Update()
    {
        transform.position += (Vector3)(velocity * Time.deltaTime);
        velocity = Vector2.MoveTowards(velocity, Vector2.zero, 5f * Time.deltaTime);
    }
}
