using UnityEngine;

public class BossAI : MonoBehaviour
{
    public Transform pointsParent;          // drag BossPoints here
    public float rayStartHeight = 100f;
    public float footOffset = 0f;
    public float turnSpeed = 8f;

    [Header("Running")]
    public float runSpeed = 12f;            // set to 2x the player's sprint speed
    public float runAtPlayerChance = 0.4f;  // chance a destination is the closest allowed point to you
    public float teleportMin = 6f;          // random teleport every min..max seconds while running
    public float teleportMax = 12f;
    public float minRunTime = 3f;           // run at least this long before he can stop to throw

    [Header("Point distances from player (m)")]
    public float tooClose = 20f;            // below this: never picked
    public float bestMin = 40f;             // bestMin..bestMax = best band
    public float bestMax = 75f;             // outside the best band (but above tooClose) = last resort

    [Header("Throwing")]
    public GameObject boulderPrefab;        // optional for now
    public float throwHeight = 3.5f;        // chest height where the boulder appears
    public int minBoulders = 6;
    public int maxBoulders = 8;
    public float throwInterval = 1.5f;
    public float offscreenTime = 0.5f;      // seconds you must not see him before he stops to throw

    [Header("Rest and shots")]
    public float restDuration = 5f;
    [Range(0f, 1f)] public float damageChanceWhenNotResting = 0.25f;

    public Vector3 PlayerVelocity { get { return playerVelocity; } }
    enum State { Run, Throw, Rest }
    State state = State.Run;

