using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    public float chaseRange = 15f;
    public float appearRange = 10f;
    private NavMeshAgent agent;
    private Transform player;
    private Renderer[] renderers;
    private Animator animator;
    private bool hasSpotted = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        renderers = GetComponentsInChildren<Renderer>();
        SetVisible(false);
    }

    void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= appearRange)
            SetVisible(true);
        else
            SetVisible(false);

        if (distance <= chaseRange)
        {
            agent.SetDestination(player.position);
            if (!hasSpotted)
            {
                animator.SetTrigger("SpotPlayer");
                hasSpotted = true;
            }
        }
        else
        {
            hasSpotted = false;
        }
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.y = 1f;
        transform.position = pos;
    }

    void SetVisible(bool visible)
    {
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }
    }
}