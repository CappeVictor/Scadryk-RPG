using UnityEngine;

/// <summary>Biome-themed enemies with readable attacks; player damage comes from announced attacks, never contact alone.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class ScadrykEnemyHealth : MonoBehaviour
{
    private const float FinalWaveOuterRadius = 6.7f;
    private const float FinalWaveSafeRadius = 2.2f;
    internal const float FinalStarfallRadius = 1.75f;
    private const float FinalVolleyAngleStep = 0.19f;
    private const float FinalVolleyLength = 7.2f;
    private const float FinalVolleySpeed = 7.1f;
    private const float FinalVolleySpawnOffset = 0.75f;
    private const float FinalStarfallProjectileAngleStep = 0.48f;
    private const float FinalStarfallProjectileScale = 0.48f;
    private const float FinalStarfallProjectileLength = 10f;
    private const float FinalStarfallProjectileSpeed = 5.4f;
    internal const float SupportPulseRadius = 2.5f;
    internal const float OpeningAttackSpacing = 0.52f;
    internal static float AttackRecoveryDuration(bool boss, bool finalBoss) => finalBoss ? 0.42f : boss ? 0.36f : 0.24f;
    private static readonly float[] SteeringAngles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f, 145f, -145f, 180f };
    private readonly RaycastHit2D[] steeringHits = new RaycastHit2D[16];
    private readonly bool[] blockedSteeringHeadings = new bool[10];
    internal static float SupportPulseDiameter => SupportPulseRadius * 2f;
    internal static bool SupportPulseContains(Vector2 origin, Vector2 target) =>
        Vector2.Distance(origin, target) <= SupportPulseRadius;
    [SerializeField, Min(1f)] private float maxHealth = 60f;
    private float health, nextAttackAt, windupEndsAt;
    private float attackRange = 1.2f;
    private Rigidbody2D body;
    private CircleCollider2D bodyCollider;
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
    private bool openingAttackPending;
    private bool openingAttackSlotReserved;
    private Sprite[] animationFrames;
    private Vector2 attackTarget;
    private int biomeIndex;
    private float moveSpeed;
    private bool isFinalBoss;
    private int finalAttackIndex;
    private int queuedFinalPattern;
    private int queuedFinalStage;
    private int combatRole;
    private Color roleTint = Color.white;
    private float staggerUntil, exposedUntil, knockbackUntil, recoveryUntil;
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

    internal static ScadrykCue WarningCueFor(int biome, bool boss, bool finalBoss, int role)
    {
        if (finalBoss) return ScadrykCue.FinalBossWarning;
        int attackRole = boss ? biome == 0 ? 0 : biome == 1 ? 1 : 2 : Mathf.Clamp(role, 0, 3);
        return attackRole switch
        {
            0 => ScadrykCue.LaneWarning,
            1 => ScadrykCue.VolleyWarning,
            2 => ScadrykCue.AreaWarning,
            _ => ScadrykCue.SupportWarning
        };
    }

    internal static Color StateTint(bool windingUp, bool exposed, Color roleTint) =>
        windingUp ? new Color(1f, 0.68f, 0.24f) : exposed ? new Color(0.58f, 0.9f, 1f) : roleTint;

    public void Configure(float hp, float speed, bool boss, int biome, Sprite[] frames, int role = 0, float openingAttackDelay = 0f)
    {
        maxHealth = hp; health = hp; moveSpeed = speed; IsBoss = boss;
        biomeIndex = Mathf.Clamp(biome, 0, 2);
        combatRole = boss ? 0 : Mathf.Clamp(role, 0, 3);
        nextAttackAt = Time.time + Mathf.Max(0f, openingAttackDelay);
        openingAttackPending = true;
        openingAttackSlotReserved = false;
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
        nextAttackAt = Time.time + OpeningAttackDelay(0, true);
        openingAttackPending = true;
        openingAttackSlotReserved = false;
        maxHealth = 1250f;
        health = maxHealth;
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
        bodyCollider = GetComponent<CircleCollider2D>();
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
        bool requiresClearShot = !isFinalBoss && (IsBoss ? biomeIndex == 1 : combatRole == 1);
        bool shotBlocked = requiresClearShot && ScadrykCombatRules.WorldBlocks(body.position, player.transform.position);
        if (sprite != null)
        {
            bool windingUp = isWindup && windupEndsAt > Time.time;
            sprite.color = StateTint(windingUp, Time.time < exposedUntil, roleTint);
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
        if (Time.time < recoveryUntil) { BodyVelocity = Vector2.zero; return; }
        if (Time.time >= nextAttackAt && distance <= attackRange && !shotBlocked)
        {
            if (openingAttackPending)
            {
                if (!openingAttackSlotReserved)
                {
                    nextAttackAt = director != null ? director.ScheduleOpeningAttack(Time.time) : Time.time;
                    openingAttackSlotReserved = true;
                }
                if (Time.time < nextAttackAt) { BodyVelocity = Vector2.zero; return; }
                openingAttackPending = false;
            }
            BeginAttack();
            return;
        }

        // Crystal throwers and the salt guardian move until they can see Nara;
        // all other pursuers steer around solid scenery instead of pushing into it.
        if (shotBlocked)
        {
            BodyVelocity = MoveWithObstacleAvoidance(delta.normalized, distance) * moveSpeed;
            return;
        }

        if (!IsBoss && (combatRole == 1 || combatRole == 3))
        {
            if (distance > 5.5f) BodyVelocity = MoveWithObstacleAvoidance(delta.normalized, distance) * moveSpeed;
            else if (distance < 3.2f) BodyVelocity = MoveWithObstacleAvoidance(-delta.normalized, distance) * moveSpeed * 0.8f;
            else BodyVelocity = Vector2.zero;
        }
        else if (distance > Mathf.Min(attackRange * 0.72f, 3.1f))
            BodyVelocity = MoveWithObstacleAvoidance(delta.normalized, distance) * moveSpeed;
        else
            BodyVelocity = Vector2.zero;
    }

    private Vector2 MoveWithObstacleAvoidance(Vector2 desiredHeading, float distanceToTarget)
    {
        if (desiredHeading.sqrMagnitude < 0.001f) return Vector2.zero;
        desiredHeading.Normalize();
        float castDistance = Mathf.Min(Mathf.Max(0f, distanceToTarget), 1.7f);
        if (castDistance < 0.05f) return desiredHeading;

        float radius = bodyCollider != null ? bodyCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y)) : 0.34f;
        blockedSteeringHeadings[0] = SolidAhead(desiredHeading, radius, castDistance);
        if (!blockedSteeringHeadings[0]) return desiredHeading;
        for (int i = 1; i < SteeringAngles.Length; i++)
        {
            Vector2 heading = Rotate(desiredHeading, SteeringAngles[i] * Mathf.Deg2Rad);
            blockedSteeringHeadings[i] = SolidAhead(heading, radius, castDistance);
        }
        return SelectSteeringHeading(desiredHeading, blockedSteeringHeadings);
    }

    private bool SolidAhead(Vector2 heading, float radius, float distance)
    {
        int hitCount = Physics2D.CircleCastNonAlloc(body.position, radius * 0.92f, heading, steeringHits, distance);
        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            Collider2D hit = steeringHits[hitIndex].collider;
            if (hit == null || !hit.enabled || hit.isTrigger || hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (hit.GetComponent<ScadrykEnemyHealth>() != null || hit.GetComponent<ScadrykPlayerController>() != null || hit.GetComponent<ScadrykEnemyProjectile>() != null) continue;
            return true;
        }
        return false;
    }

    internal static Vector2 SelectSteeringHeading(Vector2 desiredHeading, bool[] blocked)
    {
        if (desiredHeading.sqrMagnitude < 0.001f) return Vector2.zero;
        desiredHeading.Normalize();
        if (blocked == null || blocked.Length < SteeringAngles.Length || !blocked[0]) return desiredHeading;

        float bestProgress = float.NegativeInfinity;
        Vector2 bestHeading = desiredHeading;
        for (int i = 1; i < SteeringAngles.Length; i++)
        {
            if (blocked[i]) continue;
            Vector2 candidate = Rotate(desiredHeading, SteeringAngles[i] * Mathf.Deg2Rad);
            float progress = Vector2.Dot(candidate, desiredHeading);
            if (progress <= bestProgress) continue;
            bestProgress = progress;
            bestHeading = candidate;
        }
        return bestHeading;
    }

    private void BeginAttack()
    {
        attackTarget = player.transform.position;
        ScadrykAudio.Play(WarningCueFor(biomeIndex, IsBoss, isFinalBoss, combatRole));
        if (isFinalBoss)
        {
            BeginFinalBossAttack();
            return;
        }
        float windup = IsBoss ? (biomeIndex == 0 ? 0.95f : biomeIndex == 1 ? 0.86f : 1.05f) :
            combatRole == 0 ? 0.7f : combatRole == 1 ? 0.85f : combatRole == 2 ? 0.95f : 1.1f;
        if (animator != null) animator.PlayWindup((attackTarget - (Vector2)transform.position).normalized, windup);
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
        else if (IsBoss && biomeIndex == 1)
        {
            Vector2 direction = (attackTarget - origin).normalized;
            float length = CrystalVolleyLength(origin, attackTarget);
            for (int i = -1; i <= 1; i++)
            {
                Vector2 shardDirection = Rotate(direction, i * 0.32f);
                ScadrykEffects.TelegraphLine(origin, origin + shardDirection * length, warning,
                    ScadrykEnemyProjectile.HitboxDiameter(0.34f), windup);
            }
        }
        else
        {
            Vector2 direction = (attackTarget - origin).normalized;
            Vector2 end = !IsBoss && combatRole == 0 ? origin + direction * 3.3f :
                IsBoss && biomeIndex == 0 ? origin + direction * Mathf.Clamp(Vector2.Distance(origin, attackTarget) + 1.25f, 2.8f, 7.2f) : attackTarget;
            float telegraphThickness = !IsBoss && combatRole == 0 ? 0.86f :
            !IsBoss && combatRole == 1 ? ScadrykEnemyProjectile.HitboxDiameter(0.34f) : IsBoss && biomeIndex == 0 ? 1.4f : ScadrykEnemyProjectile.HitboxDiameter(0.34f);
            if ((!IsBoss && combatRole == 0) || (IsBoss && biomeIndex == 0))
                telegraphThickness = RootLaneVisualWidth(telegraphThickness, PlayerCollisionRadius(player));
            ScadrykEffects.TelegraphLine(origin, end, warning, telegraphThickness, windup);
        }
    }

    private void BeginFinalBossAttack()
    {
        queuedFinalStage = FinalStage;
        int stage = queuedFinalStage;
        queuedFinalPattern = finalAttackIndex % (stage == 1 ? 2 : 3);
        float windup = stage == 3 ? 1.18f : 1.35f;
        if (animator != null) animator.PlayWindup((attackTarget - (Vector2)transform.position).normalized, windup);
        isWindup = true;
        windupEndsAt = Time.time + windup;
        Vector2 origin = transform.position;
        Color warning = stage == 1 ? new Color(0.62f, 0.9f, 0.42f, 0.82f) : stage == 2 ? new Color(1f, 0.72f, 0.32f, 0.82f) : new Color(0.98f, 0.26f, 0.62f, 0.85f);
        if (queuedFinalPattern == 0)
        {
            Vector2 direction = (attackTarget - origin).normalized;
            int halfCount = stage == 1 ? 1 : stage == 2 ? 2 : 3;
            for (int i = -halfCount; i <= halfCount; i++)
                ScadrykEffects.TelegraphLine(origin, origin + Rotate(direction, i * FinalVolleyAngleStep) * FinalVolleyLength, warning,
                    ScadrykEnemyProjectile.HitboxDiameter(0.58f), windup);
        }
        else if (queuedFinalPattern == 1)
        {
            float diameter = FinalStarfallVisualDiameter;
            ScadrykEffects.Voidburst(attackTarget, diameter, windup);
            ScadrykEffects.Ring(attackTarget, diameter, warning, windup + 0.46f);
            if (stage > 1)
            {
                Vector2 direction = (attackTarget - origin).normalized;
                int count = FinalStarfallSideProjectileCount(stage);
                for (int i = 0; i < count; i++)
                {
                    Vector2 shardDirection = Rotate(direction, FinalStarfallSideProjectileAngle(stage, i));
                    ScadrykEffects.TelegraphLine(origin, origin + shardDirection * FinalStarfallProjectileLength, warning,
                        ScadrykEnemyProjectile.HitboxDiameter(FinalStarfallProjectileScale), windup);
                }
            }
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
        recoveryUntil = Time.time + AttackRecoveryDuration(IsBoss, isFinalBoss);
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
                ScadrykEnemyProjectile.Launch(origin + direction * FinalVolleySpawnOffset, Rotate(direction, i * FinalVolleyAngleStep), FinalVolleySpeed, 14f + stage, shot, color, 0.58f, "Arauto — remembered volley",
                    Mathf.Max(0f, FinalVolleyLength - FinalVolleySpawnOffset - ScadrykEnemyProjectile.HitboxRadius(0.58f)));
            ScadrykEffects.Voidburst(origin + direction * 1.1f, 2.3f, 0.26f);
        }
        else if (pattern == 1)
        {
            Vector2 target = attackTarget;
            StartCoroutine(ResolveStarfall(target));
            Vector2 direction = (target - origin).normalized;
            if (stage > 1)
                for (int i = 0; i < FinalStarfallSideProjectileCount(stage); i++)
                    ScadrykEnemyProjectile.Launch(origin, Rotate(direction, FinalStarfallSideProjectileAngle(stage, i)), FinalStarfallProjectileSpeed, 14f,
                        stage == 2 ? ScadrykEffects.SaltShardSprite : ScadrykEffects.VoidburstSprite, new Color(1f, 0.7f, 0.8f), FinalStarfallProjectileScale, "Arauto — falling memory",
                        FinalStarfallProjectileLength - ScadrykEnemyProjectile.HitboxRadius(FinalStarfallProjectileScale));
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
        ScadrykEffects.Voidburst(target, FinalStarfallVisualDiameter, 0.38f);
        if (player != null && FinalStarfallHits(player.transform.position, target))
            player.TakeDamage(23f);
    }

    internal static float FinalStarfallVisualDiameter => FinalStarfallRadius * 2f;
    internal static bool FinalStarfallHits(Vector2 target, Vector2 center) => Vector2.Distance(target, center) <= FinalStarfallRadius;
    internal static int FinalStarfallSideProjectileCount(int stage) => stage == 2 ? 1 : stage == 3 ? 3 : 0;
    internal static float FinalStarfallSideProjectileAngle(int stage, int index) =>
        (index - (FinalStarfallSideProjectileCount(stage) - 1) * 0.5f) * FinalStarfallProjectileAngleStep;
    internal static float OpeningAttackDelay(int enemyIndex, bool boss) => boss ? 1.25f : 0.3f + Mathf.Max(0, enemyIndex) * OpeningAttackSpacing;
    internal static float ReserveOpeningAttackSlot(ref float nextAvailableAt, float requestedAt)
    {
        float scheduledAt = Mathf.Max(nextAvailableAt, requestedAt);
        nextAvailableAt = scheduledAt + OpeningAttackSpacing;
        return scheduledAt;
    }

    private void ResolveRootThrust()
    {
        Vector2 start = transform.position;
        Vector2 direction = (attackTarget - start).normalized;
        float length = IsBoss ? Mathf.Clamp(Vector2.Distance(start, attackTarget) + 1.25f, 2.8f, 7.2f) : 3.3f;
        float width = IsBoss ? 1.4f : 0.86f;
        Vector2 end = start + direction * length;
        float warnedWidth = RootLaneVisualWidth(width, PlayerCollisionRadius(player));
        ScadrykEffects.GroundLane((start + end) * 0.5f, direction, length, warnedWidth, new Color(0.42f, 0.72f, 0.28f, 0.95f), 0.3f);
        if (RootLaneHits(player.transform.position, start, end, width, PlayerCollisionRadius(player)) &&
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
        float speed = IsBoss ? 7.5f : 6.7f;
        float travelDistance = IsBoss ? CrystalVolleyLength(transform.position, attackTarget) : Vector2.Distance(transform.position, attackTarget);
        for (int i = 0; i < shardCount; i++)
        {
            float offset = shardCount == 1 ? 0f : (i - 1) * spread;
            Vector2 shardDirection = Rotate(direction, offset);
            ScadrykEnemyProjectile.Launch(transform.position + (Vector3)(shardDirection * 0.55f), shardDirection, speed, IsBoss ? 15f : 11f,
                biomeIndex == 0 ? ScadrykEffects.RootThrustSprite : biomeIndex == 2 ? ScadrykEffects.BellPulseSprite : ScadrykEffects.SaltShardSprite,
                size: 0.34f, projectileName: "Salt crystal shard",
                travelDistance: Mathf.Max(0f, travelDistance - 0.55f - ScadrykEnemyProjectile.HitboxRadius(0.34f)));
        }
    }

    private static float CrystalVolleyLength(Vector2 origin, Vector2 target) =>
        Mathf.Clamp(Vector2.Distance(origin, target) + 1.25f, 2.8f, 7.2f);

    private void ResolveAreaPulse()
    {
        const float radius = 3.2f;
        Color color = biomeIndex == 0 ? new Color(0.55f, 0.85f, 0.38f) : biomeIndex == 1 ? new Color(1f, 0.72f, 0.35f) : new Color(0.35f, 0.88f, 1f);
        ScadrykEffects.Ring(transform.position, radius * 2f, color, 0.32f);
        if (Vector2.Distance(player.transform.position, transform.position) <= radius) player.TakeDamage(12f);
    }

    private void ResolveSupportPulse()
    {
        ScadrykEffects.Ring(transform.position, SupportPulseDiameter, new Color(0.38f, 1f, 0.6f), 0.35f);
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, SupportPulseRadius);
        foreach (Collider2D hit in nearby)
            if (hit.TryGetComponent<ScadrykEnemyHealth>(out var ally) && ally != this && !ally.dead &&
                SupportPulseContains(transform.position, ally.transform.position)) ally.Heal(15f);
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
            if (animator != null) animator.CancelWindup();
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

    internal static float PlayerCollisionRadius(ScadrykPlayerController target)
    {
        CircleCollider2D collider = target != null ? target.GetComponent<CircleCollider2D>() : null;
        if (collider == null) return 0.38f;
        Vector3 scale = target.transform.lossyScale;
        return collider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
    }

    internal static float RootLaneVisualWidth(float attackWidth, float playerRadius) => attackWidth + playerRadius * 2f;

    internal static bool RootLaneHits(Vector2 target, Vector2 start, Vector2 end, float attackWidth, float playerRadius) =>
        DistanceToSegment(target, start, end) <= RootLaneVisualWidth(attackWidth, playerRadius) * 0.5f;

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
