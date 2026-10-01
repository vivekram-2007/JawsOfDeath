using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0) return;
        currentHealth -= amount;
        Debug.Log("Player health: " + currentHealth);
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log("Player died");
        }
    }
}