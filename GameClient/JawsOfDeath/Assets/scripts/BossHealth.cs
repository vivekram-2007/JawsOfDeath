using UnityEngine;

public class BossHealth : MonoBehaviour
{
    public string bossName = "ZOMBIE BOSS";
    public int maxHealth = 400;
    [Range(0f, 1f)] public float healFraction = 0.5f;

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private BossAI ai;
    private float damageSinceRest;
    private float healRemaining;
    private float healPerSecond;

    void Start()
    {
        CurrentHealth = maxHealth;
        ai = GetComponent<BossAI>();
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
        if (ai != null && !ai.enabled) return;          // asleep: ignore shots
        if (ai != null && !ai.RegisterShot()) return;   // not resting: he teleports, damage only 1 in 4

        CurrentHealth -= amount;
        damageSinceRest += amount;

        if (CurrentHealth <= 0f)
        {
            CurrentHealth = 0f;
            IsDead = true;
            Destroy(gameObject);
        }
    }

    public void BeginRest(float duration)
    {
        healRemaining = damageSinceRest * healFraction;
        healPerSecond = duration > 0f ? healRemaining / duration : healRemaining;
        damageSinceRest = 0f;
    }

    public void ResetHealth()
    {
        CurrentHealth = maxHealth;
        damageSinceRest = 0f;
        healRemaining = 0f;
        healPerSecond = 0f;
    }
}