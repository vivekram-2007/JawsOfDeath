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

    public GameObject zombiePrefab;      // drag the ZombiePlaceholder PREFAB from the Project window
    public Terrain terrain;              // drag Terrain1
    public Transform parent;             // drag the zombies object
    public int count = 50;
    public float playerSafeRadius = 25f; // no zombies this close to the player at start
    public AvoidZone[] avoidZones;       // SafeZone_Orb1, BossARENA

    void Start()
    {
        if (zombiePrefab == null || terrain == null) return;

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        int spawned = 0;
        int attempts = 0;
        while (spawned < count && attempts < count * 30)
        {
            attempts++;
            Vector3 p = origin + new Vector3(Random.value * size.x, 0f, Random.value * size.z);

            NavMeshHit hit;
            if (!NavMesh.SamplePosition(p + Vector3.up * 50f, out hit, 100f, NavMesh.AllAreas)) continue;
            if (IsBlocked(hit.position, player)) continue;

            Instantiate(zombiePrefab, hit.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), parent);
            spawned++;
        }
        Debug.Log("ZombieSpawner: spawned " + spawned + " zombies");
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
}