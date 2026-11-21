using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Responsive top-down movement, mouse aiming, melee, dash and shockwave.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class ScadrykPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(1f)] private float moveSpeed = 6.2f;
    [SerializeField, Min(1f)] private float acceleration = 42f;
    [SerializeField, Min(1f)] private float dashSpeed = 13f;
    [SerializeField, Min(0.05f)] private float dashDuration = 0.16f;
    [SerializeField, Min(0.1f)] private float dashCooldown = 0.75f;
    [Header("Combat")]
    [SerializeField, Min(0.1f)] private float attackRange = 1.45f;
    [SerializeField, Min(0f)] private float attackDamage = 35f;
    [SerializeField, Min(0.05f)] private float attackCooldown = 0.32f;
    [SerializeField, Min(0f)] private float shockwaveDamage = 42f;
    [SerializeField, Min(0.1f)] private float shockwaveRadius = 3.2f;
    [SerializeField, Min(0.1f)] private float shockwaveCooldown = 5f;

    private Rigidbody2D body;
#if UNITY_6000_0_OR_NEWER
    private Vector2 BodyVelocity { get => body.linearVelocity; set => body.linearVelocity = value; }
#else
    private Vector2 BodyVelocity { get => body.velocity; set => body.velocity = value; }
#endif
    private Vector2 moveInput;
    private Vector2 facing = Vector2.right;
    private Vector2 dashDirection;
    private float dashEndsAt, nextDashAt, nextAttackAt, nextShockwaveAt, dashBufferedUntil, attackBufferedUntil;
    private float health = 120f;
    private float maxHealth = 120f;
    private int lastBasicAttackStep = -1;
    private float basicAttackComboUntil;
    private bool isDying;
    private float invulnerableUntil;
    private float rootedUntil;
    private readonly bool[] learnedAbilities = new bool[3];
    private readonly float[] abilityReadyAt = new float[3];
    private SpriteRenderer sprite;
    private ScadrykActorAnimator animator;
    private ScadrykGameDirector director;
    private Sprite[] spriteFrames;
    private const float BasicAttackWindup = 0.12f;
    private const int BasicAttackComboLength = 3;
    internal const float BasicAttackComboWindow = 0.82f;
    internal const float BasicAttackBufferWindow = 0.2f;
    internal const float DashBufferWindow = 0.12f;
    internal static float AbilityWindupFor(int index) => Mathf.Clamp(index, 0, 2) switch
    {
        0 => 0.10f,
        1 => 0.14f,
        _ => 0.18f
    };
    internal static float AbilityRecoveryFor(int index) => Mathf.Clamp(index, 0, 2) switch
    {
        0 => 0.24f,
        1 => 0.30f,
        _ => 0.36f
    };
    private Vector3 lastMousePosition;
    private bool usingGamepadAim;
    private bool hasIndependentGamepadAim;
    private readonly HashSet<ScadrykTerrainZone> terrainZones = new HashSet<ScadrykTerrainZone>();
    private float terrainSpeedScale = 1f;

    public Vector2 Facing => facing;
    public float Health => health;
    public float MaxHealth => maxHealth;
    public float ShockwaveRadius => shockwaveRadius;
    public float Health01 => maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth);
    public float DashCooldownLeft => Mathf.Max(0f, nextDashAt - Time.time);
    public float ShockwaveCooldownLeft => Mathf.Max(0f, nextShockwaveAt - Time.time);
    public bool HasAbility(int index) => index >= 0 && index < learnedAbilities.Length && learnedAbilities[index];
    public float AbilityCooldownLeft(int index) => index < 0 || index >= abilityReadyAt.Length ? 0f : Mathf.Max(0f, abilityReadyAt[index] - Time.time);

    public void UnlockAbility(int index)
    {
        if (index < 0 || index >= learnedAbilities.Length) return;
        learnedAbilities[index] = true;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        Transform visual = transform.Find("Animated visual");
        sprite = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        animator = GetComponent<ScadrykActorAnimator>();
        director = FindAnyObjectByType<ScadrykGameDirector>();
    }

    private void Update()
    {
        if (isDying || (director != null && director.IsPaused))
        {
            moveInput = Vector2.zero;
            dashBufferedUntil = attackBufferedUntil = 0f;
            return;
        }

        Vector2 keyboard = new Vector2((ScadrykSettings.Held(10) ? 1f : 0f) - (ScadrykSettings.Held(9) ? 1f : 0f),
            (ScadrykSettings.Held(7) ? 1f : 0f) - (ScadrykSettings.Held(8) ? 1f : 0f));
        Vector2 stick = new Vector2(Input.GetAxisRaw("Scadryk Move X"), Input.GetAxisRaw("Scadryk Move Y"));
        moveInput = Vector2.ClampMagnitude(keyboard + stick, 1f);
        bool keyboardAttack = ScadrykSettings.Down(0), gamepadAttack = ScadrykSettings.PadDown(0);
        bool keyboardDash = ScadrykSettings.Down(1), gamepadDash = ScadrykSettings.PadDown(1);
        bool keyboardPulse = ScadrykSettings.Down(2), gamepadPulse = ScadrykSettings.PadDown(2);
        bool keyboardCrescent = ScadrykSettings.Down(3), gamepadCrescent = ScadrykSettings.PadDown(3);
        bool keyboardSaltburst = ScadrykSettings.Down(4), gamepadSaltburst = ScadrykSettings.PadDown(4);
        bool keyboardTidebreaker = ScadrykSettings.Down(5), gamepadTidebreaker = ScadrykSettings.PadDown(5);
        bool keyboardInteract = ScadrykSettings.Down(6), gamepadInteract = ScadrykSettings.PadDown(6);
        bool mouseAttack = Input.GetMouseButtonDown(0), mousePulse = Input.GetMouseButtonDown(1);
        bool keyboardAction = keyboardAttack || keyboardDash || keyboardPulse || keyboardCrescent || keyboardSaltburst || keyboardTidebreaker || keyboardInteract || mouseAttack || mousePulse;
        bool gamepadAction = gamepadAttack || gamepadDash || gamepadPulse || gamepadCrescent || gamepadSaltburst || gamepadTidebreaker || gamepadInteract;
        if (director != null)
        {
            if (keyboard.sqrMagnitude > 0.01f || keyboardAction || Input.mousePosition != lastMousePosition)
                director.RecordInputMode("keyboard_mouse");
            if (stick.sqrMagnitude > 0.01f || gamepadAction)
                director.RecordInputMode("gamepad");
        }
        AimAtPointer(keyboard.sqrMagnitude > 0.01f || keyboardAction, stick.sqrMagnitude > 0.01f || gamepadAction);
        if (keyboardDash || gamepadDash) dashBufferedUntil = Time.time + DashBufferWindow;
        TryStartBufferedDash();
        if (mouseAttack || keyboardAttack || gamepadAttack) attackBufferedUntil = Time.time + BasicAttackBufferWindow;
        TryStartBufferedAttack();
        if (keyboardPulse || mousePulse || gamepadPulse) Shockwave();
        if (keyboardCrescent || gamepadCrescent) UseAbility(0);
        if (keyboardSaltburst || gamepadSaltburst) UseAbility(1);
        if (keyboardTidebreaker || gamepadTidebreaker) UseAbility(2);
        if ((keyboardInteract || gamepadInteract) && director != null) director.TryInteract(transform.position);

        UpdateSprite();
    }

    private void FixedUpdate()
    {
        if (isDying || (director != null && director.IsPaused)) { BodyVelocity = Vector2.zero; return; }
        if (Time.time < dashEndsAt)
        {
            BodyVelocity = dashDirection * dashSpeed;
            return;
        }
        float effectiveSpeed = moveSpeed * terrainSpeedScale;
        if (Time.time < rootedUntil) effectiveSpeed *= 0.52f;
        BodyVelocity = Vector2.MoveTowards(BodyVelocity, moveInput * effectiveSpeed, acceleration * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<ScadrykTerrainZone>(out var zone) || !terrainZones.Add(zone)) return;
        RefreshTerrainSpeed();
        if (director != null) director.ShowShallowWaterHint();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent<ScadrykTerrainZone>(out var zone) || !terrainZones.Remove(zone)) return;
        RefreshTerrainSpeed();
    }

    private void RefreshTerrainSpeed()
    {
        terrainSpeedScale = 1f;
        foreach (ScadrykTerrainZone zone in terrainZones)
            if (zone != null) terrainSpeedScale = Mathf.Min(terrainSpeedScale, zone.MovementScale);
    }

    private void AimAtPointer(bool keyboardMouseInput, bool gamepadInput)
    {
        if (Input.mousePosition != lastMousePosition)
        {
            keyboardMouseInput = true;
            lastMousePosition = Input.mousePosition;
        }
        Vector2 stick = new Vector2(Input.GetAxisRaw("Scadryk Aim X"), -Input.GetAxisRaw("Scadryk Aim Y"));
        if (stick.sqrMagnitude > 0.09f)
        {
            hasIndependentGamepadAim = true;
            facing = ResolveGamepadFacing(facing, stick, Vector2.zero, true);
            usingGamepadAim = true;
            if (director != null) director.RecordInputMode("gamepad");
            return;
        }
        if (keyboardMouseInput) { usingGamepadAim = false; hasIndependentGamepadAim = false; }
        if (gamepadInput) usingGamepadAim = true;
        if (usingGamepadAim)
        {
            facing = ResolveGamepadFacing(facing, Vector2.zero,
                new Vector2(Input.GetAxisRaw("Scadryk Move X"), Input.GetAxisRaw("Scadryk Move Y")), hasIndependentGamepadAim);
            return;
        }
        if (keyboardMouseInput && director != null) director.RecordInputMode("keyboard_mouse");
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 pointer = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 aim = (Vector2)pointer - body.position;
        if (aim.sqrMagnitude > 0.03f) facing = aim.normalized;
    }

    internal static Vector2 ResolveGamepadFacing(Vector2 lastFacing, Vector2 aimStick, Vector2 moveStick, bool hasIndependentAim)
    {
        if (aimStick.sqrMagnitude > 0.09f) return aimStick.normalized;
        if (!hasIndependentAim && moveStick.sqrMagnitude > 0.01f) return moveStick.normalized;
        return lastFacing.sqrMagnitude > 0.001f ? lastFacing.normalized : Vector2.right;
    }

    public void SetSpriteFrames(Sprite[] frames)
    {
        spriteFrames = frames;
        Transform visual = transform.Find("Animated visual");
        if (visual != null) sprite = visual.GetComponent<SpriteRenderer>();
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (sprite == null) return;
        if (spriteFrames != null && spriteFrames.Length >= 16)
        {
            int row = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) ? (facing.x < 0f ? 1 : 2) : (facing.y < 0f ? 0 : 3);
            int column = moveInput.sqrMagnitude > 0.02f ? Mathf.FloorToInt(Time.time * 8f) % 4 : 0;
            sprite.sprite = spriteFrames[row * 4 + column];
        }
        sprite.color = Time.time < invulnerableUntil && (ScadrykSettings.ReduceFlash || Mathf.FloorToInt(Time.time * 18f) % 2 != 0) ? new Color(0.45f, 0.85f, 1f) : Time.time < rootedUntil ? new Color(0.72f, 0.92f, 0.56f) : Color.white;
    }

    internal static bool InputBufferCanStart(float now, float bufferedUntil, float cooldownEndsAt) =>
        bufferedUntil > now && now >= cooldownEndsAt;
    internal static bool DashBufferCanStart(float now, float bufferedUntil, float cooldownEndsAt) =>
        InputBufferCanStart(now, bufferedUntil, cooldownEndsAt);

    private void TryStartBufferedDash()
    {
        if (!DashBufferCanStart(Time.time, dashBufferedUntil, nextDashAt))
        {
            if (Time.time >= dashBufferedUntil) dashBufferedUntil = 0f;
            return;
        }
        dashBufferedUntil = 0f;
        Dash();
    }

    private void TryStartBufferedAttack()
    {
        if (!InputBufferCanStart(Time.time, attackBufferedUntil, nextAttackAt))
        {
            if (Time.time >= attackBufferedUntil) attackBufferedUntil = 0f;
            return;
        }
        attackBufferedUntil = 0f;
        int comboStep = NextBasicAttackStep(lastBasicAttackStep, lastBasicAttackStep >= 0 && Time.time <= basicAttackComboUntil);
        lastBasicAttackStep = comboStep;
        basicAttackComboUntil = Time.time + BasicAttackComboWindow;
        float attackRangeForStep = attackRange * BasicAttackRangeScale(comboStep);
        float attackDamageForStep = attackDamage * BasicAttackDamageScale(comboStep);
        float windupForStep = BasicAttackWindupForStep(comboStep);
        nextAttackAt = Time.time + attackCooldown * BasicAttackCooldownScale(comboStep);
        Vector2 direction = facing;
        ScadrykAudio.Play(ScadrykCue.Sword);
        if (animator != null) animator.PlayAttack(direction, comboStep == BasicAttackComboLength - 1);
        StartCoroutine(ResolveBasicAttack(direction, comboStep, attackDamageForStep, attackRangeForStep, windupForStep));
    }

    internal static int NextBasicAttackStep(int previousStep, bool comboActive)
    {
        if (!comboActive || previousStep < 0 || previousStep >= BasicAttackComboLength) return 0;
        return (previousStep + 1) % BasicAttackComboLength;
    }

    internal static float BasicAttackDamageScale(int step) => Mathf.Clamp(step, 0, BasicAttackComboLength - 1) == BasicAttackComboLength - 1 ? 1.5f : 1f;
    internal static float BasicAttackRangeScale(int step) => Mathf.Clamp(step, 0, BasicAttackComboLength - 1) switch
    {
        0 => 0.98f,
        1 => 1.04f,
        _ => 1.18f
    };
    internal static float BasicAttackCooldownScale(int step) => Mathf.Clamp(step, 0, BasicAttackComboLength - 1) == BasicAttackComboLength - 1 ? 1.55f : 1f;
    internal static float BasicAttackWindupForStep(int step) => Mathf.Clamp(step, 0, BasicAttackComboLength - 1) switch
    {
        0 => BasicAttackWindup,
        1 => 0.14f,
        _ => 0.2f
    };

    private IEnumerator ResolveBasicAttack(Vector2 direction, int comboStep, float damage, float range, float windup)
    {
        yield return new WaitForSeconds(windup);
        if (isDying || (director != null && director.IsPaused)) yield break;
        ScadrykEffects.Slash(transform.position, direction, range, comboStep == BasicAttackComboLength - 1);
        Vector2 footprint = ScadrykEffects.PlayerSlashFootprint(range);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, footprint.x + footprint.y, ~0);
        bool hitEnemy = false;
        foreach (Collider2D hit in hits)
        {
            if (!hit.TryGetComponent<ScadrykEnemyHealth>(out var enemy)) continue;
            if (!WithinVisualFootprint(enemy.transform.position, transform.position, direction, ScadrykEffects.PlayerSlashForwardOffset, footprint, EnemyCollisionRadius(enemy))) continue;
            if (ScadrykCombatRules.WorldBlocks(transform.position, enemy.transform.position)) continue;
            enemy.TakeDamage(damage);
            if (comboStep == BasicAttackComboLength - 1) enemy.ApplyBasicFinisherStagger();
            hitEnemy = true;
        }
        if (hitEnemy) ScadrykAudio.Play(comboStep == BasicAttackComboLength - 1 ? ScadrykCue.PlayerFinisher : ScadrykCue.PlayerStrike);
    }

    private void Dash()
    {
        if (Time.time < nextDashAt) return;
        dashDirection = moveInput.sqrMagnitude > 0.01f ? moveInput : facing;
        dashEndsAt = Time.time + dashDuration;
        nextDashAt = Time.time + dashCooldown;
        invulnerableUntil = dashEndsAt + 0.08f;
        ScadrykAudio.Play(ScadrykCue.Dash);
        if (animator != null) animator.PlayDash(dashDirection);
        ScadrykEffects.Burst(transform.position, new Color(0.55f, 0.85f, 1f, 0.75f), 0.75f);
    }

    private void Shockwave()
    {
        if (Time.time < nextShockwaveAt) return;
        nextShockwaveAt = Time.time + shockwaveCooldown;
        Vector2 direction = facing;
        if (animator != null) animator.PlayWindup(direction, 0.08f);
        StartCoroutine(ResolveShockwave(direction));
    }

    private IEnumerator ResolveShockwave(Vector2 direction)
    {
        yield return new WaitForSeconds(0.08f);
        if (isDying || (director != null && director.IsPaused)) yield break;
        if (director != null) director.TryPulseAnchor(transform.position);
        ScadrykAudio.Play(ScadrykCue.Pulse);
        if (animator != null) animator.PlayAttack(direction, true);
        ScadrykEffects.PlayerPulse(transform.position, shockwaveRadius);
        ShakeCamera(0.055f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius, ~0);
        foreach (Collider2D hit in hits)
            if (hit.TryGetComponent<ScadrykEnemyHealth>(out var enemy)) enemy.TakeDamage(shockwaveDamage);
    }

    private void UseAbility(int index)
    {
        if (!HasAbility(index))
        {
            if (director != null) director.ShowAbilityLocked(index);
            return;
        }
        if (Time.time < abilityReadyAt[index]) return;

        if (director != null) director.RecordPowerUse(index);

        float radius;
        float damage;
        float cooldown;
        if (index == 0)
        {
            radius = 3.6f; damage = 72f; cooldown = 7f;
        }
        else if (index == 1)
        {
            radius = 4.25f; damage = 82f; cooldown = 10f;
        }
        else
        {
            radius = 7.6f; damage = 120f; cooldown = 14f;
        }

        abilityReadyAt[index] = Time.time + cooldown;
        ScadrykAudio.Play(ScadrykCue.Power);
        Vector2 direction = facing;
        if (animator != null) animator.PlayWindup(direction, AbilityWindupFor(index));
        StartCoroutine(ResolveAbility(index, direction, radius, damage));
    }

    private IEnumerator ResolveAbility(int index, Vector2 direction, float radius, float damage)
    {
        yield return new WaitForSeconds(AbilityWindupFor(index));
        if (isDying || (director != null && director.IsPaused)) yield break;
        Vector2 origin = transform.position;
        if (director != null) director.TryRevealSecret(index, origin);
        if (index == 0) ScadrykEffects.AbilityCrescent(origin, direction);
        else if (index == 1) ScadrykEffects.AbilitySaltburst(origin, radius * 2.1f);
        else ScadrykEffects.AbilityTidebreaker(origin, direction);
        ScadrykAudio.Play(ScadrykCue.PlayerPowerImpact);
        if (animator != null) animator.PlayAbilityImpact(direction, AbilityRecoveryFor(index));
        ShakeCamera(index == 2 ? 0.12f : 0.075f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, ~0);
        foreach (Collider2D hit in hits)
        {
            if (!hit.TryGetComponent<ScadrykEnemyHealth>(out var enemy)) continue;
            Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
            float enemyRadius = EnemyCollisionRadius(enemy);
            if ((index == 0 || index == 2) && !AbilityFootprintContainsPoint(index, enemy.transform.position, origin, direction, enemyRadius)) continue;
            if (AbilityPathIsBlocked(index, origin, enemy.transform.position)) continue;
            enemy.TakeDamage(damage);
            if (index == 0) enemy.ApplyCrescentControl();
            else if (index == 1) enemy.ApplySaltExposure();
            else enemy.ApplyTidePush(toEnemy);
        }
    }

    private static float EnemyCollisionRadius(ScadrykEnemyHealth enemy)
    {
        CircleCollider2D collider = enemy != null ? enemy.GetComponent<CircleCollider2D>() : null;
        if (collider == null) return 0f;
        Vector3 scale = enemy.transform.lossyScale;
        return collider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
    }

    internal static bool WithinVisualFootprint(Vector2 target, Vector2 origin, Vector2 direction, float forwardCenter, Vector2 size, float targetRadius = 0f)
    {
        Vector2 forward = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
        Vector2 offset = target - origin;
        float along = Vector2.Dot(offset, forward);
        float across = Mathf.Abs(offset.x * -forward.y + offset.y * forward.x);
        return Mathf.Abs(along - forwardCenter) <= size.x * 0.5f + targetRadius && across <= size.y * 0.5f + targetRadius;
    }

    internal bool AbilityFootprintContainsPoint(int index, Vector2 target, Vector2 origin, float targetRadius = 0f)
        => AbilityFootprintContainsPoint(index, target, origin, facing, targetRadius);

    internal static bool AbilityFootprintContainsPoint(int index, Vector2 target, Vector2 origin, Vector2 direction, float targetRadius = 0f)
    {
        if (index == 0) return WithinVisualFootprint(target, origin, direction, 1.05f, new Vector2(4.1f, 2.8f), targetRadius);
        if (index == 1) return Vector2.Distance(target, origin) <= 4.25f + targetRadius;
        if (index == 2) return WithinVisualFootprint(target, origin, direction, 2.7f, new Vector2(7.4f, 4.4f), targetRadius);
        return false;
    }

    internal static bool AbilityPathIsBlocked(int index, Vector2 origin, Vector2 target) =>
        (index == 0 || index == 2) && ScadrykCombatRules.WorldBlocks(origin, target);

    public void TakeDamage(float amount)
    {
        if (isDying || Time.time < invulnerableUntil || (director != null && director.IsPaused)) return;
        health -= Mathf.Max(0f, amount);
        ScadrykAudio.Play(ScadrykCue.Hurt);
        invulnerableUntil = Time.time + 0.62f;
        if (animator != null) animator.PlayHit(-facing);
        ShakeCamera(0.095f);
        ScadrykEffects.Burst(transform.position, new Color(1f, 0.27f, 0.2f, 0.7f), 0.8f);
        if (health <= 0f)
        {
            health = 0f;
            isDying = true;
            if (director != null) director.RecordPlayerDeath();
            BodyVelocity = Vector2.zero;
            if (animator != null) animator.PlayDeath();
            StartCoroutine(RespawnAfterDeath());
        }
    }

    private IEnumerator RespawnAfterDeath()
    {
        yield return new WaitForSeconds(0.76f);
        if (director != null) director.RestartCurrentRoom();
    }

    private static void ShakeCamera(float strength)
    {
        if (ScadrykSettings.ReduceShake) return;
        Camera camera = Camera.main;
        if (camera == null) return;
        ScadrykCameraFollow follow = camera.GetComponent<ScadrykCameraFollow>();
        if (follow != null) follow.Shake(strength, 0.24f);
    }

    public void RestoreHealth(float amount) => health = Mathf.Min(maxHealth, health + Mathf.Max(0f, amount));
    public void ResetAbilities()
    {
        System.Array.Clear(learnedAbilities, 0, learnedAbilities.Length);
        System.Array.Clear(abilityReadyAt, 0, abilityReadyAt.Length);
    }
    public void ResetCombatCooldowns()
    {
        dashEndsAt = nextDashAt = nextAttackAt = nextShockwaveAt = 0f;
        dashBufferedUntil = attackBufferedUntil = 0f;
        lastBasicAttackStep = -1;
        basicAttackComboUntil = 0f;
        System.Array.Clear(abilityReadyAt, 0, abilityReadyAt.Length);
    }
    public void ApplyRootSlow(float duration) => rootedUntil = Mathf.Max(rootedUntil, Time.time + duration);
    public void SetPortalTransitPose(Vector2 position, float visibility)
    {
        BodyVelocity = Vector2.zero;
        body.position = position;
        if (animator != null) animator.SetPortalScale(visibility);
    }
    public void ResetForRoom(Vector2 position) { health = maxHealth; rootedUntil = 0f; dashBufferedUntil = attackBufferedUntil = 0f; lastBasicAttackStep = -1; basicAttackComboUntil = 0f; terrainZones.Clear(); terrainSpeedScale = 1f; isDying = false; invulnerableUntil = Time.time + 1f; transform.position = position; BodyVelocity = Vector2.zero; if (animator != null) { animator.SetPortalScale(1f); animator.ResetPose(); } }
}
