using UnityEngine;

public class ZombieHealth : MonoBehaviour
{
    public int maxHealth = 50;
    public bool requiresFire = false;   // tick this on the boss
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (requiresFire && !OrbPickup.hasFire) return;

        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}