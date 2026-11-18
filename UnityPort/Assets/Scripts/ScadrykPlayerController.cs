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
    [SerializeField, Min(0f)] private float attackDamage = 32f;
    [SerializeField, Min(0.05f)] private float attackCooldown = 0.28f;
    [SerializeField, Min(0f)] private float shockwaveDamage = 48f;
    [SerializeField, Min(0.1f)] private float shockwaveRadius = 3.2f;
    [SerializeField, Min(0.1f)] private float shockwaveCooldown = 4f;

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
    private float health = 100f;
    private float maxHealth = 100f;
    private float invulnerableUntil;
    private float rootedUntil;
    private SpriteRenderer sprite;
    private ScadrykActorAnimator animator;
    private ScadrykGameDirector director;
    private Sprite[] spriteFrames;

    public Vector2 Facing => facing;
    public float Health => health;
    public float MaxHealth => maxHealth;
    public float Health01 => maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth);
    public float DashCooldownLeft => Mathf.Max(0f, nextDashAt - Time.time);
    public float ShockwaveCooldownLeft => Mathf.Max(0f, nextShockwaveAt - Time.time);

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        sprite = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponent<ScadrykActorAnimator>();
        director = FindAnyObjectByType<ScadrykGameDirector>();
    }

    private void Update()
    {
        if (director != null && director.IsPaused) { moveInput = Vector2.zero; return; }

        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        AimAtPointer();
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J)) Attack();
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.K)) Dash();
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1)) Shockwave();
        if (Input.GetKeyDown(KeyCode.E) && director != null) director.TryUseExit(transform.position);

        UpdateSprite();
    }

    private void FixedUpdate()
    {
        if (director != null && director.IsPaused) { BodyVelocity = Vector2.zero; return; }
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
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 pointer = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 aim = (Vector2)pointer - body.position;
        if (aim.sqrMagnitude > 0.03f) facing = aim.normalized;
    }

    public void SetSpriteFrames(Sprite[] frames) { spriteFrames = frames; UpdateSprite(); }

    private void UpdateSprite()
    {
        if (sprite == null) return;
        if (spriteFrames != null && spriteFrames.Length >= 16)
        {
            int row = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) ? (facing.x < 0f ? 1 : 2) : (facing.y < 0f ? 0 : 3);
            int column = moveInput.sqrMagnitude > 0.02f ? Mathf.FloorToInt(Time.time * 8f) % 4 : 0;
            sprite.sprite = spriteFrames[row * 4 + column];
        }
        sprite.color = Time.time < invulnerableUntil && Mathf.FloorToInt(Time.time * 18f) % 2 != 0 ? new Color(0.45f, 0.85f, 1f) : Time.time < rootedUntil ? new Color(0.72f, 0.92f, 0.56f) : Color.white;
    }

    private void Attack()
    {
        if (Time.time < nextAttackAt) return;
        nextAttackAt = Time.time + attackCooldown;
        if (animator != null) animator.PlayAttack(facing, false);
        ScadrykEffects.Slash(transform.position, facing, attackRange);
        Collider2D[] hits = Physics2D.OverlapCircleAll((Vector2)transform.position + facing * 0.8f, attackRange, ~0);
        foreach (Collider2D hit in hits)
            if (hit.TryGetComponent<ScadrykEnemyHealth>(out var enemy)) enemy.TakeDamage(attackDamage);
    }

    private void Dash()
    {
        if (Time.time < nextDashAt) return;
        dashDirection = moveInput.sqrMagnitude > 0.01f ? moveInput : facing;
        dashEndsAt = Time.time + dashDuration;
        nextDashAt = Time.time + dashCooldown;
        invulnerableUntil = dashEndsAt + 0.08f;
        ScadrykEffects.Burst(transform.position, new Color(0.55f, 0.85f, 1f, 0.75f), 0.75f);
    }

    private void Shockwave()
    {
        if (Time.time < nextShockwaveAt) return;
        nextShockwaveAt = Time.time + shockwaveCooldown;
        if (animator != null) animator.PlayAttack(facing, true);
        ScadrykEffects.PlayerPulse(transform.position, shockwaveRadius * 2f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius, ~0);
        foreach (Collider2D hit in hits)
            if (hit.TryGetComponent<ScadrykEnemyHealth>(out var enemy)) enemy.TakeDamage(shockwaveDamage);
    }

    public void TakeDamage(float amount)
    {
        if (Time.time < invulnerableUntil || (director != null && director.IsPaused)) return;
        health -= Mathf.Max(0f, amount);
        ScadrykEffects.Burst(transform.position, new Color(1f, 0.27f, 0.2f, 0.7f), 0.8f);
        if (health <= 0f)
        {
            health = maxHealth;
            if (director != null) director.RestartCurrentRoom();
        }
    }

    public void RestoreHealth(float amount) => health = Mathf.Min(maxHealth, health + Mathf.Max(0f, amount));
    public void ApplyRootSlow(float duration) => rootedUntil = Mathf.Max(rootedUntil, Time.time + duration);
    public void ResetForRoom(Vector2 position) { health = maxHealth; rootedUntil = 0f; transform.position = position; BodyVelocity = Vector2.zero; }
}
