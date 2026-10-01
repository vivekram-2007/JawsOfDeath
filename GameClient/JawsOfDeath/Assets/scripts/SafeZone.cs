using UnityEngine;

public class SafeZone : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ZombieAI.playerInSafeZone = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ZombieAI.playerInSafeZone = false;
        }
    }
}