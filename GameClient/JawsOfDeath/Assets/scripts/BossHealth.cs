using UnityEngine;

public class BossHealth : MonoBehaviour
{
    public string bossName = "ZOMBIE BOSS";
    public int maxHealth = 400;
    [Range(0f, 1f)] public float healFraction = 0.5f;   // heals this share of damage taken since the last rest

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private float damageSinceRest;
    private float healRemaining;
    private float healPerSecond;

    void Start()
    {
        CurrentHealth = maxHealth;
    }

    void Update()
    {
        if (IsDead || healRemaining <= 0f) return;

        float step = Mathf.Min(healPerSecond * Time.deltaTime, healRemaining);
        CurrentHealth = Mathf.Min(CurrentHealth + step, maxHealth);
        healRemaining -= step;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHealth -= amount;
        damageSinceRest += amount;

        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            IsDead = true;
            Destroy(gameObject);   // no death animation yet
        }
    }

    // BossAI calls this when a rest starts
    public void BeginRest(float duration)
    {
        healRemaining = damageSinceRest * healFraction;
        healPerSecond = duration > 0f ? healRemaining / duration : healRemaining;
        damageSinceRest = 0f;
    }
}