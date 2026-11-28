using UnityEngine;

/// <summary>Fast linear projectile shared by basin creatures and the Arauto.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class ScadrykEnemyProjectile : MonoBehaviour
{
    private const float ColliderRadius = 0.45f;
    internal const int ProjectilePoolCapacity = 24;
    private static readonly System.Collections.Generic.List<ScadrykEnemyProjectile> Pool =
        new System.Collections.Generic.List<ScadrykEnemyProjectile>(ProjectilePoolCapacity);
    internal const float ForestHitSlowDuration = 0.42f;
    internal const float SaltHitSlowDuration = 0.62f;
    internal const float CoastalHitKnockbackSpeed = 2.4f;
    internal const float CoastalHitKnockbackDuration = 0.12f;
    private Rigidbody2D body;
    private CircleCollider2D hitbox;
    private SpriteRenderer spriteRenderer;
    private float damage;
    private float expiresAt;
    private float travelDistanceLimit;
    private int hitEffectBiome = -1;
    internal float TravelDistanceLimit => travelDistanceLimit;

    internal static float HitboxDiameter(float spriteScale) => ColliderRadius * 2f * spriteScale;
    internal static float HitboxRadius(float spriteScale) => ColliderRadius * spriteScale;
    internal static bool ProjectileArgumentsAreValid(Vector2 position, Vector2 direction, float speed,
        float hitDamage, float size, float travelDistance)
    {
        float directionMagnitudeSquared = direction.sqrMagnitude;
        return !float.IsNaN(position.x) && !float.IsInfinity(position.x) &&
            !float.IsNaN(position.y) && !float.IsInfinity(position.y) &&
            !float.IsNaN(direction.x) && !float.IsInfinity(direction.x) &&
            !float.IsNaN(direction.y) && !float.IsInfinity(direction.y) &&
            directionMagnitudeSquared > 0.0001f && !float.IsInfinity(directionMagnitudeSquared) &&
            !float.IsNaN(speed) && !float.IsInfinity(speed) && speed > 0f &&
            ScadrykCombatRules.IsValidDamageAmount(hitDamage) &&
            !float.IsNaN(size) && !float.IsInfinity(size) && size > 0f &&
            !float.IsNaN(travelDistance) && !float.IsInfinity(travelDistance) &&
            (travelDistance == -1f || travelDistance >= 0f);
    }

#if UNITY_6000_0_OR_NEWER
    private Vector2 BodyVelocity { set => body.linearVelocity = value; }
#else
    private Vector2 BodyVelocity { set => body.velocity = value; }
#endif

    public static ScadrykEnemyProjectile Launch(Vector2 position, Vector2 direction, float speed, float damage, Sprite projectileSprite = null, Color? tint = null, float size = 0.34f, string projectileName = "Salt crystal shard", float travelDistance = -1f, int hitEffectBiome = -1, Transform roomRoot = null)
    {
        if (!ProjectileArgumentsAreValid(position, direction, speed, damage, size, travelDistance)) return null;
        ScadrykEnemyProjectile projectile = Acquire(roomRoot, projectileName);
        GameObject shard = projectile.gameObject;
        shard.transform.position = position;
        shard.transform.localScale = Vector3.one * size;
        projectile.spriteRenderer.sprite = projectileSprite != null ? projectileSprite : ScadrykEffects.SaltShardSprite ?? ScadrykEffects.WhiteSprite;
        projectile.spriteRenderer.color = tint ?? Color.white;
        projectile.spriteRenderer.sortingOrder = 8;
        shard.transform.up = direction;
        projectile.hitbox.radius = ColliderRadius;
        projectile.hitbox.isTrigger = true;
        projectile.body.gravityScale = 0f;
        projectile.body.bodyType = RigidbodyType2D.Kinematic;
        projectile.body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        projectile.Configure(direction, speed, damage, travelDistance, hitEffectBiome);
        return projectile;
    }

    private static ScadrykEnemyProjectile Acquire(Transform roomRoot, string projectileName)
    {
        ScadrykEnemyProjectile projectile = null;
        while (Pool.Count > 0 && projectile == null)
        {
            int last = Pool.Count - 1;
            projectile = Pool[last];
            Pool.RemoveAt(last);
        }

        if (projectile == null)
        {
            var shard = new GameObject(projectileName);
            projectile = shard.AddComponent<ScadrykEnemyProjectile>();
            projectile.body = shard.GetComponent<Rigidbody2D>();
            projectile.hitbox = shard.GetComponent<CircleCollider2D>();
            projectile.spriteRenderer = shard.AddComponent<SpriteRenderer>();
        }
        else
        {
            projectile.transform.SetParent(roomRoot, true);
            projectile.gameObject.name = projectileName;
            projectile.gameObject.SetActive(true);
        }

        if (projectile.transform.parent != roomRoot)
            projectile.transform.SetParent(roomRoot, true);
        return projectile;
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
        if (Time.time >= expiresAt) ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (!gameObject.activeSelf) return;
        BodyVelocity = Vector2.zero;
        if (Pool.Count < ProjectilePoolCapacity)
        {
            gameObject.SetActive(false);
            Pool.Add(this);
        }
        else Destroy(gameObject);
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
        ReturnToPool();
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
