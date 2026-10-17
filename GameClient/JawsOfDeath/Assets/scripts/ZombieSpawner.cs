using UnityEngine;
using UnityEngine.AI;

public class ZombieSpawner : MonoBehaviour
{
    [System.Serializable]
    public class AvoidZone
    {
        public Transform center;
        public float radius = 30f;
    }

    [System.Serializable]
    public class DensityZone
    {
        public Transform center;
        public float radius = 60f;
        [Range(0f, 1f)] public float acceptChance = 0.2f; // 1 = super hot, low = cold
        public Color gizmoColor = Color.blue;
    }

    public GameObject zombiePrefab;      // drag the ZombiePlaceholder PREFAB from the Project window
    public Terrain terrain;              // drag Terrain1
    public Transform parent;             // drag the zombies object
    public int count = 120;
    public float playerSafeRadius = 25f; // no zombies this close to the player at start
    public AvoidZone[] avoidZones;       // SafeZone_Orb1, BossARENA

    [Header("Density zones")]
    [Range(0f, 1f)] public float normalChance = 0.5f; // chance outside every zone
    public DensityZone[] densityZones;                // 1 hot + 2 cold

    void Start()
    {
        if (zombiePrefab == null || terrain == null) return;

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        int spawned = 0;
        int attempts = 0;
        while (spawned < count && attempts < count * 60)
        {
            attempts++;
            Vector3 p = origin + new Vector3(Random.value * size.x, 0f, Random.value * size.z);

            NavMeshHit hit;
            if (!NavMesh.SamplePosition(p + Vector3.up * 50f, out hit, 100f, NavMesh.AllAreas)) continue;
            if (IsBlocked(hit.position, player)) continue;
            if (Random.value > ChanceAt(hit.position)) continue;

            Instantiate(zombiePrefab, hit.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
            spawned++;
        }
        Debug.Log("ZombieSpawner: spawned " + spawned + " zombies");
    }

    float ChanceAt(Vector3 pos)
    {
        float best = -1f;
        foreach (DensityZone z in densityZones)
        {
            if (z.center == null) continue;
            if (Flat(pos - z.center.position).magnitude < z.radius)
                best = Mathf.Max(best, z.acceptChance);
        }
        return best >= 0f ? best : normalChance;
    }

    bool IsBlocked(Vector3 pos, GameObject player)
    {
        if (player != null && Flat(pos - player.transform.position).magnitude < playerSafeRadius) return true;

        foreach (AvoidZone z in avoidZones)
        {
            if (z.center != null && Flat(pos - z.center.position).magnitude < z.radius) return true;
        }
        return false;
    }

    Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    void OnDrawGizmos()
    {
        if (densityZones == null) return;
        foreach (DensityZone z in densityZones)
        {
            if (z.center == null) continue;
            Gizmos.color = z.gizmoColor;
            Gizmos.DrawWireSphere(z.center.position, z.radius);
        }
    }
}