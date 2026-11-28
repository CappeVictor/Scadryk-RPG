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
    private float dashEndsAt, nextDashAt, nextAttackAt, nextShockwaveAt, dashBufferedUntil, attackBufferedUntil, pulseBufferedUntil;
    private float health = 120f;
    private float maxHealth = 120f;
    private float aimLockedUntil;
    private int lastBasicAttackStep = -1;
    private float basicAttackComboUntil;
    private bool isDying;
    private float invulnerableUntil;
    private float rootedUntil;
    private float saltCrustUntil;
    private float impactKnockbackUntil;
    private Vector2 impactKnockbackVelocity;
    private readonly bool[] learnedAbilities = new bool[3];
    private readonly bool[] powerEchoes = new bool[3];
    private readonly float[] abilityReadyAt = new float[3];
    private readonly float[] abilityBufferedUntil = new float[3];
    private readonly List<Collider2D> attackHits = new List<Collider2D>(16);
    private ContactFilter2D attackHitFilter;
    private SpriteRenderer sprite;
    private ScadrykActorAnimator animator;
    private ScadrykGameDirector director;
    private Sprite[] spriteFrames;
    private const float BasicAttackWindup = 0.12f;
    private const int BasicAttackComboLength = 3;
    internal const float BasicAttackComboWindow = 0.82f;
    internal const float BasicAttackBufferWindow = 0.2f;
    internal const float AbilityBufferWindow = 0.18f;
    internal const float ShockwaveBufferWindow = 0.18f;
    internal const float DashBufferWindow = 0.12f;
    internal const float DashPostInvulnerabilityGrace = 0.08f;
    internal const float ShockwaveWindup = 0.08f;
    internal const float ShockwaveRecovery = 0.28f;
    internal const float SaltburstAbilityRadius = 4.25f;
    internal const float SaltCrustSlowScale = 0.74f;
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
    private Vector2 terrainFlowVelocity;

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

    public void UnlockPowerEcho(int index)
    {
        if (index < 0 || index >= powerEchoes.Length) return;
        powerEchoes[index] = true;
    }

    public bool HasPowerEcho(int index) => index >= 0 && index < powerEchoes.Length && powerEchoes[index];

    internal static float AbilityCooldownFor(int index, bool echo) => Mathf.Clamp(index, 0, 2) switch
    {
        0 => echo ? 5.6f : 7f,
        1 => 10f,
        _ => 14f
    };

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        attackHitFilter = new ContactFilter2D();
        attackHitFilter.SetLayerMask(~0);
        attackHitFilter.useTriggers = Physics2D.queriesHitTriggers;
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
            dashBufferedUntil = attackBufferedUntil = pulseBufferedUntil = 0f;
            System.Array.Clear(abilityBufferedUntil, 0, abilityBufferedUntil.Length);
            return;
        }

        Vector2 keyboard = new Vector2((ScadrykSettings.Held(10) ? 1f : 0f) - (ScadrykSettings.Held(9) ? 1f : 0f),
            (ScadrykSettings.Held(7) ? 1f : 0f) - (ScadrykSettings.Held(8) ? 1f : 0f));
        Vector2 stick = ScadrykSettings.ReadGamepadStick("Scadryk Move X", "Scadryk Move Y");
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
        if (keyboardPulse || mousePulse || gamepadPulse) pulseBufferedUntil = Time.time + ShockwaveBufferWindow;
        TryStartBufferedShockwave();
        if (keyboardCrescent || gamepadCrescent) UseAbility(0);
        if (keyboardSaltburst || gamepadSaltburst) UseAbility(1);
        if (keyboardTidebreaker || gamepadTidebreaker) UseAbility(2);
        for (int ability = 0; ability < abilityBufferedUntil.Length; ability++) TryStartBufferedAbility(ability);
        if ((keyboardInteract || gamepadInteract) && director != null) director.TryInteract(transform.position);

        UpdateSprite();
    }

    private void FixedUpdate()
    {
        if (terrainZones.RemoveWhere(zone => zone == null) > 0) RefreshTerrainSpeed();
        bool paused = director != null && director.IsPaused;
        if (!CanMeasureTerrainExposure(isDying, paused)) { BodyVelocity = Vector2.zero; return; }
        if (director != null)
        {
            if (IsInCoastalCurrent()) director.RecordCoastalCurrentExposure(Time.fixedDeltaTime);
            if (IsInShallowWater()) director.RecordShallowWaterExposure(Time.fixedDeltaTime);
            if (IsInSaltCrosswind()) director.RecordSaltCrosswindExposure(Time.fixedDeltaTime);
        }
        if (Time.time < dashEndsAt)
        {
            BodyVelocity = dashDirection * dashSpeed;
            return;
        }
        if (Time.time < impactKnockbackUntil)
        {
            BodyVelocity = impactKnockbackVelocity;
            return;
        }
        float rootScale = Time.time < rootedUntil ? 0.52f : Time.time < saltCrustUntil ? SaltCrustSlowScale : 1f;
        Vector2 targetVelocity = ResolveTerrainTargetVelocity(moveInput, moveSpeed, terrainSpeedScale, terrainFlowVelocity, rootScale);
        BodyVelocity = Vector2.MoveTowards(BodyVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<ScadrykTerrainZone>(out var zone) || !terrainZones.Add(zone)) return;
        RefreshTerrainSpeed();
        if (director != null) director.ShowTerrainHint(zone.HintKey, zone.EntryHint);
        if (director != null && zone.HintKey == ScadrykTerrainZone.CoastalCurrentKey) director.RecordCoastalCurrentEntry();
        if (zone.EntryCue.HasValue)
        {
            ScadrykAudio.Play(zone.EntryCue.Value);
            zone.ConsumeEntryCue();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent<ScadrykTerrainZone>(out var zone) || !terrainZones.Remove(zone)) return;
        RefreshTerrainSpeed();
    }

    private void RefreshTerrainSpeed()
    {
        terrainSpeedScale = 1f;
        terrainFlowVelocity = Vector2.zero;
        foreach (ScadrykTerrainZone zone in terrainZones)
        {
            if (zone == null) continue;
            terrainSpeedScale = Mathf.Min(terrainSpeedScale, zone.MovementScale);
            terrainFlowVelocity = ScadrykTerrainZone.CombineFlow(terrainFlowVelocity, zone.FlowVelocity);
        }
    }

    private bool IsInCoastalCurrent()
    {
        foreach (ScadrykTerrainZone zone in terrainZones)
            if (zone != null && zone.HintKey == ScadrykTerrainZone.CoastalCurrentKey) return true;
        return false;
    }

    private bool IsInShallowWater()
    {
        foreach (ScadrykTerrainZone zone in terrainZones)
            if (zone != null && zone.HintKey == ScadrykTerrainZone.ShallowWaterKey) return true;
        return false;
    }

    private bool IsInSaltCrosswind()
    {
        foreach (ScadrykTerrainZone zone in terrainZones)
            if (zone != null && zone.HintKey == ScadrykTerrainZone.SaltDriftKey) return true;
        return false;
    }

    internal static Vector2 ResolveTerrainTargetVelocity(Vector2 input, float speed, float terrainScale, Vector2 flow, float rootedScale = 1f) =>
        Vector2.ClampMagnitude(input, 1f) * (Mathf.Max(0f, speed) * Mathf.Clamp(terrainScale, 0.4f, 1f) * Mathf.Clamp(rootedScale, 0f, 1f)) +
        Vector2.ClampMagnitude(flow, ScadrykTerrainZone.MaximumFlowSpeed);

    internal static bool CanMeasureTerrainExposure(bool isDying, bool isPaused) => !isDying && !isPaused;

    private void AimAtPointer(bool keyboardMouseInput, bool gamepadInput)
    {
        if (!CanUpdateAim(Time.time, aimLockedUntil)) return;
        if (Input.mousePosition != lastMousePosition)
        {
            keyboardMouseInput = true;
            lastMousePosition = Input.mousePosition;
        }
        Vector2 stick = ScadrykSettings.ReadGamepadStick("Scadryk Aim X", "Scadryk Aim Y");
        stick.y = -stick.y;
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
                ScadrykSettings.ReadGamepadStick("Scadryk Move X", "Scadryk Move Y"), hasIndependentGamepadAim);
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
        bool invulnerableTint = Time.time < invulnerableUntil && (ScadrykSettings.ReduceFlash || Mathf.FloorToInt(Time.time * 18f) % 2 != 0);
        sprite.color = StatusTint(invulnerableTint, Time.time < rootedUntil, Time.time < saltCrustUntil, Time.time < impactKnockbackUntil);
    }

    internal static Color StatusTint(bool invulnerable, bool rooted, bool saltCrusted, bool knockedBack)
    {
        if (invulnerable) return new Color(0.45f, 0.85f, 1f);
        if (rooted) return new Color(0.72f, 0.92f, 0.56f);
        if (saltCrusted) return new Color(1f, 0.82f, 0.46f);
        if (knockedBack) return new Color(0.42f, 0.88f, 0.94f);
        return Color.white;
    }

    internal static bool InputBufferCanStart(float now, float bufferedUntil, float cooldownEndsAt) =>
        bufferedUntil > now && now >= cooldownEndsAt;
    internal static bool CanUpdateAim(float now, float lockUntil) => now >= lockUntil;
    internal static bool DashBufferCanStart(float now, float bufferedUntil, float cooldownEndsAt) =>
        InputBufferCanStart(now, bufferedUntil, cooldownEndsAt);
    internal static float DashInvulnerabilityUntil(float currentUntil, float dashEndsAt, float graceDuration) =>
        Mathf.Max(currentUntil, dashEndsAt + Mathf.Max(0f, graceDuration));

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
        aimLockedUntil = Time.time + windupForStep + 0.04f;
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
        int hitCount = CollectAttackHits(transform.position, footprint.x + footprint.y);
        bool hitEnemy = false;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = attackHits[i];
            if (hit == null) continue;
            if (!hit.TryGetComponent<ScadrykEnemyHealth>(out var enemy)) continue;
            if (!WithinVisualFootprint(enemy.transform.position, transform.position, direction, ScadrykEffects.PlayerSlashForwardOffset, footprint, EnemyCollisionRadius(enemy))) continue;
            if (ScadrykCombatRules.WorldBlocks(transform.position, enemy.transform.position)) continue;
            enemy.TakeDamage(damage);
            if (comboStep == BasicAttackComboLength - 1) enemy.ApplyBasicFinisherStagger(transform.position, direction);
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
        invulnerableUntil = DashInvulnerabilityUntil(invulnerableUntil, dashEndsAt, DashPostInvulnerabilityGrace);
        ScadrykAudio.Play(ScadrykCue.Dash);
        if (animator != null) animator.PlayDash(dashDirection);
        ScadrykEffects.Burst(transform.position, new Color(0.55f, 0.85f, 1f, 0.75f), 0.75f);
    }

    private void TryStartBufferedShockwave()
    {
        if (!InputBufferCanStart(Time.time, pulseBufferedUntil, nextShockwaveAt))
        {
            if (Time.time >= pulseBufferedUntil) pulseBufferedUntil = 0f;
            return;
        }
        pulseBufferedUntil = 0f;
        nextShockwaveAt = Time.time + shockwaveCooldown;
        Vector2 direction = facing;
        aimLockedUntil = Time.time + ShockwaveWindup + 0.04f;
        if (animator != null) animator.PlayWindup(direction, ShockwaveWindup);
        StartCoroutine(ResolveShockwave(direction));
    }

    private IEnumerator ResolveShockwave(Vector2 direction)
    {
        yield return new WaitForSeconds(ShockwaveWindup);
        if (isDying || (director != null && director.IsPaused)) yield break;
        if (director != null) director.TryPulseAnchor(transform.position);
        ScadrykAudio.Play(ScadrykCue.Pulse);
        if (animator != null) animator.PlayAbilityImpact(direction, ShockwaveRecovery);
        ScadrykEffects.PlayerPulse(transform.position, shockwaveRadius);
        ShakeCamera(0.055f);
        int hitCount = CollectAttackHits(transform.position, shockwaveRadius);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = attackHits[i];
            if (hit != null && hit.TryGetComponent<ScadrykEnemyHealth>(out var enemy)) enemy.TakeDamage(shockwaveDamage);
        }
    }

    private void UseAbility(int index)
    {
        if (!HasAbility(index))
        {
            if (director != null) director.ShowAbilityLocked(index);
            return;
        }
        abilityBufferedUntil[index] = Time.time + AbilityBufferWindow;
        TryStartBufferedAbility(index);
    }

    private void TryStartBufferedAbility(int index)
    {
        if (index < 0 || index >= abilityBufferedUntil.Length) return;
        if (!InputBufferCanStart(Time.time, abilityBufferedUntil[index], abilityReadyAt[index]))
        {
            if (Time.time >= abilityBufferedUntil[index]) abilityBufferedUntil[index] = 0f;
            return;
        }
        abilityBufferedUntil[index] = 0f;

        if (director != null) director.RecordPowerUse(index);

        float radius;
        float damage;
        float cooldown = AbilityCooldownFor(index, HasPowerEcho(index));
        if (index == 0)
        {
            radius = 3.6f; damage = 72f;
        }
        else if (index == 1)
        {
            radius = SaltburstAbilityRadius; damage = 82f;
        }
        else
        {
            radius = 7.6f; damage = 120f;
        }

        abilityReadyAt[index] = Time.time + cooldown;
        ScadrykAudio.Play(ScadrykCue.Power);
        Vector2 direction = facing;
        aimLockedUntil = Time.time + AbilityWindupFor(index) + 0.04f;
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
        else if (index == 1) ScadrykEffects.AbilitySaltburst(origin, ScadrykEffects.AbilitySaltburstVisualDiameter(radius));
        else ScadrykEffects.AbilityTidebreaker(origin, direction);
        ScadrykAudio.Play(ScadrykAudio.AbilityImpactCue(index));
        if (animator != null) animator.PlayAbilityImpact(direction, AbilityRecoveryFor(index));
        ShakeCamera(index == 2 ? 0.12f : 0.075f);
        int hitCount = CollectAttackHits(origin, radius);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = attackHits[i];
            if (hit == null) continue;
            if (!hit.TryGetComponent<ScadrykEnemyHealth>(out var enemy)) continue;
            Vector2 toEnemy = (Vector2)enemy.transform.position - origin;
            float enemyRadius = EnemyCollisionRadius(enemy);
            if ((index == 0 || index == 2) && !AbilityFootprintContainsPoint(index, enemy.transform.position, origin, direction, enemyRadius)) continue;
            if (AbilityPathIsBlocked(index, origin, enemy.transform.position)) continue;
            enemy.TakeDamage(damage);
            if (index == 0) enemy.ApplyCrescentControl();
            else if (index == 1) enemy.ApplySaltExposure(HasPowerEcho(index));
            else enemy.ApplyTidePush(toEnemy, HasPowerEcho(index));
        }
    }

    private int CollectAttackHits(Vector2 origin, float radius)
    {
        attackHits.Clear();
        return Physics2D.OverlapCircle(origin, radius, attackHitFilter, attackHits);
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
        if (index == 1) return Vector2.Distance(target, origin) <= SaltburstAbilityRadius + targetRadius;
        if (index == 2) return WithinVisualFootprint(target, origin, direction, 2.7f, new Vector2(7.4f, 4.4f), targetRadius);
        return false;
    }

    internal static bool AbilityPathIsBlocked(int index, Vector2 origin, Vector2 target) =>
        (index == 0 || index == 2) && ScadrykCombatRules.WorldBlocks(origin, target);

    public bool TakeDamage(float amount)
    {
        if (!ScadrykCombatRules.IsValidDamageAmount(amount) || isDying || Time.time < invulnerableUntil ||
            (director != null && director.IsPaused)) return false;
        health -= amount;
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
        return true;
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
        System.Array.Clear(abilityBufferedUntil, 0, abilityBufferedUntil.Length);
        aimLockedUntil = 0f;
    }
    public void ResetCombatCooldowns()
    {
        dashEndsAt = nextDashAt = nextAttackAt = nextShockwaveAt = 0f;
        dashBufferedUntil = attackBufferedUntil = pulseBufferedUntil = 0f;
        lastBasicAttackStep = -1;
        basicAttackComboUntil = 0f;
        System.Array.Clear(abilityReadyAt, 0, abilityReadyAt.Length);
        System.Array.Clear(abilityBufferedUntil, 0, abilityBufferedUntil.Length);
        aimLockedUntil = 0f;
    }
    public void ApplyRootSlow(float duration) => rootedUntil = Mathf.Max(rootedUntil, Time.time + duration);
    public void ApplySaltCrustSlow(float duration) => saltCrustUntil = Mathf.Max(saltCrustUntil, Time.time + duration);
    internal static Vector2 KnockbackVelocityFrom(Vector2 target, Vector2 source, float strength)
    {
        Vector2 away = target - source;
        if (away.sqrMagnitude < 0.001f) away = Vector2.up;
        return away.normalized * Mathf.Max(0f, strength);
    }
    public void ApplyImpactKnockback(Vector2 source, float strength, float duration)
    {
        if (isDying || (director != null && director.IsPaused)) return;
        impactKnockbackVelocity = KnockbackVelocityFrom(transform.position, source, strength);
        impactKnockbackUntil = Mathf.Max(impactKnockbackUntil, Time.time + Mathf.Max(0f, duration));
    }
    public void SetPortalTransitPose(Vector2 position, float visibility)
    {
        BodyVelocity = Vector2.zero;
        body.position = position;
        if (animator != null) animator.SetPortalScale(visibility);
    }
    public void ResetForRoom(Vector2 position)
    {
        health = maxHealth;
        rootedUntil = 0f;
        saltCrustUntil = impactKnockbackUntil = 0f;
        impactKnockbackVelocity = Vector2.zero;
        dashBufferedUntil = attackBufferedUntil = pulseBufferedUntil = 0f;
        System.Array.Clear(abilityBufferedUntil, 0, abilityBufferedUntil.Length);
        aimLockedUntil = 0f;
        facing = Vector2.right;
        dashDirection = Vector2.zero;
        lastBasicAttackStep = -1;
        basicAttackComboUntil = 0f;
        terrainZones.Clear();
        terrainSpeedScale = 1f;
        terrainFlowVelocity = Vector2.zero;
        isDying = false;
        invulnerableUntil = Time.time + 1f;
        transform.position = position;
        BodyVelocity = Vector2.zero;
        if (animator != null)
        {
            animator.SetPortalScale(1f);
            animator.ResetPose();
        }
    }
}
