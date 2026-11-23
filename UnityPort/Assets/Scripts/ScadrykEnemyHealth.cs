using UnityEngine;
using System.Collections.Generic;

/// <summary>Biome-themed enemies with readable attacks; player damage comes from announced attacks, never contact alone.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class ScadrykEnemyHealth : MonoBehaviour
{
    internal const float FinalWaveOuterRadius = 6.7f;
    internal const float FinalWaveSafeRadius = 2.2f;
    internal const float FinalStarfallRadius = 1.75f;
    private const float FinalVolleyAngleStep = 0.19f;
    private const float FinalVolleyLength = 7.2f;
    private const float FinalVolleySpeed = 7.1f;
    private const float FinalVolleySpawnOffset = 0.75f;
    private const float FinalStarfallProjectileAngleStep = 0.48f;
    private const float FinalStarfallProjectileScale = 0.48f;
    private const float FinalStarfallProjectileLength = 10f;
    private const float FinalStarfallProjectileSpeed = 5.4f;
    internal const float FinalStageBreatherDuration = 0.9f;
    internal const float FinalStageAttackDelay = 3.1f;
    internal const float AreaPulseRadius = 3.2f;
    internal const float SaltPulseRadius = 3.65f;
    internal const float CoastalPulseRadius = 2.8f;
    internal const float BellPulseRadius = 5.3f;
    internal const float SupportPulseRadius = 2.5f;
    internal static float SupportPulseInnerDiameter => SupportPulseRadius * 0.9f;
    internal const float OpeningAttackSpacing = 0.52f;
    internal const int SteeringHitCapacity = 32;
    internal static float AttackRecoveryDuration(bool boss, bool finalBoss) => finalBoss ? 0.42f : boss ? 0.36f : 0.24f;
    internal static float SaltExposureDuration(bool boss, bool echo) => boss ? (echo ? 4f : 3f) : (echo ? 7f : 5f);
    internal static float TidePushStrength(bool echo) => echo ? 7.2f : 5.5f;
    internal static float TidePushDuration(bool echo) => echo ? 0.31f : 0.24f;
    private static readonly float[] SteeringAngles = { 0f, 35f, -35f, 70f, -70f, 110f, -110f, 145f, -145f, 180f };
    private static readonly List<Collider2D> SupportPulseHits = new List<Collider2D>(16);
    private static ContactFilter2D supportPulseFilter;
    private static bool supportPulseFilterReady;
    private readonly RaycastHit2D[] steeringHits = new RaycastHit2D[SteeringHitCapacity];
    private readonly bool[] blockedSteeringHeadings = new bool[10];
    private readonly GameObject[] attackWarnings = new GameObject[8];
    private int attackWarningCount;
    internal static float AreaPulseDiameter => AreaPulseRadius * 2f;
    internal static float BellPulseDiameter => BellPulseRadius * 2f;
    internal static float SupportPulseDiameter => SupportPulseRadius * 2f;
    internal static float FinalWaveOuterDiameter => FinalWaveOuterRadius * 2f;
    internal static float FinalWaveSafeDiameter => FinalWaveSafeRadius * 2f;
    internal static bool AreaPulseContains(Vector2 origin, Vector2 target) =>
        Vector2.Distance(origin, target) <= AreaPulseRadius;
    internal static float AreaPulseRadiusForBiome(int biome) => Mathf.Clamp(biome, 0, 2) switch
    {
        0 => AreaPulseRadius,
        1 => SaltPulseRadius,
        _ => CoastalPulseRadius
    };
    internal static float AreaPulseDiameterForBiome(int biome) => AreaPulseRadiusForBiome(biome) * 2f;
    internal static string AreaPulseLabelForBiome(int biome) => Mathf.Clamp(biome, 0, 2) switch
    {
        0 => "pulso de raízes",
        1 => "pulso de sal",
        _ => "onda do recife"
    };
    internal static bool AreaPulseContains(Vector2 origin, Vector2 target, int biome) =>
        Vector2.Distance(origin, target) <= AreaPulseRadiusForBiome(biome);
    internal static float CoastalPulseKnockbackSpeed => 4.2f;
    internal static float CoastalPulseKnockbackDuration => 0.18f;
    internal static float SaltPulseSlowDuration => 0.9f;
    internal static float ForestPulseRootDuration => 0.65f;
    internal static bool BellPulseContains(Vector2 origin, Vector2 target) =>
        Vector2.Distance(origin, target) <= BellPulseRadius;
    internal static bool SupportPulseContains(Vector2 origin, Vector2 target) =>
        Vector2.Distance(origin, target) <= SupportPulseRadius;
    internal static bool SupportPulseWouldHeal(float allyHealth, float allyMaxHealth, float distance) =>
        allyMaxHealth > 0f && allyHealth < allyMaxHealth - 0.01f && distance <= SupportPulseRadius;
    internal static Vector2 AnnouncedAttackDirection(Vector2 origin, Vector2 announcedTarget)
    {
        Vector2 direction = announcedTarget - origin;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
    }
    internal static bool FinalWaveHitsAtDistance(float distance) =>
        distance > FinalWaveSafeRadius && distance < FinalWaveOuterRadius;
    internal static bool TidebreakerCancelsWindup(bool boss, bool windingUp) => !boss && windingUp;
    [SerializeField, Min(1f)] private float maxHealth = 60f;
    private float health, nextAttackAt, windupEndsAt;
    private float lastAttackStartedAt = -10f;
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
    private bool attackStartSlotReserved;
    private Sprite[] animationFrames;
    private Vector2 attackTarget;
    private int biomeIndex;
    private float moveSpeed;
    private bool isFinalBoss;
    private int finalAttackIndex;
    private int queuedFinalPattern;
    private int queuedFinalStage;
    private string lastAttackReadabilityLabel;
    private bool pendingFinalStageBreather;
    private float finalStageBreatherUntil;
    private int combatRole;
    private Color roleTint = Color.white;
    private float animationPhase;
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
    public int DisplayStage => !isFinalBoss ? 0 : isWindup ? queuedFinalStage : FinalStage;
    public int BiomeIndex => biomeIndex;

    internal static ScadrykCue WarningCueFor(int biome, bool boss, bool finalBoss, int role)
    {
        if (finalBoss) return FinalBossPatternWarningCue(0);
        int attackRole = boss ? biome == 0 ? 0 : biome == 1 ? 1 : 2 : Mathf.Clamp(role, 0, 3);
        return attackRole switch
        {
            0 => ScadrykCue.LaneWarning,
            1 => ScadrykCue.VolleyWarning,
            2 => ScadrykCue.AreaWarning,
            _ => ScadrykCue.SupportWarning
        };
    }

    internal static ScadrykCue FinalBossPatternWarningCue(int pattern) => Mathf.Clamp(pattern, 0, 2) switch
    {
        0 => ScadrykCue.FinalVolleyWarning,
        1 => ScadrykCue.FinalStarfallWarning,
        _ => ScadrykCue.FinalWaveWarning
    };

    internal static string AttackReadabilityLabel(int biome, bool boss, bool finalBoss, int role, int finalPattern, int finalStage)
    {
        if (finalBoss)
        {
            string pattern = finalPattern switch
            {
                0 => "salva de Ecos · entre as linhas",
                1 => "queda de memória · saia da marca",
                _ => "onda de vazio · centro seguro"
            };
            return "Arauto fase " + Mathf.Clamp(finalStage, 1, 3) + " — " + pattern;
        }
        if (boss) return biome switch
        {
            0 => "Raiz-Mor — avanço de raízes",
            1 => "Vigia do Sal — salva de estilhaços",
            _ => "Vigia do Coro — pulso de sino"
        };
        string roleName = Mathf.Clamp(role, 0, 3) switch
        {
            0 => "Caçador",
            1 => "Atirador",
            2 => "Ressonador",
            _ => "Tecelão"
        };
        string attackName = Mathf.Clamp(role, 0, 3) switch
        {
            0 => biome switch { 0 => "avanço de raízes", 1 => "estocada de sal", _ => "investida do recife" },
            1 => ScadrykEnemyProjectile.LabelForBiome(biome),
            2 => AreaPulseLabelForBiome(biome),
            _ => "cura dos Ecos"
        };
        return roleName + " — " + attackName;
    }

    internal bool TryGetRecentAttackLabel(float now, out string label)
    {
        label = null;
        if (dead || (!isWindup && now - lastAttackStartedAt > 2.5f)) return false;
        label = lastAttackReadabilityLabel;
        return !string.IsNullOrEmpty(label);
    }

    internal bool TryGetWindupAttackLabel(out string label)
        => TryGetWindupAttackLabel(out label, out _);

    internal bool TryGetWindupAttackLabel(out string label, out float secondsUntilImpact)
    {
        label = null;
        secondsUntilImpact = float.PositiveInfinity;
        if (dead || !isWindup) return false;
        label = lastAttackReadabilityLabel;
        secondsUntilImpact = Mathf.Max(0f, windupEndsAt - Time.time);
        return !string.IsNullOrEmpty(label);
    }

    internal static Color StateTint(bool windingUp, bool exposed, Color roleTint) =>
        windingUp ? new Color(1f, 0.68f, 0.24f) : exposed ? new Color(0.58f, 0.9f, 1f) : roleTint;

    internal static Color RoleTintForBiome(int biome, int role)
    {
        Color roleColor = Mathf.Clamp(role, 0, 3) switch
        {
            0 => new Color(1f, 0.87f, 0.75f),
            1 => new Color(0.72f, 0.9f, 1f),
            2 => new Color(1f, 0.76f, 0.85f),
            _ => new Color(0.77f, 1f, 0.82f)
        };
        Color biomeColor = Mathf.Clamp(biome, 0, 2) switch
        {
            0 => new Color(0.35f, 0.67f, 0.42f),
            1 => new Color(0.88f, 0.61f, 0.27f),
            _ => new Color(0.24f, 0.55f, 0.78f)
        };
        return Color.Lerp(roleColor, biomeColor, 0.3f);
    }

    internal static bool RoleTintPalettesAreLegible()
    {
        const float minimumColorDistanceSquared = 0.0064f;
        for (int biome = 0; biome < 3; biome++)
        for (int firstRole = 0; firstRole < 4; firstRole++)
        for (int secondRole = firstRole + 1; secondRole < 4; secondRole++)
            if (ColorDistanceSquared(RoleTintForBiome(biome, firstRole), RoleTintForBiome(biome, secondRole)) < minimumColorDistanceSquared)
                return false;
        for (int role = 0; role < 4; role++)
        for (int firstBiome = 0; firstBiome < 3; firstBiome++)
        for (int secondBiome = firstBiome + 1; secondBiome < 3; secondBiome++)
            if (ColorDistanceSquared(RoleTintForBiome(firstBiome, role), RoleTintForBiome(secondBiome, role)) < minimumColorDistanceSquared)
                return false;
        return true;
    }

    private static float ColorDistanceSquared(Color first, Color second)
    {
        float red = first.r - second.r, green = first.g - second.g, blue = first.b - second.b;
        return red * red + green * green + blue * blue;
    }

    public void Configure(float hp, float speed, bool boss, int biome, Sprite[] frames, int role = 0, float openingAttackDelay = 0f)
    {
        lastAttackReadabilityLabel = null;
        maxHealth = hp; health = hp; moveSpeed = speed; IsBoss = boss;
        biomeIndex = Mathf.Clamp(biome, 0, 2);
        combatRole = boss ? 0 : Mathf.Clamp(role, 0, 3);
        nextAttackAt = Time.time + Mathf.Max(0f, openingAttackDelay);
        attackStartSlotReserved = false;
        roleTint = boss ? Color.white : RoleTintForBiome(biomeIndex, combatRole);
        attackRange = boss ? (biomeIndex == 1 ? 8f : biomeIndex == 0 ? 6.8f : 5.8f) :
            combatRole == 0 ? 3.1f : combatRole == 1 ? 7.2f : combatRole == 2 ? 3.9f : 5.5f;
        animationFrames = frames;
        if (sprite != null && animationFrames != null && animationFrames.Length > 0) sprite.sprite = animationFrames[0];
        var collider = GetComponent<CircleCollider2D>();
        collider.radius = boss ? 0.58f : 0.34f;
    }

    public void ConfigureFinalBoss(Sprite[] frames)
    {
        lastAttackReadabilityLabel = null;
        isFinalBoss = true;
        IsBoss = true;
        nextAttackAt = Time.time + OpeningAttackDelay(0, true);
        attackStartSlotReserved = false;
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
        animationPhase = Random.value;
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

    private void OnDestroy() => ClearAttackWarnings();

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
                sprite.sprite = animationFrames[BodyVelocity.sqrMagnitude > 0.05f ? AnimationFrameIndex(Time.time, 7f, animationFrames.Length, animationPhase) : 0];
        }

        if (Time.time < knockbackUntil) { BodyVelocity = knockbackVelocity; return; }
        if (Time.time < staggerUntil) { BodyVelocity = Vector2.zero; return; }

        if (attackStartSlotReserved && (distance > attackRange || shotBlocked)) attackStartSlotReserved = false;
        if (attackStartSlotReserved && Time.time < nextAttackAt) { BodyVelocity = Vector2.zero; return; }

        if (isWindup)
        {
            BodyVelocity = Vector2.zero;
            if (Time.time >= windupEndsAt) ResolveAttack();
            return;
        }

        if (windupEndsAt > 0f && Time.time >= windupEndsAt) ResolveAttack();
        if (Time.time < finalStageBreatherUntil) { BodyVelocity = Vector2.zero; return; }
        if (Time.time < recoveryUntil) { BodyVelocity = Vector2.zero; return; }
        if (Time.time >= nextAttackAt && distance <= attackRange && !shotBlocked)
        {
            if (director != null)
            {
                if (!attackStartSlotReserved)
                {
                    nextAttackAt = director.ScheduleEnemyAttackStart(Time.time);
                    attackStartSlotReserved = true;
                }
                if (Time.time < nextAttackAt) { BodyVelocity = Vector2.zero; return; }
            }
            attackStartSlotReserved = false;
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
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.DefaultRaycastLayers);
        filter.useTriggers = false;
        int hitCount = Physics2D.CircleCast(body.position, radius * 0.92f, heading, filter, steeringHits, distance);
        if (SteeringQueryIsSaturated(hitCount)) return true;
        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            Collider2D hit = steeringHits[hitIndex].collider;
            if (hit == null || !hit.enabled || hit.isTrigger || hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
            if (hit.TryGetComponent<ScadrykEnemyHealth>(out _) || hit.TryGetComponent<ScadrykPlayerController>(out _) ||
                hit.TryGetComponent<ScadrykEnemyProjectile>(out _)) continue;
            return true;
        }
        return false;
    }

    internal static bool SteeringQueryIsSaturated(int hitCount) => hitCount >= SteeringHitCapacity;

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

    private void TrackAttackWarning(GameObject warning)
    {
        if (warning == null) return;
        ScadrykTimedVisual timedWarning = warning.GetComponent<ScadrykTimedVisual>();
        if (timedWarning != null) timedWarning.MarkDangerWarning();
        if (attackWarningCount < attackWarnings.Length) attackWarnings[attackWarningCount++] = warning;
    }

    private void ClearAttackWarnings()
    {
        attackWarningCount = ClearTrackedAttackWarnings(attackWarnings, attackWarningCount);
    }

    internal static int ClearTrackedAttackWarnings(GameObject[] warnings, int count)
    {
        if (warnings == null) return 0;
        int safeCount = Mathf.Clamp(count, 0, warnings.Length);
        for (int i = 0; i < safeCount; i++)
        {
            GameObject warning = warnings[i];
            warnings[i] = null;
            if (warning == null) continue;
            warning.SetActive(false);
            Destroy(warning);
        }
        return 0;
    }

    private void BeginAttack()
    {
        if (!IsBoss && combatRole == 3 && !HasInjuredSupportTarget())
        {
            nextAttackAt = Time.time + 0.4f;
            return;
        }
        ClearAttackWarnings();
        attackTarget = player.transform.position;
        if (isFinalBoss)
        {
            BeginFinalBossAttack();
            return;
        }
        ScadrykAudio.Play(WarningCueFor(biomeIndex, IsBoss, false, combatRole));
        lastAttackStartedAt = Time.time;
        lastAttackReadabilityLabel = AttackReadabilityLabel(biomeIndex, IsBoss, false, combatRole, queuedFinalPattern, queuedFinalStage);
        float windup = IsBoss ? (biomeIndex == 0 ? 0.95f : biomeIndex == 1 ? 0.86f : 1.05f) :
            combatRole == 0 ? 0.7f : combatRole == 1 ? 0.85f : combatRole == 2 ? 0.95f : 1.1f;
        if (animator != null) animator.PlayWindup(AnnouncedAttackDirection(transform.position, attackTarget), windup);
        isWindup = true;
        windupEndsAt = Time.time + windup;
        Vector2 origin = transform.position;

        Color warning = biomeIndex == 0 ? new Color(0.64f, 0.86f, 0.35f, 0.85f) :
            biomeIndex == 1 ? new Color(1f, 0.78f, 0.34f, 0.85f) : new Color(0.34f, 0.9f, 1f, 0.85f);
        if (!IsBoss && combatRole == 3)
        {
            foreach (GameObject signal in ScadrykEffects.SupportPulseSignal(origin, new Color(0.38f, 1f, 0.6f, 0.8f), windup, true))
                TrackAttackWarning(signal);
        }
        else if (!IsBoss && combatRole == 2)
            TrackAttackWarning(ScadrykEffects.Ring(origin, AreaPulseDiameterForBiome(biomeIndex), warning, windup, true));
        else if (IsBoss && biomeIndex == 2)
            TrackAttackWarning(ScadrykEffects.Ring(origin, BellPulseDiameter, warning, windup, true));
        else if (IsBoss && biomeIndex == 1)
        {
            Vector2 direction = (attackTarget - origin).normalized;
            float length = CrystalVolleyLength(origin, attackTarget);
            for (int i = -1; i <= 1; i++)
            {
                Vector2 shardDirection = Rotate(direction, i * 0.32f);
                float shardWidth = ScadrykEnemyProjectile.HitboxDiameter(0.34f);
                Vector2 shardEnd = ScadrykCombatRules.ClipWarningLineEnd(origin, origin + shardDirection * length, shardWidth);
                TrackAttackWarning(ScadrykEffects.TelegraphLine(origin, shardEnd, warning, shardWidth, windup));
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
            end = ScadrykCombatRules.ClipWarningLineEnd(origin, end, telegraphThickness);
            TrackAttackWarning(ScadrykEffects.TelegraphLine(origin, end, warning, telegraphThickness, windup));
        }
    }

    private void BeginFinalBossAttack()
    {
        queuedFinalStage = FinalStage;
        int stage = queuedFinalStage;
        queuedFinalPattern = finalAttackIndex % (stage == 1 ? 2 : 3);
        lastAttackReadabilityLabel = AttackReadabilityLabel(biomeIndex, IsBoss, true, combatRole, queuedFinalPattern, queuedFinalStage);
        ScadrykAudio.Play(FinalBossPatternWarningCue(queuedFinalPattern));
        lastAttackStartedAt = Time.time;
        float windup = stage == 3 ? 1.18f : 1.35f;
        if (animator != null) animator.PlayWindup(AnnouncedAttackDirection(transform.position, attackTarget), windup);
        isWindup = true;
        windupEndsAt = Time.time + windup;
        Vector2 origin = transform.position;
        Color warning = stage == 1 ? new Color(0.62f, 0.9f, 0.42f, 0.82f) : stage == 2 ? new Color(1f, 0.72f, 0.32f, 0.82f) : new Color(0.98f, 0.26f, 0.62f, 0.85f);
        if (queuedFinalPattern == 0)
        {
            Vector2 direction = (attackTarget - origin).normalized;
            int halfCount = stage == 1 ? 1 : stage == 2 ? 2 : 3;
            for (int i = -halfCount; i <= halfCount; i++)
            {
                float shardWidth = ScadrykEnemyProjectile.HitboxDiameter(0.58f);
                Vector2 shardEnd = ScadrykCombatRules.ClipWarningLineEnd(origin,
                    origin + Rotate(direction, i * FinalVolleyAngleStep) * FinalVolleyLength, shardWidth);
                TrackAttackWarning(ScadrykEffects.TelegraphLine(origin, shardEnd, warning, shardWidth, windup));
            }
        }
        else if (queuedFinalPattern == 1)
        {
            float diameter = FinalStarfallVisualDiameter;
            TrackAttackWarning(ScadrykEffects.Voidburst(attackTarget, diameter, windup, true));
            TrackAttackWarning(ScadrykEffects.Ring(attackTarget, diameter, warning, windup + 0.46f, true));
            if (stage > 1)
            {
                Vector2 direction = (attackTarget - origin).normalized;
                int count = FinalStarfallSideProjectileCount(stage);
                for (int i = 0; i < count; i++)
                {
                    Vector2 shardDirection = Rotate(direction, FinalStarfallSideProjectileAngle(stage, i));
                    float shardWidth = ScadrykEnemyProjectile.HitboxDiameter(FinalStarfallProjectileScale);
                    Vector2 shardEnd = ScadrykCombatRules.ClipWarningLineEnd(origin,
                        origin + shardDirection * FinalStarfallProjectileLength, shardWidth);
                    TrackAttackWarning(ScadrykEffects.TelegraphLine(origin, shardEnd, warning, shardWidth, windup));
                }
            }
        }
        else
        {
            TrackAttackWarning(ScadrykEffects.Ring(origin, FinalWaveOuterRadius * 2f, warning, windup, true));
            TrackAttackWarning(ScadrykEffects.Ring(origin, FinalWaveSafeRadius * 2f, new Color(0.55f, 1f, 0.82f, 0.92f), windup, true));
        }
    }

    private void ResolveAttack()
    {
        isWindup = false;
        ScadrykAudio.Play(ScadrykCue.Impact);
        windupEndsAt = 0f;
        nextAttackAt = Time.time + (IsBoss ? 2.3f : 2.6f);
        if (animator != null) animator.PlayAttack(AnnouncedAttackDirection(transform.position, attackTarget), IsBoss);
        recoveryUntil = Time.time + AttackRecoveryDuration(IsBoss, isFinalBoss);
        if (player == null) return;

        if (isFinalBoss)
        {
            ResolveFinalBossAttack();
            if (pendingFinalStageBreather) BeginFinalStageBreather();
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

    internal static int AnimationFrameIndex(float time, float framesPerSecond, int frameCount, float normalizedPhase)
    {
        if (frameCount <= 0) return 0;
        float frameTime = Mathf.Max(0f, time) * Mathf.Max(0f, framesPerSecond) + Mathf.Repeat(normalizedPhase, 1f) * frameCount;
        return Mathf.FloorToInt(frameTime) % frameCount;
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
            float diameter = FinalWaveOuterDiameter;
            ScadrykEffects.Voidburst(origin, diameter, 0.42f);
            ScadrykEffects.Ring(origin, diameter, new Color(0.98f, 0.24f, 0.57f, 0.96f), 0.42f);
            if (FinalWaveHitsAtDistance(distance)) player.TakeDamage(stage == 3 ? 27f : 23f);
        }
    }

    private void BeginFinalStageBreather()
    {
        pendingFinalStageBreather = false;
        finalStageBreatherUntil = Mathf.Max(finalStageBreatherUntil, Time.time + FinalStageBreatherDuration);
        recoveryUntil = Mathf.Max(recoveryUntil, finalStageBreatherUntil);
        nextAttackAt = Mathf.Max(nextAttackAt, Time.time + FinalStageAttackDelay);
        BodyVelocity = Vector2.zero;
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
    internal static float OpeningAttackDelay(int enemyIndex, bool boss, bool campaignOpeningRoom = false) =>
        boss ? 1.25f : (campaignOpeningRoom ? 2.6f : 0.3f) + Mathf.Max(0, enemyIndex) * OpeningAttackSpacing;
    internal static float ReserveEnemyAttackSlot(ref float nextAvailableAt, float requestedAt)
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
        end = ScadrykCombatRules.ClipWarningLineEnd(start, end, warnedWidth);
        float visibleLength = Vector2.Distance(start, end);
        Color laneColor = biomeIndex == 0 ? new Color(0.42f, 0.72f, 0.28f, 0.95f) :
            biomeIndex == 1 ? new Color(0.94f, 0.69f, 0.31f, 0.95f) : new Color(0.3f, 0.78f, 0.88f, 0.95f);
        Sprite laneSprite = biomeIndex == 0 ? ScadrykEffects.RootThrustSprite :
            biomeIndex == 1 ? ScadrykEffects.SaltShardSprite : ScadrykEffects.TidebreakerSprite;
        if (visibleLength > 0.05f)
            ScadrykEffects.GroundLane((start + end) * 0.5f, direction, visibleLength, warnedWidth, laneColor, 0.3f, laneSprite);
        if (RootLaneHits(player.transform.position, start, end, width, PlayerCollisionRadius(player)) &&
            !ScadrykCombatRules.WorldBlocks(start, player.transform.position))
        {
            bool hit = player.TakeDamage(IsBoss ? 21f : 13f);
            if (hit)
            {
                if (biomeIndex == 0) player.ApplyRootSlow(IsBoss ? 1.55f : ForestPulseRootDuration);
                else if (biomeIndex == 1) player.ApplySaltCrustSlow(0.48f);
                else player.ApplyImpactKnockback(start, 2.4f, 0.12f);
            }
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
                travelDistance: Mathf.Max(0f, travelDistance - 0.55f - ScadrykEnemyProjectile.HitboxRadius(0.34f)),
                hitEffectBiome: biomeIndex);
        }
    }

    private static float CrystalVolleyLength(Vector2 origin, Vector2 target) =>
        Mathf.Clamp(Vector2.Distance(origin, target) + 1.25f, 2.8f, 7.2f);

    private void ResolveAreaPulse()
    {
        Color color = biomeIndex == 0 ? new Color(0.55f, 0.85f, 0.38f) : biomeIndex == 1 ? new Color(1f, 0.72f, 0.35f) : new Color(0.35f, 0.88f, 1f);
        Vector2 origin = transform.position;
        ScadrykEffects.Ring(origin, AreaPulseDiameterForBiome(biomeIndex), color, 0.32f);
        if (!AreaPulseContains(origin, player.transform.position, biomeIndex) || !player.TakeDamage(12f)) return;
        if (biomeIndex == 0) player.ApplyRootSlow(ForestPulseRootDuration);
        else if (biomeIndex == 1) player.ApplySaltCrustSlow(SaltPulseSlowDuration);
        else player.ApplyImpactKnockback(origin, CoastalPulseKnockbackSpeed, CoastalPulseKnockbackDuration);
    }

    private void ResolveSupportPulse()
    {
        ScadrykEffects.SupportPulseSignal(transform.position, new Color(0.38f, 1f, 0.6f), 0.35f, false);
        if (!supportPulseFilterReady)
        {
            supportPulseFilter = new ContactFilter2D();
            supportPulseFilter.SetLayerMask(~0);
            supportPulseFilter.useTriggers = Physics2D.queriesHitTriggers;
            supportPulseFilterReady = true;
        }
        SupportPulseHits.Clear();
        int hitCount = Physics2D.OverlapCircle(transform.position, SupportPulseRadius, supportPulseFilter, SupportPulseHits);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = SupportPulseHits[i];
            if (hit == null) continue;
            if (hit.TryGetComponent<ScadrykEnemyHealth>(out var ally) && ally != this && !ally.dead &&
                SupportPulseWouldHeal(ally.health, ally.maxHealth, Vector2.Distance(transform.position, ally.transform.position))) ally.Heal(15f);
        }
    }

    private bool HasInjuredSupportTarget()
    {
        if (!supportPulseFilterReady)
        {
            supportPulseFilter = new ContactFilter2D();
            supportPulseFilter.SetLayerMask(~0);
            supportPulseFilter.useTriggers = Physics2D.queriesHitTriggers;
            supportPulseFilterReady = true;
        }
        SupportPulseHits.Clear();
        int hitCount = Physics2D.OverlapCircle(transform.position, SupportPulseRadius, supportPulseFilter, SupportPulseHits);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = SupportPulseHits[i];
            if (hit == null || !hit.TryGetComponent<ScadrykEnemyHealth>(out var ally) || ally == this || ally.dead) continue;
            float distance = Vector2.Distance(transform.position, ally.transform.position);
            if (SupportPulseWouldHeal(ally.health, ally.maxHealth, distance)) return true;
        }
        return false;
    }

    private void Heal(float amount) { health = Mathf.Min(maxHealth, health + amount); }

    public void ApplyCrescentControl()
    {
        if (dead) return;
        staggerUntil = Mathf.Max(staggerUntil, Time.time + (IsBoss ? 0.26f : 0.82f));
        if (isWindup)
        {
            ClearAttackWarnings();
            isWindup = false;
            windupEndsAt = 0f;
            nextAttackAt = staggerUntil + 0.8f;
            if (animator != null) animator.CancelWindup();
            if (isFinalBoss && pendingFinalStageBreather) BeginFinalStageBreather();
        }
    }

    public void ApplyBasicFinisherStagger()
    {
        if (dead || IsBoss) return;
        staggerUntil = Mathf.Max(staggerUntil, Time.time + 0.24f);
        if (!isWindup) return;
        ClearAttackWarnings();
        isWindup = false;
        windupEndsAt = 0f;
        nextAttackAt = staggerUntil + 0.3f;
        if (animator != null) animator.CancelWindup();
    }

    public void ApplySaltExposure(bool echo = false)
    {
        if (!dead) exposedUntil = Mathf.Max(exposedUntil, Time.time + SaltExposureDuration(IsBoss, echo));
    }

    public void ApplyTidePush(Vector2 direction, bool echo = false)
    {
        if (dead || IsBoss) return;
        knockbackVelocity = direction.sqrMagnitude > 0.01f ? direction.normalized * TidePushStrength(echo) : Vector2.zero;
        knockbackUntil = Time.time + TidePushDuration(echo);
        staggerUntil = Mathf.Max(staggerUntil, knockbackUntil + 0.25f);
        if (!TidebreakerCancelsWindup(IsBoss, isWindup)) return;
        ClearAttackWarnings();
        isWindup = false;
        windupEndsAt = 0f;
        nextAttackAt = staggerUntil + 0.45f;
        if (animator != null) animator.CancelWindup();
    }

    private void ResolveBellPulse()
    {
        float radius = IsBoss ? BellPulseRadius : 3.65f;
        ScadrykEffects.Ring(transform.position, radius * 2f, new Color(0.34f, 0.9f, 1f, 0.96f), 0.32f);
        ScadrykEffects.Burst(transform.position, new Color(0.24f, 0.72f, 0.94f, 0.24f), radius * 1.5f);
        if (IsBoss ? BellPulseContains(transform.position, player.transform.position) :
            Vector2.Distance(player.transform.position, transform.position) <= radius)
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
        if (isFinalBoss && health > 0f && FinalStage != previousStage)
        {
            if (isWindup) pendingFinalStageBreather = true;
            else BeginFinalStageBreather();
            if (director != null) director.ShowBossStage(FinalStage);
        }
        if (animator != null) animator.PlayHit(player != null ? ((Vector2)transform.position - (Vector2)player.transform.position).normalized : Vector2.up);
        if (health <= 0f)
        {
            ClearAttackWarnings();
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
