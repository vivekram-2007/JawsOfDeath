using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    public float chaseRange = 15f;
    public float appearRange = 10f; // set this smaller than chaseRange or equal
    private NavMeshAgent agent;
    private Transform player;
    private Renderer[] renderers;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        renderers = GetComponentsInChildren<Renderer>();
        SetVisible(false); // start hidden
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= appearRange)
        {
            SetVisible(true);
        }
        else
        {
            SetVisible(false);
        }

        if (distance <= chaseRange)
        {
            agent.SetDestination(player.position);
        }
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }
    }
}