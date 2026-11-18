using UnityEngine;

/// <summary>Fast salt crystal fired by the basin creatures.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class ScadrykEnemyProjectile : MonoBehaviour
{
    private Rigidbody2D body;
    private float damage;
    private float expiresAt;

#if UNITY_6000_0_OR_NEWER
    private Vector2 BodyVelocity { set => body.linearVelocity = value; }
#else
    private Vector2 BodyVelocity { set => body.velocity = value; }
#endif

    public static void Launch(Vector2 position, Vector2 direction, float speed, float damage)
    {
        var shard = new GameObject("Salt crystal shard");
        shard.transform.position = position;
        shard.transform.localScale = Vector3.one * 0.34f;
        var renderer = shard.AddComponent<SpriteRenderer>();
        renderer.sprite = ScadrykEffects.SaltShardSprite ?? ScadrykEffects.WhiteSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 8;
        shard.transform.up = direction;
        var collider = shard.AddComponent<CircleCollider2D>();
        collider.radius = 0.45f;
        collider.isTrigger = true;
        var body = shard.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var projectile = shard.AddComponent<ScadrykEnemyProjectile>();
        projectile.Configure(direction, speed, damage);
    }

    private void Configure(Vector2 direction, float speed, float hitDamage)
    {
        body = GetComponent<Rigidbody2D>();
        damage = hitDamage;
        expiresAt = Time.time + 3.2f;
        BodyVelocity = direction.normalized * speed;
    }

    private void Update()
    {
        if (Time.time >= expiresAt) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<ScadrykPlayerController>(out var target)) return;
        target.TakeDamage(damage);
        ScadrykEffects.Burst(transform.position, new Color(1f, 0.75f, 0.3f, 0.82f), 0.72f);
        Destroy(gameObject);
    }
}
