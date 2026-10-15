using UnityEngine;

public class BossAI : MonoBehaviour
{
    public Transform pointsParent;          // drag BossPoints here
    public float moveInterval = 4f;         // seconds between teleports (test only)
    public float rayStartHeight = 100f;     // how far above the point the ground check starts
    public float footOffset = 0f;           // raise or lower him if his feet sink or float
    public float turnSpeed = 6f;

    private Transform[] points;
    private Transform player;
    private float timer;
    private int lastIndex = -1;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        int n = pointsParent.childCount;
        points = new Transform[n];
        for (int i = 0; i < n; i++) points[i] = pointsParent.GetChild(i);

        TeleportToRandomPoint();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= moveInterval)
        {
            timer = 0f;
            TeleportToRandomPoint();
        }

        if (player != null)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
            }
        }
    }

    void TeleportToRandomPoint()
    {
        if (points == null || points.Length == 0) return;

        int i;
        do { i = Random.Range(0, points.Length); }
        while (points.Length > 1 && i == lastIndex);
        lastIndex = i;

        transform.position = SnapToGround(points[i].position);
    }

    Vector3 SnapToGround(Vector3 pos)
    {
        Vector3 start = new Vector3(pos.x, pos.y + rayStartHeight, pos.z);
        RaycastHit[] hits = Physics.RaycastAll(start, Vector3.down, rayStartHeight * 3f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in hits)
        {
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            return new Vector3(pos.x, h.point.y + footOffset, pos.z);
        }
        return new Vector3(pos.x, transform.position.y, pos.z); // no ground found, keep current height
    }
}