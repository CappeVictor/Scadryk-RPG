using System.Collections;
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
    private float dashEndsAt, nextDashAt, nextAttackAt, nextShockwaveAt;
    private float health = 120f;
    private float maxHealth = 120f;
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
    private Vector3 lastMousePosition;
    private bool usingGamepadAim;

    public Vector2 Facing => facing;
    public float Health => health;
    public float MaxHealth => maxHealth;
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
        if (isDying || (director != null && director.IsPaused)) { moveInput = Vector2.zero; return; }

        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        AimAtPointer();
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || ScadrykSettings.Down(0) || Input.GetKeyDown(KeyCode.JoystickButton0)) Attack();
        if (ScadrykSettings.Down(1) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.JoystickButton1)) Dash();
        if (ScadrykSettings.Down(2) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.JoystickButton2)) Shockwave();
        if (ScadrykSettings.Down(3) || Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.JoystickButton4)) UseAbility(0);
        if (ScadrykSettings.Down(4) || Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.JoystickButton5)) UseAbility(1);
        if (ScadrykSettings.Down(5) || Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.JoystickButton6)) UseAbility(2);
        if ((ScadrykSettings.Down(6) || Input.GetKeyDown(KeyCode.JoystickButton3)) && director != null) director.TryInteract(transform.position);

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
        float effectiveSpeed = Time.time < rootedUntil ? moveSpeed * 0.52f : moveSpeed;
        BodyVelocity = Vector2.MoveTowards(BodyVelocity, moveInput * effectiveSpeed, acceleration * Time.fixedDeltaTime);
    }

    private void AimAtPointer()
    {
        Vector2 stick = new Vector2(Input.GetAxisRaw("Scadryk Aim X"), -Input.GetAxisRaw("Scadryk Aim Y"));
        if (stick.sqrMagnitude > 0.09f)
        {
            facing = stick.normalized;
            usingGamepadAim = true;
            return;
        }
        if (Input.mousePosition != lastMousePosition)
        {
            usingGamepadAim = false;
            lastMousePosition = Input.mousePosition;
        }
        if (usingGamepadAim) return;
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 pointer = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 aim = (Vector2)pointer - body.position;
        if (aim.sqrMagnitude > 0.03f) facing = aim.normalized;
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

    private void Attack()
    {
        if (Time.time < nextAttackAt) return;
        nextAttackAt = Time.time + attackCooldown;
        Vector2 direction = facing;
        ScadrykAudio.Play(ScadrykCue.Sword);
        if (animator != null) animator.PlayAttack(direction, false);
        StartCoroutine(ResolveBasicAttack(direction));
    }

    private IEnumerator ResolveBasicAttack(Vector2 direction)
    {
        yield return new WaitForSeconds(BasicAttackWindup);
        if (isDying || (director != null && director.IsPaused)) yield break;
        ScadrykEffects.Slash(transform.position, direction, attackRange);
        Collider2D[] hits = Physics2D.OverlapCircleAll((Vector2)transform.position + direction * 0.75f, attackRange * 0.82f, ~0);
        foreach (Collider2D hit in hits)
        {
            if (!hit.TryGetComponent<ScadrykEnemyHealth>(out var enemy)) continue;
            Vector2 toEnemy = (Vector2)enemy.transform.position - (Vector2)transform.position;
            if (toEnemy.sqrMagnitude > 0.01f && Vector2.Dot(direction, toEnemy.normalized) < 0.15f) continue;
            enemy.TakeDamage(attackDamage);
        }
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
        if (director != null) director.TryPulseAnchor(transform.position);
        ScadrykAudio.Play(ScadrykCue.Pulse);
        if (animator != null) animator.PlayAttack(facing, true);
        ScadrykEffects.PlayerPulse(transform.position, shockwaveRadius * 2f);
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

        if (director != null)
        {
            director.RecordPowerUse(index);
            director.TryRevealSecret(index, transform.position);
        }

        float radius;
        float damage;
        float cooldown;
        if (index == 0)
        {
            radius = 3.6f; damage = 72f; cooldown = 7f;
            ScadrykEffects.AbilityCrescent(transform.position, facing);
        }
        else if (index == 1)
        {
            radius = 4.25f; damage = 82f; cooldown = 10f;
            ScadrykEffects.AbilitySaltburst(transform.position, radius * 2.1f);
        }
        else
        {
            radius = 7.6f; damage = 120f; cooldown = 14f;
            ScadrykEffects.AbilityTidebreaker(transform.position, facing);
        }

        abilityReadyAt[index] = Time.time + cooldown;
        ScadrykAudio.Play(ScadrykCue.Power);
        if (animator != null) animator.PlayAttack(facing, true);
        ShakeCamera(index == 2 ? 0.12f : 0.075f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, ~0);
        foreach (Collider2D hit in hits)
        {
            if (!hit.TryGetComponent<ScadrykEnemyHealth>(out var enemy)) continue;
            Vector2 toEnemy = (Vector2)enemy.transform.position - (Vector2)transform.position;
            if (index == 0 && (toEnemy.magnitude > radius || Vector2.Dot(facing, toEnemy.normalized) < 0.12f)) continue;
            if (index == 2 && (toEnemy.magnitude > radius || Vector2.Dot(facing, toEnemy.normalized) < 0.28f)) continue;
            enemy.TakeDamage(damage);
        }
    }

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
    public void ApplyRootSlow(float duration) => rootedUntil = Mathf.Max(rootedUntil, Time.time + duration);
    public void SetPortalTransitPose(Vector2 position, float visibility)
    {
        BodyVelocity = Vector2.zero;
        body.position = position;
        if (animator != null) animator.SetPortalScale(visibility);
    }
    public void ResetForRoom(Vector2 position) { health = maxHealth; rootedUntil = 0f; isDying = false; invulnerableUntil = Time.time + 1f; transform.position = position; BodyVelocity = Vector2.zero; if (animator != null) { animator.SetPortalScale(1f); animator.ResetPose(); } }
}
