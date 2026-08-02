using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    public Camera playerCamera;
    public float range = 100f;
    public int damage = 25;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, range))
        {
            ZombieHealth zombie = hit.transform.GetComponent<ZombieHealth>();
            if (zombie != null)
            {
                zombie.TakeDamage(damage);
            }
        }
    }
}