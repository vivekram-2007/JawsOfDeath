using UnityEngine;

public class Boulder : MonoBehaviour
{
    public float speed = 55f;
    public float damage = 34f;
    public float aimError = 0.4f;       // metres of random miss (lower = more accurate)
    [Range(0f, 1f)] public float leadAmount = 1f;   // 1 = full prediction of where you will be
    public float radius = 1.2f;         // hit size; keep it close to half the boulder's scale
    public float lifeTime = 6f;
    public float spinSpeed = 200f;

    private Vector3 direction;
    private bool launched;

    // BossAI calls this through SendMessage
    public void Launch(Vector3 target)
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            Vector3 vel = Vector3.zero;
            BossAI boss = FindFirstObjectByType<BossAI>();
            if (boss != null) vel = boss.PlayerVelocity;

            // iterate to find the intercept point
            Vector3 aim = target;
            for (int i = 0; i < 3; i++)
            {
                float t = Vector3.Distance(transform.position, aim) / speed;
                aim = target + vel * t * leadAmount;
            }
            target = aim;
        }

        target += Random.insideUnitSphere * aimError;
        direction = (target - transform.position).normalized;
        launched = true;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (!launched) return;

        float dist = speed * Time.deltaTime;
        transform.Rotate(Vector3.right, spinSpeed * Time.deltaTime, Space.Self);

        RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, direction, dist, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in hits)
        {
            if (h.collider.GetComponentInParent<BossHealth>() != null) continue;

            PlayerHealth ph = h.collider.GetComponentInParent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);

            Destroy(gameObject);
            return;
        }

        transform.position += direction * dist;
    }
}