    Transform[] points;
    Transform player;
    BossHealth health;
    int lastIndex = -1;
    public int rememberPoints = 3;   // how many recent points he avoids
    readonly System.Collections.Generic.List<int> recent = new System.Collections.Generic.List<int>();
    Vector3 destination;
    bool hasDestination;
    float teleportTimer, runTime, hiddenTimer, throwTimer, restTimer;
    int toThrow, thrown;
    Vector3 lastPlayerPos, playerVelocity;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) { player = p.transform; lastPlayerPos = player.position; }
        health = GetComponent<BossHealth>();

        int n = pointsParent.childCount;
        points = new Transform[n];
        for (int i = 0; i < n; i++) points[i] = pointsParent.GetChild(i);

        TeleportToPoint(PickPoint(false));
        StartRun();
    }

    void Update()
    {
        if (player == null) return;

        float dt = Time.deltaTime;
        if (dt > 0f) playerVelocity = (player.position - lastPlayerPos) / dt;
        lastPlayerPos = player.position;

        switch (state)
        {
            case State.Run: UpdateRun(dt); break;
            case State.Throw: UpdateThrow(dt); break;
            case State.Rest: UpdateRest(dt); break;
        }
    }

    // ---------- states ----------

    void StartRun()
    {
        state = State.Run;
        runTime = 0f;
        hiddenTimer = 0f;
        hasDestination = false;
        teleportTimer = Random.Range(teleportMin, teleportMax);
    }

    void UpdateRun(float dt)
    {
        runTime += dt;

        teleportTimer -= dt;
        if (teleportTimer <= 0f)
        {
            TeleportToPoint(PickPoint(false));
            teleportTimer = Random.Range(teleportMin, teleportMax);
            hasDestination = false;
        }

        if (!hasDestination || Flat(destination - transform.position).magnitude < 1f)
            PickDestination();

        Vector3 dir = Flat(destination - transform.position);
        if (dir.sqrMagnitude > 0.01f)
        {
            Vector3 step = dir.normalized * runSpeed * dt;
            if (step.magnitude > dir.magnitude) step = dir;
            transform.position = SnapToGround(transform.position + step);
            FaceDirection(dir);
        }

        // stop and throw when the player is not looking at him
        if (runTime >= minRunTime && !IsOnScreen()) hiddenTimer += dt; else hiddenTimer = 0f;
        if (hiddenTimer >= offscreenTime) StartThrow();
    }

    void StartThrow()
    {
        state = State.Throw;
        toThrow = Random.Range(minBoulders, maxBoulders + 1);
        thrown = 0;
        throwTimer = 0.8f;
    }

    void UpdateThrow(float dt)
    {
        FaceDirection(player.position - transform.position);
        throwTimer -= dt;
        if (throwTimer > 0f) return;

        ThrowBoulder();
        thrown++;
        throwTimer = throwInterval;
        if (thrown >= toThrow) StartRest();
    }

    void StartRest()
    {
        state = State.Rest;
        restTimer = restDuration;
        if (health != null) health.BeginRest(restDuration);
    }

    void UpdateRest(float dt)
    {
        FaceDirection(player.position - transform.position);
        restTimer -= dt;
        if (restTimer <= 0f) StartRun();
    }

    // ---------- shots ----------

    // BossHealth calls this on every hit. Returns true if the damage should count.
    public bool RegisterShot()
    {
        if (state == State.Rest) return true;

        TeleportToPoint(PickPoint(false));
        hasDestination = false;
        return Random.value < damageChanceWhenNotResting;
    }

    // ---------- helpers ----------

    void ThrowBoulder()
    {
        Vector3 origin = transform.position + Vector3.up * throwHeight + transform.forward * 1f;
        Vector3 target = player.position + Vector3.up * 1f;

        if (boulderPrefab != null)
        {
            GameObject b = Instantiate(boulderPrefab, origin, Quaternion.identity);
            // next step: Boulder script implements Launch(Vector3 target)
            b.SendMessage("Launch", target, SendMessageOptions.DontRequireReceiver);
        }
    }

    void PickDestination()
    {
        bool toward = Random.value < runAtPlayerChance;
        int idx = PickPoint(toward, true);

        if (idx < 0)
        {
            // no fresh point to run to: teleport away instead of running back
            TeleportToPoint(PickPoint(false, false));
            int here = lastIndex;
            recent.Clear();
            recent.Add(here);
            idx = PickPoint(toward, true);
            if (idx < 0) idx = PickPoint(false, false);
        }

        Remember(idx);
        destination = points[idx].position;
        hasDestination = true;
    }

    void Remember(int i)
    {
        recent.Remove(i);
        recent.Add(i);
        while (recent.Count > rememberPoints) recent.RemoveAt(0);
    }

    // closestToPlayer = true: nearest point that is still allowed. false: weighted random.
    int PickPoint(bool closestToPlayer) { return PickPoint(closestToPlayer, false); }

    int PickPoint(bool closestToPlayer, bool avoidRecent)
    {
        int n = points.Length;
        float[] weight = new float[n];
        float total = 0f;
        int closest = -1, farthest = 0;
        float closestD = float.MaxValue, farthestD = -1f;

        for (int i = 0; i < n; i++)
        {
            float d = Flat(points[i].position - player.position).magnitude;
            if (d > farthestD) { farthestD = d; farthest = i; }

            if (i == lastIndex && n > 1) continue;
            if (avoidRecent && recent.Contains(i)) continue;
            if (d < tooClose) continue;

            if (d < closestD) { closestD = d; closest = i; }
            weight[i] = (d >= bestMin && d <= bestMax) ? 10f : 1f;
            total += weight[i];
        }

        if (total <= 0f) return avoidRecent ? -1 : farthest;
        if (closestToPlayer && closest >= 0) return closest;

        float r = Random.value * total;
        for (int i = 0; i < n; i++)
        {
            r -= weight[i];
            if (r <= 0f && weight[i] > 0f) return i;
        }
        return farthest;
    }

    void TeleportToPoint(int i)
    {
        lastIndex = i;
        Remember(i);
        transform.position = SnapToGround(points[i].position);
    }

    bool IsOnScreen()
    {
        Camera cam = Camera.main;
        if (cam == null) return true;
        Vector3 v = cam.WorldToViewportPoint(transform.position + Vector3.up * 3f);
        return v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f;
    }

    void FaceDirection(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;
        Quaternion t = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, t, turnSpeed * Time.deltaTime);
    }

    Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    Vector3 SnapToGround(Vector3 pos)
    {
        Vector3 start = new Vector3(pos.x, pos.y + rayStartHeight, pos.z);
        RaycastHit[] hits = Physics.RaycastAll(start, Vector3.down, rayStartHeight * 3f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in hits)
        {
            if (h.transform == transform || h.transform.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<BossGate>() != null) continue;   // ignore the glass sphere
            if (player != null && (h.transform == player || h.transform.IsChildOf(player))) continue;
            return new Vector3(pos.x, h.point.y + footOffset, pos.z);
        }
        return new Vector3(pos.x, transform.position.y, pos.z);
    }
}