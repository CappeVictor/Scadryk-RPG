using UnityEngine;

/// <summary>Fast linear projectile shared by basin creatures and the Arauto.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class ScadrykEnemyProjectile : MonoBehaviour
{
    private const float ColliderRadius = 0.45f;
    internal const float ForestHitSlowDuration = 0.42f;
    internal const float SaltHitSlowDuration = 0.62f;
    internal const float CoastalHitKnockbackSpeed = 2.4f;
    internal const float CoastalHitKnockbackDuration = 0.12f;
    private Rigidbody2D body;
    private float damage;
    private float expiresAt;
    private float travelDistanceLimit;
    private int hitEffectBiome = -1;
    internal float TravelDistanceLimit => travelDistanceLimit;

    internal static float HitboxDiameter(float spriteScale) => ColliderRadius * 2f * spriteScale;
    internal static float HitboxRadius(float spriteScale) => ColliderRadius * spriteScale;

#if UNITY_6000_0_OR_NEWER
    private Vector2 BodyVelocity { set => body.linearVelocity = value; }
#else
    private Vector2 BodyVelocity { set => body.velocity = value; }
#endif

    public static void Launch(Vector2 position, Vector2 direction, float speed, float damage, Sprite projectileSprite = null, Color? tint = null, float size = 0.34f, string projectileName = "Salt crystal shard", float travelDistance = -1f, int hitEffectBiome = -1, Transform roomRoot = null)
    {
        var shard = new GameObject(projectileName);
        if (roomRoot != null)
            shard.transform.SetParent(roomRoot, true);
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
        projectile.Configure(direction, speed, damage, travelDistance, hitEffectBiome);
    }

    private void Configure(Vector2 direction, float speed, float hitDamage, float travelDistance, int effectBiome)
    {
        body = GetComponent<Rigidbody2D>();
        damage = hitDamage;
        hitEffectBiome = effectBiome >= 0 ? Mathf.Clamp(effectBiome, 0, 2) : -1;
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
            bool damageApplied = target.TakeDamage(damage);
            if (damageApplied) ApplyBiomeHitEffect(target, transform.position, hitEffectBiome);
            ScadrykEffects.Burst(transform.position, ImpactColor(hitEffectBiome), 0.72f);
        }
        else if (other.isTrigger) return;
        Destroy(gameObject);
    }

    internal static Color ImpactColor(int biome)
    {
        if (biome < 0) return new Color(0.93f, 0.36f, 0.67f, 0.82f);
        return Mathf.Clamp(biome, 0, 2) switch
        {
            0 => new Color(0.58f, 0.88f, 0.43f, 0.82f),
            1 => new Color(1f, 0.69f, 0.29f, 0.82f),
            _ => new Color(0.34f, 0.82f, 0.94f, 0.82f)
        };
    }

    internal static string LabelForBiome(int biome) => Mathf.Clamp(biome, 0, 2) switch
    {
        0 => "espinho lançado",
        1 => "estilhaço de sal",
        _ => "fragmento de sino"
    };

    internal static void ApplyBiomeHitEffect(ScadrykPlayerController target, Vector2 projectileOrigin, int biome)
    {
        if (target == null || biome < 0) return;
        switch (Mathf.Clamp(biome, 0, 2))
        {
            case 0: target.ApplyRootSlow(ForestHitSlowDuration); break;
            case 1: target.ApplySaltCrustSlow(SaltHitSlowDuration); break;
            default: target.ApplyImpactKnockback(projectileOrigin, CoastalHitKnockbackSpeed, CoastalHitKnockbackDuration); break;
        }
    }
}
