using UnityEngine;

public class LavaDamage : MonoBehaviour
{
    public float damagePerSecond = 20f;
    public float surfaceOffset = 0.1f;

    Transform player;
    PlayerHealth health;
    Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            health = p.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        if (player == null || health == null || rend == null) return;

        Vector3 pos = player.position;
        Bounds b = rend.bounds;

        bool inLava = pos.x > b.min.x && pos.x < b.max.x
                   && pos.z > b.min.z && pos.z < b.max.z
                   && pos.y < b.max.y + surfaceOffset;

        if (inLava)
            health.TakeDamage(damagePerSecond * Time.deltaTime);
    }
}