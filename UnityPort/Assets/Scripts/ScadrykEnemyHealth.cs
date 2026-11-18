using UnityEngine;

/// <summary>Biome-themed enemy attacks with readable wind-ups and distinct movement.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class ScadrykEnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 60f;
    private float health, nextAttackAt, windupEndsAt;
    private float attackRange = 1.2f;
    private Rigidbody2D body;
#if UNITY_6000_0_OR_NEWER
    private Vector2 BodyVelocity { get => body.linearVelocity; set => body.linearVelocity = value; }
#else
    private Vector2 BodyVelocity { get => body.velocity; set => body.velocity = value; }
#endif
    private SpriteRenderer sprite;
    private ScadrykPlayerController player;
    private ScadrykGameDirector director;
    private ScadrykActorAnimator animator;
    private bool dead;
    private bool isWindup;
    private Sprite[] animationFrames;
    private Vector2 attackTarget;
    private int biomeIndex;
    private float touchDamage;
    private float moveSpeed;
    private bool isFinalBoss;
    private int finalAttackIndex;
    private int combatRole;
    private Color roleTint = Color.white;

    public float Health01 => maxHealth <= 0 ? 0 : Mathf.Clamp01(health / maxHealth);
    public bool IsBoss { get; private set; }
    public bool IsFinalBoss => isFinalBoss;
    public int BiomeIndex => biomeIndex;

    public void Configure(float hp, float damage, float speed, bool boss, int biome, Sprite[] frames, int role = 0)
    {
        maxHealth = hp; health = hp; touchDamage = damage; moveSpeed = speed; IsBoss = boss;
        biomeIndex = Mathf.Clamp(biome, 0, 2);
        combatRole = boss ? 0 : Mathf.Clamp(role, 0, 3);
        roleTint = boss ? Color.white : combatRole == 0 ? new Color(1f, 0.87f, 0.75f) : combatRole == 1 ? new Color(0.72f, 0.9f, 1f) : combatRole == 2 ? new Color(1f, 0.76f, 0.85f) : new Color(0.77f, 1f, 0.82f);
        attackRange = boss ? (biomeIndex == 1 ? 8f : biomeIndex == 0 ? 6.8f : 5.8f) :
            combatRole == 0 ? 3.1f : combatRole == 1 ? 7.2f : combatRole == 2 ? 3.9f : 5.5f;
        animationFrames = frames;
        if (sprite != null && animationFrames != null && animationFrames.Length > 0) sprite.sprite = animationFrames[0];
        var collider = GetComponent<CircleCollider2D>();
        collider.radius = boss ? 0.58f : 0.34f;
    }

    public void ConfigureFinalBoss(Sprite[] frames)
    {
        isFinalBoss = true;
        IsBoss = true;
        maxHealth = 1250f;
        health = maxHealth;
        touchDamage = 32f;
        moveSpeed = 1.35f;
        attackRange = 10f;
        biomeIndex = 2;
        animationFrames = frames;
        if (sprite != null && frames != null && frames.Length > 0) sprite.sprite = frames[0];
        GetComponent<CircleCollider2D>().radius = 0.86f;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f; body.freezeRotation = true; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        Transform visual = transform.Find("Animated visual");
        sprite = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        animator = GetComponent<ScadrykActorAnimator>();
        player = FindAnyObjectByType<ScadrykPlayerController>();
        director = FindAnyObjectByType<ScadrykGameDirector>();
        if (health <= 0f) health = maxHealth;
        GetComponent<CircleCollider2D>().isTrigger = false;
    }

    private void Update()
    {
        if (dead || player == null || (director != null && director.IsPaused))
        {
            if (body != null) BodyVelocity = Vector2.zero;
            return;
        }

        Vector2 delta = (Vector2)player.transform.position - body.position;
        float distance = delta.magnitude;
        if (sprite != null)
        {
            bool windingUp = isWindup && windupEndsAt > Time.time;
            sprite.color = windingUp ? new Color(1f, 0.68f, 0.24f) : roleTint;
            if (!windingUp && animationFrames != null && animationFrames.Length >= 4)
                sprite.sprite = animationFrames[BodyVelocity.sqrMagnitude > 0.05f ? Mathf.FloorToInt(Time.time * 7f) % animationFrames.Length : 0];
        }

        if (isWindup)
        {
            BodyVelocity = Vector2.zero;
            if (Time.time >= windupEndsAt) ResolveAttack();
            return;
        }

        if (windupEndsAt > 0f && Time.time >= windupEndsAt) ResolveAttack();
        if (Time.time >= nextAttackAt && distance <= attackRange)
        {
            BeginAttack();
            return;
        }

        if (!IsBoss && (combatRole == 1 || combatRole == 3))
        {
            if (distance > 5.5f) BodyVelocity = delta.normalized * moveSpeed;
            else if (distance < 3.2f) BodyVelocity = -delta.normalized * moveSpeed * 0.8f;
            else BodyVelocity = Vector2.zero;
        }
        else if (distance > Mathf.Min(attackRange * 0.72f, 3.1f))
            BodyVelocity = delta.normalized * moveSpeed;
        else
            BodyVelocity = Vector2.zero;
    }

    private void BeginAttack()
    {
        attackTarget = player.transform.position;
        ScadrykAudio.Play(ScadrykCue.Warning);
        if (animator != null) animator.PlayAttack((attackTarget - (Vector2)transform.position).normalized, IsBoss);
        if (isFinalBoss)
        {
            BeginFinalBossAttack();
            return;
        }
        float windup = IsBoss ? (biomeIndex == 0 ? 0.95f : biomeIndex == 1 ? 0.86f : 1.05f) :
            combatRole == 0 ? 0.7f : combatRole == 1 ? 0.85f : combatRole == 2 ? 0.95f : 1.1f;
        isWindup = true;
        windupEndsAt = Time.time + windup;
        Vector2 origin = transform.position;

        Color warning = biomeIndex == 0 ? new Color(0.64f, 0.86f, 0.35f, 0.85f) :
            biomeIndex == 1 ? new Color(1f, 0.78f, 0.34f, 0.85f) : new Color(0.34f, 0.9f, 1f, 0.85f);
        if (!IsBoss && combatRole == 3)
            ScadrykEffects.Ring(origin, 5f, new Color(0.38f, 1f, 0.6f, 0.8f), windup);
        else if (!IsBoss && combatRole == 2)
            ScadrykEffects.Ring(origin, 6.4f, warning, windup);
        else if (IsBoss && biomeIndex == 2)
            ScadrykEffects.Ring(origin, 10.6f, warning, windup);
        else
        {
            Vector2 direction = (attackTarget - origin).normalized;
            Vector2 end = !IsBoss && combatRole == 0 ? origin + direction * 3.3f :
                IsBoss && biomeIndex == 0 ? origin + direction * Mathf.Clamp(Vector2.Distance(origin, attackTarget) + 1.25f, 2.8f, 7.2f) : attackTarget;
            ScadrykEffects.TelegraphLine(origin, end, warning, !IsBoss && combatRole == 1 ? 0.16f : 0.34f, windup);
        }
    }

    private void BeginFinalBossAttack()
    {
        isWindup = true;
        windupEndsAt = Time.time + 1.12f;
        Vector2 origin = transform.position;
        if (finalAttackIndex % 3 == 0)
        {
            Vector2 direction = (attackTarget - origin).normalized;
            for (int i = -2; i <= 2; i++)
                ScadrykEffects.TelegraphLine(origin, origin + Rotate(direction, i * 0.22f) * 7.2f, new Color(0.98f, 0.26f, 0.62f, 0.75f), 0.16f, 1.12f);
        }
        else if (finalAttackIndex % 3 == 1)
        {
            ScadrykEffects.Voidburst(attackTarget, 3.2f, 1.12f);
            ScadrykEffects.Ring(attackTarget, 5.4f, new Color(0.98f, 0.26f, 0.62f, 0.9f), 1.12f);
        }
        else
        {
            ScadrykEffects.Voidburst(origin, 5f, 1.12f);
            ScadrykEffects.Ring(origin, 13f, new Color(0.98f, 0.26f, 0.62f, 0.95f), 1.12f);
        }
    }

    private void ResolveAttack()
    {
        isWindup = false;
        ScadrykAudio.Play(ScadrykCue.Impact);
        windupEndsAt = 0f;
        nextAttackAt = Time.time + (IsBoss ? 2.3f : 2.6f);
        if (animator != null && player != null) animator.PlayAttack(((Vector2)player.transform.position - (Vector2)transform.position).normalized, IsBoss);
        if (player == null) return;

        if (isFinalBoss)
        {
            ResolveFinalBossAttack();
            return;
        }

        if (!IsBoss)
        {
            if (combatRole == 0) ResolveRootThrust();
            else if (combatRole == 1) ResolveCrystalVolley();
            else if (combatRole == 2) ResolveAreaPulse();
            else ResolveSupportPulse();
        }
        else if (biomeIndex == 0) ResolveRootThrust();
        else if (biomeIndex == 1) ResolveCrystalVolley();
        else ResolveBellPulse();
    }

    private void ResolveFinalBossAttack()
    {
        int pattern = finalAttackIndex++ % 3;
        nextAttackAt = Time.time + 2.15f;
        Vector2 origin = transform.position;
        if (pattern == 0)
        {
            Vector2 direction = (attackTarget - origin).normalized;
            for (int i = -3; i <= 3; i++)
                ScadrykEnemyProjectile.Launch(origin + direction * 0.75f, Rotate(direction, i * 0.19f), 7.1f, 15f, ScadrykEffects.VoidburstSprite, new Color(1f, 0.72f, 0.92f), 0.58f, "Void star");
            ScadrykEffects.Voidburst(origin + direction * 1.1f, 2.3f, 0.26f);
        }
        else if (pattern == 1)
        {
            Vector2 target = attackTarget;
            StartCoroutine(ResolveStarfall(target));
            Vector2 direction = (target - origin).normalized;
            for (int i = -1; i <= 1; i++)
                ScadrykEnemyProjectile.Launch(origin, Rotate(direction, i * 0.48f), 5.4f, 14f, ScadrykEffects.VoidburstSprite, new Color(1f, 0.58f, 0.8f), 0.48f, "Falling void star");
        }
        else
        {
            float distance = Vector2.Distance(player.transform.position, origin);
            ScadrykEffects.Voidburst(origin, 13f, 0.42f);
            ScadrykEffects.Ring(origin, 13f, new Color(0.98f, 0.24f, 0.57f, 0.96f), 0.42f);
            if (distance > 2.2f && distance < 6.7f) player.TakeDamage(27f);
        }
    }

    private System.Collections.IEnumerator ResolveStarfall(Vector2 target)
    {
        yield return new WaitForSeconds(0.46f);
        if (dead) yield break;
        ScadrykEffects.Voidburst(target, 3.4f, 0.38f);
        if (player != null && Vector2.Distance(player.transform.position, target) < 1.75f)
            player.TakeDamage(23f);
    }

    private void ResolveRootThrust()
    {
        Vector2 start = transform.position;
        Vector2 direction = (attackTarget - start).normalized;
        float length = IsBoss ? Mathf.Clamp(Vector2.Distance(start, attackTarget) + 1.25f, 2.8f, 7.2f) : 3.3f;
        float width = IsBoss ? 1.4f : 0.86f;
        Vector2 end = start + direction * length;
        ScadrykEffects.GroundLane((start + end) * 0.5f, direction, length, width, new Color(0.42f, 0.72f, 0.28f, 0.95f), 0.3f);
        if (DistanceToSegment(player.transform.position, start, end) <= width * 0.5f + 0.38f)
        {
            player.TakeDamage(IsBoss ? 21f : 13f);
            if (biomeIndex == 0) player.ApplyRootSlow(IsBoss ? 1.55f : 0.65f);
        }
    }

    private void ResolveCrystalVolley()
    {
        Vector2 direction = ((Vector2)attackTarget - (Vector2)transform.position).normalized;
        int shardCount = IsBoss ? 3 : 1;
        float spread = IsBoss ? 0.32f : 0f;
        for (int i = 0; i < shardCount; i++)
        {
            float offset = shardCount == 1 ? 0f : (i - 1) * spread;
            Vector2 shardDirection = Rotate(direction, offset);
            ScadrykEnemyProjectile.Launch(transform.position + (Vector3)(shardDirection * 0.55f), shardDirection, IsBoss ? 7.5f : 6.7f, IsBoss ? 15f : 11f,
                biomeIndex == 0 ? ScadrykEffects.RootThrustSprite : biomeIndex == 2 ? ScadrykEffects.BellPulseSprite : ScadrykEffects.SaltShardSprite);
        }
    }

    private void ResolveAreaPulse()
    {
        const float radius = 3.2f;
        Color color = biomeIndex == 0 ? new Color(0.55f, 0.85f, 0.38f) : biomeIndex == 1 ? new Color(1f, 0.72f, 0.35f) : new Color(0.35f, 0.88f, 1f);
        ScadrykEffects.Ring(transform.position, radius * 2f, color, 0.32f);
        if (Vector2.Distance(player.transform.position, transform.position) <= radius) player.TakeDamage(12f);
    }

    private void ResolveSupportPulse()
    {
        ScadrykEffects.Ring(transform.position, 5f, new Color(0.38f, 1f, 0.6f), 0.35f);
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, 2.5f);
        foreach (Collider2D hit in nearby)
            if (hit.TryGetComponent<ScadrykEnemyHealth>(out var ally) && ally != this && !ally.dead) ally.Heal(15f);
    }

    private void Heal(float amount) { health = Mathf.Min(maxHealth, health + amount); }

    private void ResolveBellPulse()
    {
        float radius = IsBoss ? 5.3f : 3.65f;
        ScadrykEffects.Ring(transform.position, radius * 2f, new Color(0.34f, 0.9f, 1f, 0.96f), 0.32f);
        ScadrykEffects.Burst(transform.position, new Color(0.24f, 0.72f, 0.94f, 0.24f), radius * 1.5f);
        if (Vector2.Distance(player.transform.position, transform.position) <= radius)
            player.TakeDamage(IsBoss ? 21f : 12f);
    }

    private static Vector2 Rotate(Vector2 vector, float radians)
    {
        float sin = Mathf.Sin(radians), cos = Mathf.Cos(radians);
        return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float t = segment.sqrMagnitude < 0.001f ? 0f : Mathf.Clamp01(Vector2.Dot(point - start, segment) / segment.sqrMagnitude);
        return Vector2.Distance(point, start + segment * t);
    }

    public void TakeDamage(float amount)
    {
        if (dead) return;
        health -= Mathf.Max(0f, amount);
        if (animator != null) animator.PlayHit(player != null ? ((Vector2)transform.position - (Vector2)player.transform.position).normalized : Vector2.up);
        if (health <= 0f)
        {
            dead = true;
            BodyVelocity = Vector2.zero;
            GetComponent<Collider2D>().enabled = false;
            ScadrykHealthBar bar = GetComponent<ScadrykHealthBar>();
            if (bar != null) bar.Hide();
            if (animator != null) animator.PlayDeath();
            ScadrykEffects.DeathBurst(transform.position, IsBoss ? new Color(1f, 0.54f, 0.22f, 0.9f) : new Color(0.55f, 0.84f, 0.8f, 0.8f), IsBoss);
            if (director != null) director.OnEnemyDefeated(this);
            Destroy(gameObject, 0.74f);
        }
    }
}
