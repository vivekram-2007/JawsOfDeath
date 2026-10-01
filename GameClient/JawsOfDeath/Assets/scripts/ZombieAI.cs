using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    public float appearRange = 10f;
    public float giveUpRange = 25f;
    public float attackRange = 2f;

    private NavMeshAgent agent;
    private Transform player;
    private Renderer[] renderers;
    private Animator animator;
    private bool isChasing = false;

    // Shared by ALL zombies — true when player is in any safe zone
    public static bool playerInSafeZone = false;

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

        // Start chasing once player gets close enough
        if (!isChasing && distance <= appearRange)
        {
            isChasing = true;
            SetVisible(true);
        }

        // Stop chasing if player is in a safe zone, or got too far away
        if (isChasing && (playerInSafeZone || distance > giveUpRange))
        {
            isChasing = false;
        }

        if (isChasing)
        {
            animator.SetBool("IsChasing", true);
            agent.isStopped = false;
            agent.SetDestination(player.position);

            if (distance <= attackRange)
            {
                animator.SetBool("IsAttacking", true);
                agent.isStopped = true;
            }
            else
            {
                animator.SetBool("IsAttacking", false);
            }
        }
        else
        {
            animator.SetBool("IsChasing", false);
            animator.SetBool("IsAttacking", false);
            agent.isStopped = true;
            SetVisible(false);
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