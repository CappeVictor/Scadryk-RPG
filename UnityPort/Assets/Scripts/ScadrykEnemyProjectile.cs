using UnityEngine;

/// <summary>Fast linear projectile shared by basin creatures and the Arauto.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class ScadrykEnemyProjectile : MonoBehaviour
{
    private const float ColliderRadius = 0.45f;
    private Rigidbody2D body;
    private float damage;
    private float expiresAt;
    private float travelDistanceLimit;
    internal float TravelDistanceLimit => travelDistanceLimit;

    internal static float HitboxDiameter(float spriteScale) => ColliderRadius * 2f * spriteScale;
    internal static float HitboxRadius(float spriteScale) => ColliderRadius * spriteScale;

#if UNITY_6000_0_OR_NEWER
    private Vector2 BodyVelocity { set => body.linearVelocity = value; }
#else
    private Vector2 BodyVelocity { set => body.velocity = value; }
#endif

    public static void Launch(Vector2 position, Vector2 direction, float speed, float damage, Sprite projectileSprite = null, Color? tint = null, float size = 0.34f, string projectileName = "Salt crystal shard", float travelDistance = -1f)
    {
        var shard = new GameObject(projectileName);
        ScadrykGameDirector director = FindAnyObjectByType<ScadrykGameDirector>();
        if (director != null && director.ActiveRoomRoot != null)
            shard.transform.SetParent(director.ActiveRoomRoot, true);
        shard.transform.position = position;
        shard.transform.localScale = Vector3.one * size;
        var renderer = shard.AddComponent<SpriteRenderer>();
        renderer.sprite = projectileSprite != null ? projectileSprite : ScadrykEffects.SaltShardSprite ?? ScadrykEffects.WhiteSprite;
        renderer.color = tint ?? Color.white;
        renderer.sortingOrder = 8;
        shard.transform.up = direction;
        var collider = shard.AddComponent<CircleCollider2D>();
        collider.radius = ColliderRadius;
        collider.isTrigger = true;
        var body = shard.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var projectile = shard.AddComponent<ScadrykEnemyProjectile>();
        projectile.Configure(direction, speed, damage, travelDistance);
    }

    private void Configure(Vector2 direction, float speed, float hitDamage, float travelDistance)
    {
        body = GetComponent<Rigidbody2D>();
        damage = hitDamage;
        travelDistanceLimit = travelDistance >= 0f && speed > 0f ? travelDistance : Mathf.Max(0f, speed) * 3.2f;
        float lifetime = speed > 0f ? travelDistanceLimit / speed : 3.2f;
        expiresAt = Time.time + lifetime;
        BodyVelocity = direction.normalized * speed;
    }

    private void Update()
    {
        if (Time.time >= expiresAt) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<ScadrykEnemyHealth>(out _)) return;
        if (other.TryGetComponent<ScadrykPlayerController>(out var target))
        {
            target.TakeDamage(damage);
            ScadrykEffects.Burst(transform.position, new Color(0.93f, 0.36f, 0.67f, 0.82f), 0.72f);
        }
        else if (other.isTrigger) return;
        Destroy(gameObject);
    }
}
