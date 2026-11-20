using UnityEngine;

/// <summary>Biome-themed enemy attacks with readable wind-ups and distinct movement.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class ScadrykEnemyHealth : MonoBehaviour
{
    private const float FinalWaveOuterRadius = 6.7f;
    private const float FinalWaveSafeRadius = 2.2f;
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
    private int queuedFinalPattern;
    private int queuedFinalStage;
    private int combatRole;
    private Color roleTint = Color.white;
    private float staggerUntil, exposedUntil, knockbackUntil;
    private Vector2 knockbackVelocity;

    public float Health => health;
    public float Health01 => maxHealth <= 0 ? 0 : Mathf.Clamp01(health / maxHealth);
    public bool IsBoss { get; private set; }
    public bool IsFinalBoss => isFinalBoss;
    public bool IsStaggered => !dead && Time.time < staggerUntil;
    public bool IsExposed => !dead && Time.time < exposedUntil;
    public bool IsKnockedBack => !dead && Time.time < knockbackUntil;
    public int FinalStage => !isFinalBoss ? 0 : Health01 > 0.7f ? 1 : Health01 > 0.35f ? 2 : 3;
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
            sprite.color = windingUp ? new Color(1f, 0.68f, 0.24f) : Time.time < exposedUntil ? new Color(1f, 0.8f, 0.42f) : roleTint;
            if (!windingUp && animationFrames != null && animationFrames.Length >= 4)
                sprite.sprite = animationFrames[BodyVelocity.sqrMagnitude > 0.05f ? Mathf.FloorToInt(Time.time * 7f) % animationFrames.Length : 0];
        }

        if (Time.time < knockbackUntil) { BodyVelocity = knockbackVelocity; return; }
        if (Time.time < staggerUntil) { BodyVelocity = Vector2.zero; return; }

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
            float telegraphThickness = !IsBoss && combatRole == 0 ? 0.86f :
                !IsBoss && combatRole == 1 ? 0.31f : IsBoss && biomeIndex == 0 ? 1.4f : 0.34f;
            ScadrykEffects.TelegraphLine(origin, end, warning, telegraphThickness, windup);
        }
    }

    private void BeginFinalBossAttack()
    {
        queuedFinalStage = FinalStage;
        int stage = queuedFinalStage;
        queuedFinalPattern = finalAttackIndex % (stage == 1 ? 2 : 3);
        float windup = stage == 3 ? 1.18f : 1.35f;
        isWindup = true;
        windupEndsAt = Time.time + windup;
        Vector2 origin = transform.position;
        Color warning = stage == 1 ? new Color(0.62f, 0.9f, 0.42f, 0.82f) : stage == 2 ? new Color(1f, 0.72f, 0.32f, 0.82f) : new Color(0.98f, 0.26f, 0.62f, 0.85f);
        if (queuedFinalPattern == 0)
        {
            Vector2 direction = (attackTarget - origin).normalized;
            int halfCount = stage == 1 ? 1 : stage == 2 ? 2 : 3;
            for (int i = -halfCount; i <= halfCount; i++)
                ScadrykEffects.TelegraphLine(origin, origin + Rotate(direction, i * 0.19f) * 7.2f, warning, 0.52f, windup);
        }
        else if (queuedFinalPattern == 1)
        {
            ScadrykEffects.Voidburst(attackTarget, 3.2f, windup);
            ScadrykEffects.Ring(attackTarget, 3.5f, warning, windup + 0.46f);
        }
        else
        {
            ScadrykEffects.Ring(origin, FinalWaveOuterRadius * 2f, warning, windup);
            ScadrykEffects.Ring(origin, FinalWaveSafeRadius * 2f, new Color(0.55f, 1f, 0.82f, 0.92f), windup);
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
        int pattern = queuedFinalPattern;
        int stage = queuedFinalStage;
        finalAttackIndex++;
        nextAttackAt = Time.time + (stage == 3 ? 1.9f : 2.25f);
        Vector2 origin = transform.position;
        if (pattern == 0)
        {
            Vector2 direction = (attackTarget - origin).normalized;
            int halfCount = stage == 1 ? 1 : stage == 2 ? 2 : 3;
            Sprite shot = stage == 1 ? ScadrykEffects.RootThrustSprite : stage == 2 ? ScadrykEffects.SaltShardSprite : ScadrykEffects.VoidburstSprite;
            Color color = stage == 1 ? new Color(0.66f, 1f, 0.52f) : stage == 2 ? new Color(1f, 0.84f, 0.5f) : new Color(1f, 0.72f, 0.92f);
            for (int i = -halfCount; i <= halfCount; i++)
                ScadrykEnemyProjectile.Launch(origin + direction * 0.75f, Rotate(direction, i * 0.19f), 7.1f, 14f + stage, shot, color, 0.58f, "Arauto — remembered volley");
            ScadrykEffects.Voidburst(origin + direction * 1.1f, 2.3f, 0.26f);
        }
        else if (pattern == 1)
        {
            Vector2 target = attackTarget;
            StartCoroutine(ResolveStarfall(target));
            Vector2 direction = (target - origin).normalized;
            if (stage > 1)
                for (int i = stage == 2 ? 0 : -1; i <= (stage == 2 ? 0 : 1); i++)
                    ScadrykEnemyProjectile.Launch(origin, Rotate(direction, i * 0.48f), 5.4f, 14f, stage == 2 ? ScadrykEffects.SaltShardSprite : ScadrykEffects.VoidburstSprite, new Color(1f, 0.7f, 0.8f), 0.48f, "Arauto — falling memory");
        }
        else
        {
            float distance = Vector2.Distance(player.transform.position, origin);
            float diameter = FinalWaveOuterRadius * 2f;
            ScadrykEffects.Voidburst(origin, diameter, 0.42f);
            ScadrykEffects.Ring(origin, diameter, new Color(0.98f, 0.24f, 0.57f, 0.96f), 0.42f);
            if (distance > FinalWaveSafeRadius && distance < FinalWaveOuterRadius) player.TakeDamage(stage == 3 ? 27f : 23f);
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
        if (DistanceToSegment(player.transform.position, start, end) <= width * 0.5f + 0.38f &&
            !ScadrykCombatRules.WorldBlocks(start, player.transform.position))
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

    public void ApplyCrescentControl()
    {
        if (dead) return;
        staggerUntil = Mathf.Max(staggerUntil, Time.time + (IsBoss ? 0.26f : 0.82f));
        if (isWindup)
        {
            isWindup = false;
            windupEndsAt = 0f;
            nextAttackAt = staggerUntil + 0.8f;
        }
    }

    public void ApplySaltExposure()
    {
        if (!dead) exposedUntil = Mathf.Max(exposedUntil, Time.time + (IsBoss ? 3f : 5f));
    }

    public void ApplyTidePush(Vector2 direction)
    {
        if (dead || IsBoss) return;
        knockbackVelocity = direction.sqrMagnitude > 0.01f ? direction.normalized * 5.5f : Vector2.zero;
        knockbackUntil = Time.time + 0.24f;
        staggerUntil = Mathf.Max(staggerUntil, knockbackUntil + 0.25f);
    }

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
        int previousStage = FinalStage;
        health -= Mathf.Max(0f, amount) * (Time.time < exposedUntil ? 1.3f : 1f);
        if (isFinalBoss && health > 0f && FinalStage != previousStage && director != null) director.ShowBossStage(FinalStage);
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
