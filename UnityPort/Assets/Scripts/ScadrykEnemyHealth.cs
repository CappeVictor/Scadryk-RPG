using UnityEngine;

/// <summary>Chasing enemy with a visible wind-up before each contact attack.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class ScadrykEnemyHealth : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 60f;
    [SerializeField, Min(0f)] private float touchDamage = 12f;
    [SerializeField, Min(0f)] private float moveSpeed = 2f;
    private float health, nextAttackAt, windupEndsAt;
    private float attackRange = 1.2f;
    private Rigidbody2D body;
    private SpriteRenderer sprite;
    private ScadrykPlayerController player;
    private ScadrykGameDirector director;
    private bool dead;
    private Sprite[] animationFrames;

    public float Health01 => maxHealth <= 0 ? 0 : Mathf.Clamp01(health / maxHealth);
    public bool IsBoss { get; private set; }

    public void Configure(float hp, float damage, float speed, bool boss, Sprite[] frames)
    {
        maxHealth = hp; health = hp; touchDamage = damage; moveSpeed = speed; IsBoss = boss;
        attackRange = boss ? 1.8f : 1.2f;
        animationFrames = frames;
        if (sprite != null && animationFrames != null && animationFrames.Length > 0) sprite.sprite = animationFrames[0];
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f; body.freezeRotation = true; body.interpolation = RigidbodyInterpolation2D.Interpolate;
        sprite = GetComponent<SpriteRenderer>();
        player = FindObjectOfType<ScadrykPlayerController>();
        director = FindObjectOfType<ScadrykGameDirector>();
        if (health <= 0f) health = maxHealth;
        GetComponent<CircleCollider2D>().isTrigger = false;
    }

    private void Update()
    {
        if (dead || player == null || (director != null && director.IsPaused)) { if (body != null) body.velocity = Vector2.zero; return; }
        Vector2 delta = (Vector2)player.transform.position - body.position;
        float distance = delta.magnitude;
        if (sprite != null)
        {
            bool windingUp = windupEndsAt > Time.time;
            sprite.color = windingUp ? new Color(1f, 0.68f, 0.24f) : Color.white;
            if (!windingUp && animationFrames != null && animationFrames.Length >= 4 && distance > 0.15f)
                sprite.sprite = animationFrames[Mathf.FloorToInt(Time.time * 6f) % animationFrames.Length];
        }
        if (windupEndsAt > 0f && Time.time >= windupEndsAt)
        {
            windupEndsAt = 0f;
            if (Vector2.Distance(transform.position, player.transform.position) <= attackRange + 0.35f)
                player.TakeDamage(touchDamage);
            nextAttackAt = Time.time + (IsBoss ? 1.25f : 1.55f);
        }
        if (distance > attackRange * 0.8f)
            body.velocity = delta.normalized * moveSpeed;
        else
        {
            body.velocity = Vector2.zero;
            if (windupEndsAt <= 0f && Time.time >= nextAttackAt) windupEndsAt = Time.time + (IsBoss ? 0.52f : 0.7f);
        }
    }

    public void TakeDamage(float amount)
    {
        if (dead) return;
        health -= Mathf.Max(0f, amount);
        if (health <= 0f)
        {
            dead = true;
            ScadrykEffects.Burst(transform.position, IsBoss ? new Color(1f, 0.54f, 0.22f, 0.9f) : new Color(0.55f, 0.84f, 0.8f, 0.8f), IsBoss ? 2f : 1.1f);
            if (director != null) director.OnEnemyDefeated(this);
            Destroy(gameObject);
        }
    }
}
